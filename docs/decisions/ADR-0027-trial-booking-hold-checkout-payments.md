# ADR-0027: Trial Booking — Hold, Checkout, and the Payments Boundary

**Status:** Accepted

## Context

Fresh Preply research this phase (help-center + independent sources) confirms the baseline already assumed: 25/50-minute trial, duration locked after booking, one trial per tutor, date/time chosen after duration, payment/balance selection completes the booking, the lesson then appears in the student's upcoming schedule. Two things are newly confirmed and matter architecturally:

1. **Preply tutors can accept or decline a trial request** (scheduling conflict, subject mismatch, etc.); a decline returns the trial credit to the student's balance to rebook with someone else. This is a **real behavioral gap** against TutorFlow's current design, where a trial becomes `Confirmed` purely from payment success with no tutor-approval gate. Implementing it properly requires a lesson-credit/balance concept that does not exist yet (roadmap item 13, "Lesson Balance") and a cancellation/refund-to-balance mechanism (roadmap item 15) — building a partial version now would be exactly the "invent a second draft-storage/second refund system" anti-pattern this project's own law warns against. **Deliberately not implemented this phase** — tracked as a real, documented Preply deviation (see §9 of the final report), not silently dropped.
2. A slot-unavailable error ("already booked by another student," minimum-notice/advance-booking violations) is communicated as a plain "try another time" message, not a technical error — matches what this phase already intended to build.

A direct re-audit of the repository (not prior reports) confirms everything previously found still holds: `Bookings` already fully owns `CreateTrialHold`/`GetTrialEligibility`/the `Held→PendingPayment→Confirmed` chain, the Postgres exclusion constraint, and the lazy expiry sweep (covering both `Held` and `PendingPayment`). `CheckoutBooking.Handler` still unconditionally returns `PaymentsUnavailable` (→ 501) for the one real branch, because `Payments` is still a literal empty stub (`AddPaymentsModule`/`MapPaymentsModule` do nothing). `ADR-0019` (Accepted) and `docs/domain/PAYMENTS_SPEC.md` already fully specify the mechanism this phase must build — this ADR does not re-decide that mechanism, it executes it, with one explicit, scoped deviation.

## Decisions

### 1. Build exactly the mechanism ADR-0019/PAYMENTS_SPEC.md already specify

