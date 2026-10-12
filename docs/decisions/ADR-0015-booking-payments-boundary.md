# ADR-0015: Booking ↔ Payments Boundary

**Status:** Proposed — requires Architect sign-off before `Bookings` is scaffolded. Does not implement `Payments`.

## Context

`docs/product/TRIAL_BOOKING_WORKFLOW.md` and `ADR-0004` already establish the required *shape*: Payments is webhook-authoritative, Bookings must never confirm on a frontend "it worked" signal, webhook delivery is at-least-once (idempotency required), and Booking/Payment are separate state machines reacting to each other's events. What's missing is the literal contract (event names, correlation identifier, exact per-outcome behavior) `Bookings` needs in order to be implemented — and `Payments` does not exist yet, so nothing here can be wired live.

**Resolved (2026-10-04) — see `ADR-0019`:** §2 and §3 below originally described `Bookings` calling `Payments`' payment-intent-creation contract "synchronously" without specifying how that call crosses the module boundary under this codebase's sanctioned mechanisms; §5 originally left the refund-request transport undecided. Both are now settled by `ADR-0019`: checkout uses a plain server-to-server HTTP call from `Bookings` to `Payments`' own public API (not a `TutorFlow.Shared` command interface, not DI), validated on `Payments`' side via a new read-only query contract (`IBookingCheckoutEligibilityQuery`, implemented by `Bookings`); refund-request is a plain integration event. §3 below is updated to reflect the resulting idempotency refinement; §5 is updated to confirm the event-based transport. See `ADR-0019` for the full mechanism and rationale.

## Decision

### 1. Correlation identifier

`Booking` stores an opaque `PaymentReferenceId` (`string?`), assigned once `Bookings` requests payment for a `Held` booking. `Bookings` does not know or assume this is a Stripe `PaymentIntent` ID specifically — it is whatever correlation token `Payments`' contract returns. This keeps `Bookings` decoupled from `ADR-0004`'s Stripe choice; only `Payments` needs to know it's Stripe.

### 2. State transitions and their triggers

The full, single authoritative `Booking` transition table lives in `BOOKINGS_SPEC.md` §2 (corrected 2026-10-04 to resolve contradictions this ADR's original table was partly responsible for). The rows relevant to the `Payments` boundary, restated here for context only — **`BOOKINGS_SPEC.md` §2 governs if anything here ever appears to disagree with it**:

| Booking transition | Trigger | Authoritative source |
|---|---|---|
| `Held` → `PendingPayment` | Student initiates checkout (`POST /api/bookings/{id}/checkout`, see `BOOKINGS_SPEC.md` §4) | `Bookings` transitions first, then calls `Payments`' public HTTP API (`POST /api/payments/intents`, `ADR-0019`) server-to-server and stores the returned `PaymentReferenceId` — see `ADR-0019` for the exact mechanism |
| `PendingPayment` → `Confirmed` | A `PaymentSucceeded`-shaped event, carrying `PaymentReferenceId` | `Payments`, webhook-driven — never the frontend |
| `PendingPayment` → `Cancelled` | Student/tutor explicitly cancels before payment succeeds | `Bookings`' cancel endpoint. **Never produces a refund** — by definition, if `Status` is still `PendingPayment`, payment has not yet succeeded (success is exactly what moves it to `Confirmed`), so there is nothing captured to refund. `Bookings` must not request a refund for this transition. |
| `PendingPayment` → `Expired` | The hold's own TTL elapsing (same clock as `Held`'s TTL) | `Bookings`' own expiry sweep (`ADR-0013` §2, now covering `PendingPayment` too) — not a `Payments`-originated trigger. A `PaymentFailed`-shaped event, if `Payments` sends one before the TTL elapses, is handled the same way: `PendingPayment` → `Cancelled` (treated as an explicit failure, not a timeout) — the exact reporting distinction between a `Payments`-reported failure and a TTL-driven expiry remains **`[OPEN — Product Owner]`**, unchanged |
| `Confirmed` → `Cancelled` | Student/tutor cancels a confirmed booking | `Bookings`' cancel endpoint, synchronous — does **not** by itself imply a refund; see the next row |
| `Cancelled` → `Refunded` | A `PaymentRefunded`-shaped event, *only* after `Bookings` has itself requested the refund, and only reachable from a `Cancelled` booking that was previously `Confirmed` (i.e. money was actually captured) | `Payments`, webhook-driven (see §4). **This is the only path to `Refunded`** — there is no direct `Confirmed` → `Refunded` edge; see `BOOKINGS_SPEC.md` §2 |

"Requires customer action" (SCA/3DS) does **not** map to a distinct `Booking` status. The booking simply remains `PendingPayment` while the frontend completes the challenge; `Payments` only notifies `Bookings` on a terminal outcome (succeeded/failed), consistent with `Payment`'s own `RequiresAction` state (`docs/domain/STATE_MACHINES.md`) being internal to the Payment state machine, not mirrored into Booking's.

### 3. Checkout: state check and idempotency, before anything else

