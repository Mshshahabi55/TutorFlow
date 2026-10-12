# ADR-0019: Booking ↔ Payments Synchronous Call Mechanism

**Status:** Accepted (2026-10-04) — Option B, with a Bookings-orchestrated refinement. Does not implement `Payments` or `Bookings` code; no code, migration, or schema change accompanies this decision.

## The conflict (context, resolved below)

`ADR-0015` §2/§3 described `Bookings`' checkout endpoint calling `Payments`' payment-intent-creation contract "synchronously," and a refund-request call whose "exact transport [was] decided when both modules exist." Neither fit either cross-module mechanism this codebase had sanctioned — integration events (async) or `ADR-0014`'s read-only query contracts (explicitly excludes side effects) — because PaymentIntent creation is synchronous *and* side-effecting. Three candidate resolutions were presented (widen the query-contract pattern to commands; split the HTTP surface so the frontend calls `Payments` directly; go fully event-driven with polled/pushed client-secret delivery). See the project history for the full comparative analysis. **Option B is approved**, refined as described below.

## Decision

### Checkout: Bookings orchestrates a server-to-server HTTP call to Payments; the frontend only ever calls Bookings

- The frontend calls the **existing** `POST /api/bookings/{id}/checkout` endpoint — no new frontend-facing route, no change to who the frontend talks to.
- `Bookings`' `CheckoutBooking` handler performs its own ownership and state checks (unchanged from today), then — **synchronously, via a plain HTTP call** (`IHttpClientFactory`-based, not Dependency Injection, not a `TutorFlow.Shared` interface) — calls `Payments`' own public HTTP API (`POST /api/payments/intents`, `PAYMENTS_SPEC.md` §E) to create or retrieve the Stripe PaymentIntent, then relays the result back to the frontend as its own HTTP response.
- `Payments` **never trusts the amount or currency supplied by whatever calls it** — not the frontend (which never calls it directly under this decision) and not even `Bookings`' own HTTP request. `Payments` independently fetches the authoritative price, and confirms eligibility, via a **read-only** query contract it consumes (see below). `Bookings`' HTTP request to `Payments` carries only identifiers — `BookingId` and the authenticated `StudentUserId` — never amount or currency.
- `Payments` owns the actual Stripe operation end to end: the PaymentIntent create/retrieve call, the idempotency key, and the `client_secret`. `Bookings` never touches the Stripe SDK, never computes an idempotency key for Stripe, and never persists the `client_secret` — it only relays it through, once, in its own synchronous response to the frontend (`ADR-0018` §2's "never persist the client secret" is unaffected).

**Why this is not Option A:** no `TutorFlow.Shared` command interface is introduced; no `Payments` type or interface is resolved via DI inside `Bookings`. The call is a plain HTTP request to `Payments`' already-public API — the same mechanism an external client would use, and the same mechanism that survives unchanged if `Payments` is ever extracted into its own deployable (only the base URL/service-discovery would change, not the calling code's shape).

**Why this is not Option C:** there is no event, no polling, no secondary channel. The frontend's single call to `Bookings` returns the `client_secret` synchronously, exactly as fast as Option A would have, with no new latency.

**Why this is a refinement of Option B, not a fourth mechanism:** the "split HTTP surface, no in-process side-effecting call" property Option B was chosen for is fully preserved — `Payments` is still reached only via its own public HTTP API, never via `Bookings`-internal code. The only refinement from Option B's original framing is *who* calls that API: `Bookings` (server-to-server), not the frontend directly. This preserves "one frontend call starts checkout" (an advantage Option A had and the original Option B framing gave up) while preserving every architectural property Option B was chosen for.

### The reciprocal read-only query: `IBookingCheckoutEligibilityQuery`