`Bookings.CheckoutBooking.Handler` is rewritten per `ADR-0019`'s refined ordering: read the booking, conditionally transition `Held→PendingPayment` (`WHERE Status='Held'`, no-op if already `PendingPayment`), then — on *every* attempt, never short-circuited — call `Payments`' own public API (`POST /api/payments/intents`, server-to-server via `IHttpClientFactory`, never DI, never a `TutorFlow.Shared` command interface) with only `{BookingId, StudentUserId}`. `Payments` independently validates the call via a new read-only `IBookingCheckoutEligibilityQuery` (`TutorFlow.Shared`, implemented by `Bookings`, consumed via DI by `Payments` — a fourth instance of `ADR-0014`'s existing pattern) requiring the booking to already be `PendingPayment` and owned by the caller — this ordering, not a new auth scheme, is what prevents calling `Payments` for a `Held` booking directly.

### 2. `Payments` is built for real, scoped to what the trial flow needs

`Payment` entity (`Created→RequiresAction→Processing→Succeeded|Failed`, per `PAYMENTS_SPEC.md` §2) and `PaymentWebhookEvent` (append-only, `StripeEventId`-unique) — both specified fully in `PAYMENTS_SPEC.md` §1. **`TutorConnectedAccount` and the refund lineage (`Succeeded→RefundPending→Refunded`, `Disputed→…`) are deliberately not built this phase** — refunds/cancellation are explicitly out of scope (§27 of this phase's brief), and Connect account onboarding has no caller anywhere yet (no tutor onboarding step produces one — `ADR-0026` didn't build it either, since it requires the still-undecided commission rate, `OPEN_QUESTIONS.md` #34/A32). This is a real, scoped deviation from `ADR-0018` §1's "destination charges" framing: this phase creates a **plain PaymentIntent on the platform's own Stripe account** (no `transfer_data`, no `application_fee_amount`) — the mechanics (`Payment` row, idempotency key, state machine, webhook pipeline) are exactly as specified; only the Connect-specific charge routing is deferred, tracked explicitly, not silently resolved.

### 3. Provider abstraction: real Stripe code path + a deterministic test adapter, both behind the same interface

`IPaymentProviderClient` (internal to `Payments`, per `ADR-0018` §4) with a real `StripePaymentProviderClient` (Stripe.net, used whenever `Payments:Stripe:SecretKey` is configured) and a `TestPaymentProviderClient` (deterministic, used otherwise — this local/dev/test environment has no Stripe account). Both implement the same create-or-retrieve-PaymentIntent contract; nothing about the `Payment` row, state machine, or webhook pipeline differs by which one is active. This satisfies "use a deterministic test/fake adapter only behind the existing abstraction, never leak it into production architecture" literally: the test adapter is swappable infrastructure, not a product-logic shortcut.

### 4. Webhook processing is real; "payment succeeded" is never frontend-asserted

`POST /api/payments/webhooks/stripe` performs real Stripe signature verification (`ADR-0018` §3) before anything else, logs the event (`PaymentWebhookEvent`, dedup by `StripeEventId`), and dispatches to a handler that does a conditional `UPDATE ... WHERE Status = @expected` on `Payment`, then publishes `PaymentSucceeded`/`PaymentFailed` (`TutorFlow.Shared`). This is the **first real publisher+consumer pair for `Payments`**, so Wolverine is introduced for it now (exactly as `ADR-0015`'s "Messaging infrastructure" section already anticipated) — joining `Search`/`Availability` as a third discovered assembly.

### 5. Bookings consumes the outcome; no synchronous cross-module call

Two new `Bookings` Wolverine consumers: `PaymentSucceededConsumer` (calls the already-built, already-idempotent `ConfirmBooking.Handler`) and `PaymentFailedConsumer` (calls a new internal, system-triggered cancellation path — `CancelBooking.Handler` assumes a user caller and an audit `CancelledByUserId`; webhook-triggered failure has neither, so this is a small, additive new handler, not a change to `CancelBooking`'s existing shape, per `PAYMENTS_SPEC.md` §16/`OPEN_QUESTIONS.md` A33).

### 6. Tutor-side accept/decline: not implemented, explicitly deferred

Per §Context above. The booking state machine stays exactly `Held→PendingPayment→Confirmed` (plus `Expired`/`Cancelled`) — no `PendingTutorApproval` state is added. Flagged as a real, benchmarked Preply behavior TutorFlow does not yet match, not silently ignored.

### 7. Frontend: `/bookings/new` becomes the real hold→checkout→payment→confirmation flow

The honest placeholder from the Availability phase is replaced. Loading the page (with the selection carried in the URL, unchanged hand-off contract from `ADR-0025`) now calls `CreateTrialHold` for the first time — this is the actual commit moment; browsing never held anything, this page does, exactly where the Availability phase's ADR said the boundary belonged. On success, the UI shows the temporary-hold state (no countdown timer — not reliably observable as a Preply pattern, and a clean "reserved while you check out" message is preferred over inventing one purely because it's technically possible), then calls `CheckoutBooking` to obtain a `clientSecret`, then either mounts real Stripe Elements (if `NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY` is configured) or shows an honest "payment isn't configured in this environment" boundary — never a fake "mark as paid" button. Confirmation is reached only by polling `GetBooking` until the webhook-driven `Confirmed` transition lands (or seeing a human-friendly expired/failed state) — never asserted by the frontend itself.

## Consequences

- `TutorFlow.Shared` gains `IBookingCheckoutEligibilityQuery`/`BookingCheckoutEligibility`, `PaymentSucceeded`, `PaymentFailed`.
- `Bookings` gains a small `IHttpClientFactory`-based `Payments` client, an `IBookingCheckoutEligibilityQuery` implementation, and a new system-cancellation path for webhook-triggered failure.
- `Payments` goes from an empty stub to a real module: `PaymentsDbContext` (schema `payments`), `Payment`/`PaymentWebhookEvent` entities, `CreatePaymentIntent` feature, Stripe webhook endpoint, Wolverine wiring, Stripe.net package.
- `Program.cs` gains `Payments` as a third Wolverine-discovered assembly plus the matching `AlwaysUseServiceLocationFor<PaymentsDbContext>()` allow-list entry.
- No `ProjectReference` added between `Payments` and `Bookings`, in either direction.
- `TutorConnectedAccount`, destination-charge routing, refund lineage, and tutor accept/decline remain explicitly future work — not silently dropped, tracked in `OPEN_QUESTIONS.md`.