**Refined by `ADR-0019` (2026-10-04)** — superseding this section's original ordering, which predated the exact call mechanism being decided:

1. **Read the booking's current status first.** Do this before calling `Payments` for anything.
2. **Only `Held` or `PendingPayment` may proceed.** Any other status fails with `409` — do not call `Payments`.
3. **If `Held`, conditionally transition to `PendingPayment`** (`WHERE Status = 'Held'`, `ADR-0013` §3). If already `PendingPayment`, skip this step — an earlier attempt already did it.
4. **Call `Payments`' public HTTP API (`POST /api/payments/intents`, `ADR-0019`) — on every attempt, including retries, never short-circuited.** This is the corrected behavior: the original text here said a `PendingPayment`-with-stored-reference retry should return the existing reference "without calling `Payments` again." That is **no longer correct**, because `Payments`' `client_secret` is never persisted by `Bookings` (`ADR-0018` §2) — a retry has nothing cached to fall back to, so it must ask `Payments` again to get a usable `client_secret`. Safety against a duplicate Stripe PaymentIntent comes from the idempotency key below, not from `Bookings` avoiding the call.
5. Relay `Payments`' response to the frontend; store the returned `PaymentReferenceId` on the `Booking` row (the conditional `UPDATE` in step 3 and the `PaymentReferenceId` store in this step may be two statements or combined, as convenient — both are idempotent).

**`BookingId` is the idempotency key** `Payments` uses for its own create-or-retrieve call to Stripe (`ADR-0018` §2) — deterministically derived, recomputed fresh on every attempt, never stored or passed between calls. This is what makes step 4's "always call, never short-circuit" safe: Stripe returns the *same* PaymentIntent (and a valid `client_secret` for it) on every attempt for the same `BookingId`, never a duplicate — covering both a simple frontend retry and the specific failure mode where `Payments`' call to Stripe succeeds but a subsequent local write fails before committing.

### 4. Idempotent webhook processing

Per `ADR-0013` §3, the `PendingPayment` → `Confirmed` transition is a single conditional `UPDATE ... WHERE id = @id AND status = 'PendingPayment'`:

- If it affects one row: confirmed for the first time: proceed to publish `BookingConfirmed` (`ADR-0016`).
- If it affects zero rows: re-read the booking. If already `Confirmed`, this is a duplicate webhook delivery — treat as idempotent success, do **not** re-publish `BookingConfirmed`. If in any other status (e.g. already `Expired`), this is a genuine conflict (payment succeeded for a hold that already expired) — surfaced as an error for investigation, not silently swallowed; the exact remediation policy (auto-refund? manual review?) is **`[OPEN — Product Owner]`**.
- **If the conditional `UPDATE` itself fails for a transient reason (DB connection lost, timeout) rather than affecting zero rows** — the handler must propagate this as a retryable failure (an HTTP `5xx`), never swallow it into a `200`/success response. Payment providers (Stripe and the webhook pattern generally) redeliver a webhook on a non-2xx response; silently absorbing a transient local failure would permanently lose that confirmation, since the provider would never know to retry. This is a correction (architecture review, 2026-10-04) — the original ADR text specified the idempotency logic but not this failure-propagation requirement.

This satisfies "duplicate payment/webhook processing" without `Bookings` needing to know anything about how `Payments` itself deduplicates on its own side.

### 5. Refund direction — confirms the previously-open question, and clarifies the edge it applies to

`docs/product/TRIAL_BOOKING_WORKFLOW.md` already recommended, without confirming, that `Bookings` must never unilaterally decide it is `Refunded`. **This ADR accepts that direction as final**: `Bookings` publishes a refund-request to `Payments`; `Payments` processes it and emits a `PaymentRefunded`-shaped event; only on receiving that event does `Bookings` transition to `Refunded`. This is a mechanical/architectural confirmation, not a business-policy decision — *when* a refund is warranted (cancellation policy, notice windows, partial vs. full) remains entirely `[OPEN — Product Owner]` per `docs/OPEN_QUESTIONS.md` #6/#18.

**Transport resolved (`ADR-0019`, 2026-10-04):** the refund-request is a **plain integration event** (`RefundRequested`, `TutorFlow.Shared`), not a synchronous call of any kind. Unlike checkout, a refund request never needs an immediate response — the cancel endpoint already returns `200 Cancelled` to the user synchronously on `Bookings`' own transition, before `Payments` is even involved. `Bookings` publishes `RefundRequested`; `Payments` consumes it whenever, processes the Stripe refund, and publishes `PaymentRefunded` back once Stripe confirms — the same async, eventually-consistent shape as every other `Bookings`↔`Payments` outcome notification.

**Correction (architecture review, 2026-10-04):** the edge this applies to is `Cancelled` → `Refunded`, never `Confirmed` → `Refunded` directly. Cancelling a `Confirmed` booking is a synchronous, immediate transition to `Cancelled` via the cancel endpoint; *whether* `Bookings` then also requests a refund for that specific cancellation is the open policy question above, and only matters at all when the booking being cancelled had reached `Confirmed` (i.e. payment had actually succeeded) — cancelling from `Held` or `PendingPayment` never has anything to refund (§2). A `Completed` booking is terminal with no `Bookings`-side transition at all; a post-completion dispute refund is recorded entirely within `Payments`' own state machine (`Succeeded → Disputed → ... → Won/Lost`) and does not change `Booking.Status` — see `BOOKINGS_SPEC.md` §2 for the full reasoning.