`Payments` validates the `BookingId`/`StudentUserId` it receives via a new query contract, defined in `TutorFlow.Shared`, **implemented by `Bookings`**, **consumed via DI by `Payments`** — the exact same shape as `ADR-0014`'s existing three query contracts (`ITutorAvailabilityQuery`, `ITutorPricingQuery`, `IActiveBookingIntervalsQuery`), now a fourth instance, in a fourth direction (`Payments` consuming from `Bookings`).

```csharp
// TutorFlow.Shared
public interface IBookingCheckoutEligibilityQuery
{
    Task<BookingCheckoutEligibility> ValidateAsync(Guid bookingId, Guid callerStudentUserId, CancellationToken ct);
}

public sealed record BookingCheckoutEligibility(
    bool IsEligible,
    string? Reason,
    int? AmountMinorUnits,
    string? Currency,
    Guid? TutorUserId);
```

- `IsEligible` is `false` (with `Reason`) unless: the booking exists; `callerStudentUserId` matches the booking's `StudentUserId`; and the booking's status is **`PendingPayment`** — not `Held`. This last point is deliberate and load-bearing (see "Ordering is the security boundary" below).
- `AmountMinorUnits`/`Currency` are the authoritative price snapshot — `Payments` uses these, and only these, to create the Stripe PaymentIntent. Nothing in the HTTP request from `Bookings` can override them.
- `TutorUserId` is included as a "strictly necessary immutable payment fact" (per the approved scope) — `Payments`' `Payment` entity needs it to route the Connect destination-charge transfer (`ADR-0018` §1); without this query, `Payments` would have no other way to learn it without reaching into `Bookings`' schema.
- This is **read-only and idempotent** — no widening of `ADR-0014`'s scope. `ADR-0014` itself is not edited by this ADR; this is a new consumer of the same, already-accepted pattern.

### Ordering is the security boundary, not new authentication

`Bookings` transitions `Held → PendingPayment` **before** making the HTTP call to `Payments` — not after, not as part of the same request without ordering. Because `IBookingCheckoutEligibilityQuery` requires `PendingPayment` specifically (not `Held`), a booking can only ever become eligible for a `Payments`-side PaymentIntent by first passing through `Bookings`' own checkout handler. This closes the obvious bypass concern (something other than `Bookings` calling `Payments`' public endpoint directly for a booking still sitting in `Held`) **without needing any new service-to-service authentication scheme** — the state-machine ordering itself is the boundary. `Payments`' `/api/payments/intents` endpoint still requires a valid `Student` JWT (unchanged auth posture), and in addition now requires the referenced booking to already be `PendingPayment`.

### Idempotency, refined from `ADR-0015` §3

`Bookings` **always** calls `Payments` on a checkout attempt where the booking is `Held` or `PendingPayment` without a resolved outcome — it does **not** short-circuit before calling `Payments` on a retry, as `ADR-0015` §3 originally specified. This is a necessary refinement, not an inconsistency: `Payments`' `client_secret` is never persisted by `Bookings` (`ADR-0018` §2), so a retry (e.g. the frontend reloaded before completing the Stripe challenge) has no cached `client_secret` to fall back to — the only way to give the frontend a usable `client_secret` again is to ask `Payments` for it again. Safety against duplicate Stripe PaymentIntents comes entirely from the **idempotency key** (`ADR-0015` §3's `BookingId`-derived key, `ADR-0018` §2), not from `Bookings` avoiding the call: `Payments`' create-or-retrieve call uses the same deterministic key on every attempt for the same `BookingId`, so Stripe itself returns the *same* PaymentIntent (and a valid `client_secret` for it) on every retry, never a duplicate.

Concretely, `Bookings`' checkout handler becomes:

