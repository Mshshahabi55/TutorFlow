# ADR-0018: Payments Module — Stripe Integration Mechanics

**Status:** Proposed — requires Architect sign-off before `Payments` is scaffolded. Does not implement `Payments`.

## Context

`ADR-0004` (Accepted) already settled Stripe + Stripe Connect Express as the provider, and the non-negotiable requirements (webhooks authoritative, signature verification, idempotency, Payment/Booking state independence, integer-minor-units money). It explicitly deferred the literal mechanics — exactly as `ADR-0003` deferred the literal exclusion-constraint mechanics that `ADR-0013` later provided for `Bookings`. This ADR provides those mechanics for `Payments`, mirroring that precedent.

**Resolved (2026-10-04):** the synchronous call `Bookings` makes to trigger PaymentIntent creation, and the refund-request transport, are now settled by `ADR-0019` (Accepted) — a server-to-server HTTP call from `Bookings` to `Payments`' `POST /api/payments/intents`, validated via a new read-only `IBookingCheckoutEligibilityQuery`; refund-request is a plain integration event. This ADR covers only what happens once a call reaches `Payments` — see `ADR-0019` for the boundary-crossing mechanism itself.

## Decision

### 1. Charge model: destination charges

Each successful trial booking produces one Stripe charge created **on the platform's own Stripe account**, with `transfer_data.destination` set to the tutor's Stripe Connect Express account ID. This is Stripe's own recommended pattern for a marketplace that charges the customer and pays a connected account, and is the simplest of Stripe's three marketplace charge patterns (destination charges vs. direct charges vs. separate charges and transfers) for a platform that wants to remain the merchant of record.

**Not decided by this ADR:** the `application_fee_amount` parameter (the platform's commission/take-rate on each charge) — this requires the commission/take-rate business decision, which is open (not one of `docs/OPEN_QUESTIONS.md` #12/#13/#29, but adjacent and equally un-invented here; tracked as a new Product Owner item, see `docs/OPEN_QUESTIONS.md`). Until that's resolved, the exact `PaymentIntent.Create` call cannot be fully specified — this is a named implementation prerequisite (see `PAYMENTS_SPEC.md` §H), not a conflict with existing docs.

### 2. PaymentIntent lifecycle mechanics

- One `Payment` row (TutorFlow's own entity) is created per PaymentIntent-creation attempt, in `Created` status, before the Stripe API call is made.
- The Stripe API call passes an `Idempotency-Key` header derived deterministically from `BookingId` (e.g. `create-intent:{bookingId}`), satisfying `ADR-0015` §3's requirement that `BookingId` be usable as the idempotency key — a retried creation attempt for the same booking returns Stripe's *same* PaymentIntent, never a duplicate.
- `Payment.AmountMinorUnits`/`Currency` are a **snapshot** taken from the values passed into the create-call (ultimately sourced from `Booking.PriceAmountMinorUnits`/`PriceCurrency`) — `Payments` never queries `Bookings`' database to re-derive them, and never re-derives them from `Tutors`' live pricing either. This mirrors `Booking`'s own "price snapshot, not a live reference" philosophy (`BOOKINGS_SPEC.md` §1.3), applied symmetrically on the `Payments` side.
- `Payment.Id` (a TutorFlow-generated `Guid`) — not the literal Stripe `PaymentIntent` ID — is the opaque correlation token returned to `Bookings` as `PaymentReferenceId`, per `ADR-0015` §1 ("`Bookings` does not know or assume this is a Stripe `PaymentIntent` ID specifically"). The real Stripe ID (`StripePaymentIntentId`) is stored on the `Payment` row but never crosses the module boundary.
- The Stripe `client_secret` returned at creation is **never persisted** — it is used once, synchronously, to respond to whichever caller triggered creation (see `ADR-0019`), then discarded. This is deliberate security hygiene (Stripe itself recommends against long-term storage of client secrets), not a business-policy decision.

### 3. Webhook processing

- Single endpoint, `POST /api/payments/webhooks/stripe`, handling every subscribed Stripe event type via the payload's `type` field — Stripe's own convention, not a TutorFlow invention.
- **Signature verification is the first step, unconditionally** — the raw request body and `Stripe-Signature` header are verified against the webhook endpoint's signing secret (via Stripe's SDK helper) before any deserialization or processing. A signature mismatch returns `400` immediately; nothing about the payload is trusted before verification succeeds.
- **Event-level idempotency**: every verified event's Stripe `event.id` is checked against an append-only `PaymentWebhookEvents` log (see `PAYMENTS_SPEC.md` §F) *before* dispatching to a handler. An already-processed `event.id` short-circuits to `200` immediately — this is in addition to, not instead of, the `Payment`-row-level conditional-update idempotency in §4, mirroring `Bookings`' own stated "defense in depth" philosophy (`BOOKINGS_SPEC.md` §6: *"`Bookings`' idempotency does not depend on `Payments`' own webhook deduplication"* — restated here from the other side: `Payments`' webhook dedup does not depend on Stripe's at-least-once delivery guarantee alone either).
- **Transient failures must be retryable**: if processing fails for a transient reason (DB unavailable, etc.) after signature verification succeeds, the handler must return a `5xx`, never swallow the failure into a `200` — Stripe redelivers on non-2xx, exactly the same requirement already locked for `Bookings`' own (not-yet-built) webhook-consuming side (`ADR-0015` §4).
- **Unrecognized event types** (Stripe sends many TutorFlow doesn't act on) are logged to the event-log table and acknowledged with `200` — not an error.
- Each individual event handler (e.g. "process `payment_intent.succeeded`") performs its own conditional `UPDATE ... WHERE Status = @expected` transition on the `Payment` row — the same pattern `ADR-0013` §3 established for `Bookings`' concurrency-critical transitions, reused here for the equivalent reason (a single atomic statement, no read-then-write gap, safe against out-of-order or duplicate webhook delivery).

### 4. Provider abstraction

Per `ADR-0004`'s "keep the provider behind an abstraction where practical": Stripe SDK calls are isolated behind a narrow internal interface inside `Payments` (e.g. `IPaymentProviderClient` wrapping create-intent, create-refund, and connected-account calls) — this interface and its Stripe-backed implementation are entirely internal to `Payments`; nothing about it is exposed to `Bookings` or any other module. This is a code-organization expectation for whenever `Payments` is scaffolded, consistent with `ADR-0004`'s existing guidance, not a new cross-module contract.

## Consequences

- `Payments`' schema includes three tables (`Payments`, `TutorConnectedAccounts`, `PaymentWebhookEvents`) — full detail in `PAYMENTS_SPEC.md` §F.
- No exclusion constraint or generated column is needed in `Payments`' schema (unlike `Bookings`) — `Payments` has no overlapping-interval concurrency problem; its concurrency concerns are idempotency-based (unique constraints + conditional updates), simpler than `Bookings`' concurrency shape.
- This ADR does not decide the commission/take-rate business question, the Stripe Connect charge-type confirmation beyond the recommendation above, or anything covered by `ADR-0019`.