### 6. Product Owner decision locked for V1 (2026-10-04): the cancellation-window policy, and full-refund-only

The *whether a refund is warranted* question this ADR left open above is now resolved:

- **Cancelled ≥24 hours before the booking's `StartUtc`: full refund.** `Bookings` requests a refund from `Payments` for the full captured amount.
- **Cancelled <24 hours before `StartUtc`: no refund.** `Bookings` does not request a refund at all — the booking simply reaches `Cancelled` and stays there (never `Refunded`).
- **No partial-refund tiers in V1.** The refund-request contract carries no amount parameter — a request is always "refund the full captured amount," never a percentage or fixed sub-amount. This is `ADR-0004`'s "partial refunds: out of scope for V1" decision applied here; see `ADR-0004`'s "Product Owner decisions locked for V1" section.
- **No-show is not a cancellation and does not go through this edge.** `BookingStatus.NoShow` is reached from `InProgress`/`Confirmed` directly (per `BOOKINGS_SPEC.md` §2) and is itself terminal with no outbound edge — this is unchanged by the no-show policy below; a no-show-triggered refund (tutor no-show) is requested by `Bookings` and processed by `Payments` exactly as any other refund request, but **`Booking.Status` stays `NoShow`** — it never transitions to `Refunded`. This follows the same already-accepted pattern as a post-completion dispute refund (§5 above): a refund that doesn't correspond to the `Cancelled → Refunded` edge is recorded entirely within `Payments`' own state machine, without a `Booking.Status` change, rather than inventing a second refund-reachable terminal state. See `docs/domain/STATE_MACHINES.md` and `docs/product/TRIAL_BOOKING_WORKFLOW.md` for the full no-show policy (grace period, who declares it, and the student-no-show/tutor-no-show payment-consequence asymmetry).
- **24 hours is measured against the booking's `StartUtc`** (the slot being cancelled), evaluated at the moment the cancel endpoint is called — not against `HeldAtUtc`/`ConfirmedAtUtc` or any other timestamp.

This is a Product Owner decision with rationale, not an architectural one: 24 hours is chosen as a single, simple threshold (no tiered partial-refund percentages) to keep V1's refund logic a one-line conditional rather than a schedule, consistent with the "full-refund-only" scope decision above. None of this is implemented by this update — the `CancelBooking` handler does not yet issue any refund request; this is the policy it must follow once that integration exists.

## What this ADR does not decide

- The literal event/message names and payload schemas (finalized when both `Bookings` and `Payments` exist and Wolverine is introduced — see "Messaging infrastructure" below).
- Any Stripe-specific detail (PaymentIntent types, Connect account model, webhook signature verification mechanics) — entirely `Payments`' concern per `ADR-0004`.
- Payout frequency/minimum/fees, earnings accrual timing, VAT/tax/invoicing — remain `[OPEN — Product Owner]`, unchanged; see `docs/OPEN_QUESTIONS.md`.

## Messaging infrastructure

Wolverine is **not** added by this ADR. The `PaymentSucceeded`/`PaymentFailed`/`PaymentRefunded` events this contract describes cannot be wired today because `Payments` (the publisher) does not exist — there is nothing to connect yet, exactly the situation already encountered for `Tutors` → `Availability` bookability (`ADR-0011`). Whichever module — `Bookings` or `Payments` — is implemented second is responsible for introducing Wolverine (or confirming a lighter interim mechanism) at that point, since that is the first moment a real publisher and a real consumer coexist.

## Consequences

- `Bookings`' first implementation slice can only be built as far as `PendingPayment` plus the conditional-confirm handler's *shape* (the handler and its idempotency logic can be written and unit-tested against a synthetic "payment succeeded" trigger even before `Payments` exists) — it cannot be exercised end-to-end, and the `POST /api/bookings/{id}/checkout` endpoint's call into `Payments` must be stubbed/deferred until `Payments` is implemented. See `BOOKINGS_SPEC.md` §9 for the proposed sequencing.
- `Booking.PaymentReferenceId` and the confirm/refund handlers are the extension points `Payments` integrates against once built; no `Bookings` code should need to change shape when that happens, only the event-wiring (Wolverine subscription) needs to be added.
- **`ADR-0019` (2026-10-04)** settles the two mechanisms this ADR left unspecified: checkout is a server-to-server HTTP call from `Bookings` to `Payments`, validated via the new read-only `IBookingCheckoutEligibilityQuery`; refund-request is a plain integration event. `Bookings`' checkout handler will need a small, additive change (an `IHttpClientFactory`-based call to `Payments`, per the refined §3 ordering above) beyond what was anticipated when `Bookings` was first implemented — this is new work, not a correction of anything already built incorrectly.