1. Read booking; ownership/existence check (unchanged).
2. If `Status == Held`: conditionally transition to `PendingPayment` (`WHERE Status = 'Held'`, unchanged mechanism, `ADR-0013` §3). If `Status` is already `PendingPayment`, skip this step — it was already done by an earlier attempt.
3. In either case, call `Payments`' `/api/payments/intents` with `{ BookingId, StudentUserId }`.
4. Relay `Payments`' response (`{ PaymentReferenceId, ClientSecret }`) to the frontend; store `PaymentReferenceId` on the `Booking` row (unchanged — `Booking.PaymentReferenceId` already exists for exactly this).
5. If step 3 fails (network error, `Payments` unavailable, Stripe error) after step 2 already committed: the booking is left in `PendingPayment` with no `PaymentReferenceId` yet. This is **not** a new failure mode requiring new recovery machinery — it is exactly the scenario `ADR-0013` §2's correction (widening the lazy expiry sweep to cover `PendingPayment`, not just `Held`) already exists to handle: the booking self-heals via the existing 10-minute TTL sweep if the student never successfully retries. A retry before that point re-enters at step 3 (step 2 is a no-op, already `PendingPayment`).

This refines, but does not contradict, `ADR-0015` §3's checkout-idempotency section — `BookingId` is still the idempotency key; `Payments` is still never called while a booking is `Held`'s sibling-but-wrong state; the only change is that "already `PendingPayment`" no longer means "skip calling `Payments`," it means "skip re-transitioning the status, but still call `Payments`."

### Refunds: confirmed as a plain integration event, not part of this mechanism

The refund-request call was never genuinely coupled to the checkout decision (see the project's own comparative analysis: a refund request does not need an immediate synchronous response — the cancel endpoint already returns `200 Cancelled` to the user synchronously on `Bookings`' own transition, and `Booking.Status → Refunded` was already documented as webhook-driven and eventually consistent, `ADR-0015` §5/§6). **Confirmed:** `Bookings` publishes a `RefundRequested`-shaped integration event (`TutorFlow.Shared`, `IIntegrationEvent`) when a cancellation or no-show warrants a refund per the already-locked policy (`ADR-0015` §6); `Payments` consumes it asynchronously, processes the Stripe refund, and publishes `PaymentRefunded` back (already specified, `PAYMENTS_SPEC.md` §16) once Stripe confirms it. This resolves `ADR-0015` §5's "exact transport decided when both modules exist" — it is now decided: an integration event, exactly like every other `Bookings`↔`Payments` outcome notification.

## What this ADR does not decide

- The commission/take-rate business question (`ADR-0018` §1, `docs/OPEN_QUESTIONS.md` #34) — unaffected by this decision.
- The Stripe Connect charge-type confirmation (`ADR-0018` §1, `docs/OPEN_QUESTIONS.md` A32) — unaffected.
- Any Stripe-specific mechanics once a call reaches `Payments` (`ADR-0018` already covers that) — unaffected.

## Consequences

- No `ProjectReference` is added between `Payments` and `Bookings`, in either direction.
- The only new `TutorFlow.Shared` contract is `IBookingCheckoutEligibilityQuery` — read-only, matching `ADR-0014`'s existing scope exactly. `ADR-0014` itself is not widened or edited.
- `Bookings` needs an `IHttpClientFactory`-based client for calling `Payments`' API — a new, small piece of `Bookings`-side infrastructure, consistent with `CLAUDE.md`'s own anti-pattern guidance (`IHttpClientFactory`, never `new HttpClient()`) and requiring no new library.
- `ADR-0015` §3's checkout-idempotency description is refined (not contradicted) per "Idempotency, refined from `ADR-0015` §3" above — `ADR-0015` itself is updated to reflect this.
- `ADR-0015` §5's refund-transport question is resolved: a plain integration event.
- `BOOKINGS_SPEC.md` §4 endpoint 5 (checkout) is updated to describe the concrete mechanism above, replacing its prior "cannot be fully implemented, returns `501`" framing — see that document for the updated endpoint spec.
- This is now an explicit architectural decision available for `Payments`/`Bookings` implementation. It does not, by itself, implement anything — `Bookings`' checkout handler still does not actually call `Payments` today; this ADR specifies what it must be changed to do.
