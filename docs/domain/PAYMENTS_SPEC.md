# Payments Module — Implementation Specification

Status: **specification/review-readiness — not implemented.** This document, together with `ADR-0018` and `ADR-0019`, is the complete design for the `Payments` module's first implementation slice (Stripe Connect Express, PaymentIntent-based student payment, webhook-authoritative confirmation, full refunds only). It does not implement `Payments`, and it does not change `Bookings`' code, migrations, or schema.

**Revision note (2026-10-04):** `ADR-0019` is now **Accepted** (Option B, Bookings-orchestrated: a server-to-server HTTP call from `Bookings` to `Payments`, validated by a new read-only `IBookingCheckoutEligibilityQuery`; refund-request confirmed as a plain integration event). Every `[BLOCKED]` marker in the original version of this document is resolved below. No code, migration, or schema change accompanies this update.

Every section below distinguishes three kinds of statements, matching `BOOKINGS_SPEC.md`'s convention:

- **Accepted** — already decided by an existing ADR; restated here for completeness.
- **Specified** — a new technical/architectural decision this document proposes (tracked in `ADR-0018`/`ADR-0019`, requiring sign-off).
- **`[OPEN — Product Owner]`** — a business-policy question this document deliberately does not answer.

## 0. What Payments owns and does not own

**Owns:** the `Payment` aggregate (one per PaymentIntent-creation attempt), the Payment state machine, the refund lifecycle for a given `Payment`, read-only observation of Stripe's dispute lifecycle, the `TutorConnectedAccount` entity (Stripe Connect Express onboarding/capability state), the webhook event log/audit trail, all Stripe-specific identifiers (`StripePaymentIntentId`, `StripeConnectedAccountId`) and API interaction, idempotency-key management for every Stripe call, and currency/amount validation against V1's EUR-only configuration.

**Does not own:** the `Booking` aggregate or its state machine (`Bookings`); the agreed price (`Bookings`' snapshot — `Payments` receives amount/currency as input parameters at intent-creation time, never computes or re-derives them, and never queries `Bookings`' database); tutor profile/verification (`Tutors`); earnings-accrual timing or payout scheduling/fees beyond Stripe Connect Express's own default behavior (deferred, `docs/OPEN_QUESTIONS.md` #12/#13); tax/invoicing (deferred, #29); any Booking-level dispute concept (resolved as not existing, `BOOKINGS_SPEC.md` §10). Every cross-module reference is a plain `Guid` (Identity `UserId`, or `Bookings`' `BookingId` used opaquely) — never an EF foreign key or navigation property, and no project reference to `TutorFlow.Modules.Bookings` is added.

## 1. Domain model

### 1.1 Aggregate root: `Payment`

One row per PaymentIntent-creation attempt. Cardinality with `Booking` is **1 : 0-or-1, and never reused** — see §3.

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK. Its string form (`Id.ToString()`) *is* the opaque `PaymentReferenceId` `Bookings` stores — never the literal Stripe ID (`ADR-0018` §2) |
| `BookingId` | `Guid` | External reference to `Bookings`, no FK. **Unique** — at most one `Payment` row ever exists per `BookingId` (§3) |
| `StripePaymentIntentId` | `string` | Payments-internal only; never crosses the module boundary. Set once, at creation, immutable thereafter |
| `StudentUserId` | `Guid` | Identity `UserId` — external reference, no FK (who is being charged) |
| `TutorUserId` | `Guid` | Identity `UserId` — external reference, no FK (who the destination-charge transfer routes to) |
| `AmountMinorUnits` | `int` | Snapshot taken from the create-call's input parameters at creation time; never re-derived afterward |
| `Currency` | `string` (3) | ISO 4217; EUR-only enforced at the application/config layer in V1, not type-level (same convention as `Booking.PriceCurrency`) |
| `Status` | `PaymentStatus` | See §2 |
| `IdempotencyKey` | `string` | The key passed to Stripe's create-intent call, deterministically derived from `BookingId` (`ADR-0018` §2) |
| `ConfirmedAtUtc` | `DateTimeOffset?` | |
| `FailedAtUtc` | `DateTimeOffset?` | |
| `FailureReason` | `string?` | Stripe's decline/failure code, for audit/admin visibility |
| `RefundRequestedAtUtc` | `DateTimeOffset?` | Set when `Bookings` requests a refund via the `RefundRequested` integration event (`ADR-0019`) |
| `RefundedAtUtc` | `DateTimeOffset?` | |
| `CreatedAtUtc` / `UpdatedAtUtc` | `DateTimeOffset` | Standard bookkeeping, matching every other module |

No `ClientSecret` column — Stripe's `client_secret` is used once, synchronously, at creation, then discarded (`ADR-0018` §2). No `RefundAmountMinorUnits` column — full-refund-only means a refund is always for the complete `AmountMinorUnits`; there is no amount parameter to store (`ADR-0004`, `ADR-0015` §6).

### 1.2 `TutorConnectedAccount`

One row per tutor who has ever started Stripe Connect Express onboarding — independent of `Payment`, its own lifecycle (§9).

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `TutorUserId` | `Guid` | Identity `UserId` — external reference, no FK. **Unique** |
| `StripeConnectedAccountId` | `string` | **Unique** |
| `OnboardingStatus` | `ConnectedAccountStatus` | See §9 |
| `ChargesEnabled` | `bool` | Mirrors Stripe's `charges_enabled` flag |
| `PayoutsEnabled` | `bool` | Mirrors Stripe's `payouts_enabled` flag — this, not `ChargesEnabled`, is what gates whether a destination-charge transfer can route to this tutor |
| `CreatedAtUtc` / `UpdatedAtUtc` | `DateTimeOffset` | |

### 1.3 `PaymentWebhookEvent`

Append-only audit/idempotency log — one row per **received** Stripe webhook event, regardless of whether it changed a `Payment` row. See §13.

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `StripeEventId` | `string` | **Unique** — the webhook-level idempotency/dedup key (`ADR-0018` §3) |
| `EventType` | `string` | e.g. `"payment_intent.succeeded"` |
| `PaymentId` | `Guid?` | Opaque reference to `Payment.Id`, nullable — not every event relates to an existing `Payment` (e.g. `account.updated` relates to `TutorConnectedAccount`) |
| `ReceivedAtUtc` | `DateTimeOffset` | |
| `ProcessedAtUtc` | `DateTimeOffset?` | Null if receipt was logged but processing hasn't completed yet |
| `RawPayloadJson` | `string` | The full Stripe event JSON. Safe to store in full — Stripe's webhook payloads never contain raw PAN/CVC in the first place (tokenization happens entirely client-side before TutorFlow's backend ever sees a payment method), so no scrubbing is required |

### 1.4 Value objects

- **`Money`** (`AmountMinorUnits: int`, `Currency: string`) — a **module-local** value object, mirroring `Tutors.Domain.Money`/`Bookings.Domain.Money` exactly. Not promoted to `TutorFlow.Shared`; `A13` remains explicitly deferred (`docs/domain/DOMAIN_MODEL.md`).

### 1.5 Invariants

- `AmountMinorUnits > 0`.
- `BookingId` is set once at creation, immutable, and unique across all `Payment` rows (§3).
- `StripePaymentIntentId` is set once Stripe's create-intent call succeeds, immutable thereafter.
- Status transitions only move forward per §2's table, enforced via conditional `UPDATE ... WHERE Status = @expected` (the same pattern as `ADR-0013` §3, not load-then-mutate) — never a domain-method mutation that could race.
- `RefundRequestedAtUtc`/`RefundedAtUtc` are only ever set when `Status` is in the refund lineage (`Succeeded → RefundPending → Refunded`).
- `TutorConnectedAccount.TutorUserId` is unique — at most one connected account per tutor.
- `PaymentWebhookEvent.StripeEventId` is unique — the DB enforces the dedup guarantee, not just application logic.

## 2. Payment state machine

**Accepted skeleton** (`docs/domain/STATE_MACHINES.md`, now made authoritative here): `Created → RequiresAction → Processing → Succeeded | Failed`; `Succeeded → RefundPending → Refunded`; `Succeeded → Disputed → UnderReview → Won | Lost`.

### Transition table

| From | To | Trigger | Mechanism |
|---|---|---|---|
| — | `Created` | `Bookings` calls `Payments`' `POST /api/payments/intents` (server-to-server HTTP, `ADR-0019`) after transitioning the booking to `PendingPayment` | `Payments` validates via `IBookingCheckoutEligibilityQuery` (`ADR-0019`), creates a Stripe PaymentIntent, persists the `Payment` row at `Created`, returns `{ PaymentReferenceId, ClientSecret }` |
| `Created` | `RequiresAction` | Stripe determines an SCA/3DS challenge is needed | Stripe webhook `payment_intent.requires_action` |
| `Created` / `RequiresAction` | `Processing` | Payment method submitted; Stripe processing (not all payment methods pass through this state) | Stripe webhook `payment_intent.processing` |
| `Created` / `RequiresAction` / `Processing` | `Succeeded` | Payment captured | Stripe webhook `payment_intent.succeeded` — `Payments` transitions, then publishes `PaymentSucceeded` to `Bookings` |
| `Created` / `RequiresAction` / `Processing` | `Failed` | Payment declined, failed, or abandoned | Stripe webhook `payment_intent.payment_failed` — `Payments` transitions, then publishes `PaymentFailed` to `Bookings`. **Treated as terminal from `Bookings`' perspective always** — V1 does not support in-place card retry on the same `Booking`/`Payment`; this matches `Bookings`' own already-locked behavior exactly (`ADR-0015` §2: `PendingPayment → Cancelled` on any failure signal), restated here for consistency, not a new decision |
| `Succeeded` | `RefundPending` | `Bookings` publishes a `RefundRequested` integration event (`ADR-0019`) | `Payments` consumes it asynchronously, validates the request (full amount only, `ADR-0004`), and calls Stripe's refund API |
| `RefundPending` | `Refunded` | Stripe confirms the refund | Stripe webhook `charge.refunded` (or `refund.updated` → succeeded) — `Payments` transitions, publishes `PaymentRefunded` to `Bookings` |
| `RefundPending` | `Succeeded` (rollback) | Stripe's refund attempt itself fails (e.g. insufficient connected-account balance to reverse the transfer) | Stripe webhook `refund.failed` — reverts to `Succeeded`, surfaced to Admin for manual intervention (§15) |
| `Succeeded` | `Disputed` | Customer initiates a chargeback with their card issuer | Stripe webhook `charge.dispute.created` |
| `Disputed` | `UnderReview` | Admin begins working the case **in Stripe's own dashboard** (manual, `ADR-0004`) | Stripe webhook `charge.dispute.updated` — **webhook-driven, not an internal Admin-triggered transition**; TutorFlow never drives this transition itself, only observes it (see §15 — V1 Admin surface is read-only) |
| `UnderReview` | `Won` | Stripe/card network rules in the platform's favor | Stripe webhook `charge.dispute.closed`, status `won` |
| `UnderReview` | `Lost` | Stripe/card network rules against the platform | Stripe webhook `charge.dispute.closed`, status `lost` — funds are already returned to the customer via the card network; no TutorFlow-side refund action is needed or triggered |

### Terminal states

`Failed`, `Refunded`, `Won`, `Lost` have no outbound edge. `Succeeded` is **not** terminal (can reach `RefundPending` or `Disputed`). All transitions into `Succeeded`/`Failed`/`Refunded`/`Disputed`/`UnderReview`/`Won`/`Lost` are **strictly webhook-driven** (`ADR-0004`'s non-negotiable requirement, restated, now also covering `UnderReview` explicitly — `STATE_MACHINES.md`'s original note didn't list it, but the same rule applies for the same reason). There is **no internal/Admin-triggered `Payment.Status` transition anywhere in V1** — every actual remediation (refund override, dispute evidence, connected-account troubleshooting) happens in Stripe's own dashboard by design (`ADR-0004`'s manual-Admin decision), and TutorFlow's `Payments` module only ever observes and reflects Stripe's state via webhooks.

### Coupling to the Booking state machine

**Accepted** (`ADR-0004`, `ADR-0015`): `Payment` and `Booking` are independent state machines that react to each other only through integration events — neither reaches into the other's table. The known edge cases where this independence surfaces an eventual-consistency wrinkle (restated from `ADR-0015` §4, not new): a `payment_intent.succeeded` webhook can arrive *after* `Bookings`' own expiry sweep has already moved a booking to `Expired` (the booking's 10-minute TTL can elapse before a slow SCA/3DS challenge completes) — `Payments` still correctly transitions its own `Payment.Status` to `Succeeded` and publishes `PaymentSucceeded` regardless; it is `Bookings`' idempotent-webhook-processing code that detects "booking already `Expired`, payment succeeded anyway" as a genuine conflict requiring manual remediation. The exact remediation policy (auto-refund? manual review?) remains `[OPEN — Product Owner]`, unchanged from `ADR-0015` §4.

## 3. Payment ↔ Booking relationship and cardinality

**One `Booking` has at most one `Payment`, ever — never reused, never retried in place.** `Payment.BookingId` is unique at the database level.

This follows directly from `Bookings`' already-locked design, not a new decision: `CheckoutBooking`'s idempotency model (`ADR-0015` §3, refined by `ADR-0019`) calls `Payments` on *every* checkout attempt for a booking that's `Held` or already `PendingPayment` — but `Payments`' own idempotency key (`BookingId`-derived, `ADR-0018` §2) guarantees this never produces more than one Stripe PaymentIntent, and therefore never more than one `Payment` row, per booking (the create-or-retrieve call either creates the one-and-only row or returns the existing one unchanged). And any `payment_intent.payment_failed` outcome moves the booking to `Cancelled` (§2 above), which is terminal with respect to payment — there is no transition back to `Held`. A student whose card is declined cannot retry the *same* booking; they must create a new trial-hold (a new `Booking` row, going through the full hold-creation/availability-check flow again), which would then get its own, separate `Payment` row. This is consistent with `BOOKINGS_SPEC.md` §6's existing stance ("the handler does not automatically retry... returns `409` and leaves the student to pick a different slot") applied to payment failure instead of a slot conflict.

## 4. PaymentIntent lifecycle

Covered in full in `ADR-0018` §1–§2. Summary: destination charges (platform is merchant of record, `transfer_data.destination` = tutor's connected account), `application_fee_amount` **not yet specifiable** (commission/take-rate undecided, §H), idempotency key derived from `BookingId`, amount/currency obtained from `IBookingCheckoutEligibilityQuery` — never from the calling HTTP request (`ADR-0019`) — and never re-derived afterward, `client_secret` never persisted. The triggering call is `Bookings`' server-to-server `POST /api/payments/intents` request (`ADR-0019`), made only after `Bookings` has already transitioned the booking to `PendingPayment`.

## 5. Idempotency model

Four distinct surfaces, each already individually specified above or in `ADR-0015`/`ADR-0018`; consolidated here:

| Surface | Mechanism |
|---|---|
| PaymentIntent creation (`Bookings` → `Payments`, server-to-server HTTP, `ADR-0019`) | `Idempotency-Key` header derived from `BookingId` on the Stripe API call — `Bookings` calls `Payments` on *every* attempt including retries (`ADR-0019`, refining `ADR-0015` §3), and this key is what guarantees a retry returns Stripe's *same* PaymentIntent, never a duplicate |
| Webhook processing (Stripe → `Payments`) | Dedup by `PaymentWebhookEvent.StripeEventId` (DB-unique) *and* by conditional-`UPDATE`-on-`Payment.Status` — defense in depth, matching `Bookings`' own stated philosophy from the other side (`ADR-0018` §3) |
| Refund request (`Bookings` → `Payments`, `RefundRequested` integration event, `ADR-0019`) | `Idempotency-Key` derived from `BookingId` on `Payments`' own Stripe refund call — only one refund is ever possible per booking under full-refund-only V1 policy, so no discriminator beyond `BookingId` is needed. A redelivered `RefundRequested` event is additionally absorbed by the `Succeeded → RefundPending` conditional-`UPDATE` guard (§2) |
| Outbound events (`Payments` → `Bookings`) | `Bookings`' consumers (`ConfirmBooking.Handler`, `ApplyRefund.Handler`) are already idempotent via their own conditional `UPDATE`s (built in the Bookings implementation slice) — `Payments`' publishing side must still use a transactional outbox (via Wolverine, once introduced, §H) so an event is never lost between DB-commit and publish on a crash |
| `IBookingCheckoutEligibilityQuery` (`Payments` → `Bookings`, DI, `ADR-0019`) | Inherently idempotent — a read-only query, called fresh on every checkout attempt, never mutates anything |

## 6. Webhook event model and processing rules

Full detail in `ADR-0018` §3. Summary of the pipeline: verify signature (reject `400` on mismatch, before anything else) → check `StripeEventId` against the event log (short-circuit `200` if already processed) → dispatch by `event.type` → each handler performs its own conditional-`UPDATE` transition → record the event as processed and publish any resulting outbound event (same transaction/outbox) → return `200` only after all of that commits. Unrecognized event types are logged and acknowledged, not errored. Transient failures after signature verification must surface as `5xx` so Stripe redelivers — never swallowed into a `200`.

Out-of-order delivery (Stripe does not guarantee webhook ordering) is tolerated by construction: each handler's `WHERE Status = @expected` guard naturally rejects an out-of-order transition attempt rather than corrupting state. A rejected-but-not-erroring case should be logged for visibility, not silently dropped — Stripe will keep retrying regardless, and the *correct* eventual event (once prior state catches up) will still apply cleanly.

## 7. Refund model and state transitions

Covered in §2's table (`Succeeded → RefundPending → Refunded`, with the `RefundPending → Succeeded` rollback path on Stripe-side refund failure). **Full-refund-only** (`ADR-0004`, `ADR-0015` §6): there is no amount parameter anywhere in the refund request or the `Payment` schema — a refund is always for the complete `AmountMinorUnits`. The *trigger* for a refund request originates from `Bookings`' already-locked policy (cancellation ≥24h before `StartUtc`, or a tutor no-show) — `Payments` does not decide *whether* a refund is warranted, only processes the request once made (`ADR-0015` §5/§6).

## 8. Stripe Connect Express connected-account lifecycle

### `ConnectedAccountStatus` states

`NotStarted → OnboardingIncomplete → PendingVerification → Active`, with `Active → Restricted` if Stripe later detects an outstanding requirement or disables a capability.

### Transitions

| From | To | Trigger |
|---|---|---|
| — | `NotStarted` | Implicit — no `TutorConnectedAccount` row exists yet for this tutor |
| `NotStarted` | `OnboardingIncomplete` | Tutor calls `POST /api/payments/connected-accounts` (§E) — `Payments` creates a Stripe Express account (`stripe.Accounts.Create`, type `express`) and an Account Link, returns Stripe's hosted onboarding redirect URL |
| `OnboardingIncomplete` | `PendingVerification` → `Active` | Stripe webhook `account.updated`, reflecting `details_submitted`/`charges_enabled`/`payouts_enabled` flags changing as the tutor completes Stripe's hosted KYC flow — entirely Stripe-hosted, no TutorFlow-side verification logic |
| `Active` | `Restricted` | Stripe webhook `account.updated` showing a capability disabled or a requirement newly overdue |

This is a genuinely separate, account-level state machine from `Payment`'s own per-transaction one — the same kind of independence already established between `Booking` and `Payment` (`ADR-0004`), applied one level further.

## 9. Tutor onboarding/payment capability gating

**Specified, V1 behavior — "fail late, at checkout":** whether a tutor has completed Stripe Connect Express onboarding is checked only at PaymentIntent-creation time (inside `Payments`' `POST /api/payments/intents` handler, `ADR-0019`), not earlier in the booking flow. `Availability`'s existing `TutorBookability` read-model (`ADR-0011`) is driven solely by `Tutors`' publish-on-`Published` event and has **no relationship to Stripe onboarding status** — a `Published`, bookable tutor who hasn't finished Connect onboarding can still be trial-held and can still reach checkout; the checkout call fails there (tutor's connected account not `PayoutsEnabled`), and the booking simply remains `Held` (never advances to `PendingPayment`) until either the tutor finishes onboarding and the student retries, or the hold expires naturally.

This is a deliberate V1 simplification, not a conflict: making tutor payment-capability a *bookability* gate would require `Payments` to publish a new event `Availability` consumes (a new integration this ADR does not design), purely to improve a UX edge case (a student discovers late that a tutor can't yet be paid). **Flagged as a future enhancement, not specified further here.**

## 10. Payment failure and retry behavior

Already fully covered: any `payment_intent.payment_failed` webhook is always treated as terminal for that `Booking`/`Payment` pair (§2, §3) — no in-place retry, no automatic re-attempt with a different payment method, consistent with `Bookings`' existing "do not invent a suggest-an-alternative feature" stance (`BOOKINGS_SPEC.md` §6).

## 11. SCA/3DS flow

`RequiresAction` exists specifically for this (`ADR-0004`, `STATE_MACHINES.md`). Mechanically: the PaymentIntent is created with Stripe's standard confirmation flow; if Stripe determines a 3DS challenge is required, the PaymentIntent's status becomes `requires_action` and the **client** (Stripe.js/Stripe Elements, entirely frontend) handles the challenge using the `client_secret` returned once at creation. `Payments`' backend has no 3DS-specific mechanics beyond returning the `client_secret` in its synchronous response to `Bookings` (which relays it to the frontend, `ADR-0019`) and waiting for the eventual webhook outcome (`Succeeded`/`Failed`) — there is nothing further for the backend to do during the challenge itself.

## 12. Currency/amount representation

Integer minor units + ISO 4217 string(3) — identical convention to `Booking.PriceAmountMinorUnits`/`PriceCurrency`. EUR-only is enforced at the application/configuration layer in V1 (e.g. rejecting a create-intent call whose currency isn't `"EUR"`), not hardcoded into the `Payment` entity's type — the column stays a plain `string(3)`, consistent with `ADR-0004`'s "keep the model extensible" requirement. `Payment.AmountMinorUnits`/`Currency` must equal whatever `Bookings` passes in the create-intent call (itself equal to `Booking.PriceAmountMinorUnits`/`PriceCurrency` at the moment checkout was called) — `Payments` never re-derives or queries for these values independently.

## 13. Audit requirements

- `PaymentWebhookEvent` (§1.3) is the primary audit trail — an append-only, complete record of every Stripe webhook ever received, including the full raw payload, regardless of whether it changed anything. This is sufficient for reconstructing "what did Stripe tell us, and when" for any payment or dispute investigation.
- Every `Payment.Status` transition is traceable: webhook-driven transitions via the event log above; the *outcome* of the single non-webhook-driven step (create-intent) is traceable via the `Payment` row's own `CreatedAtUtc`/`Status` history (no separate log needed for a single, one-time creation step).
- **Not decided here:** data-retention policy for payment records (how long `PaymentWebhookEvent.RawPayloadJson` and `Payment` rows are kept) — this is a privacy/compliance question adjacent to but distinct from #29 (VAT/tax), not invented in this specification. Flagged as a new open item (§I).

## 14. Security requirements

- **Never handle raw card data, by construction** — Stripe Elements/Stripe.js tokenizes all card input entirely client-side; TutorFlow's backend never receives a PAN/CVC at any point, so there is no raw-card-handling code path to secure in the first place. This is an architectural property, not a policy to remember.
- **Stripe secret API key and webhook signing secret** are supplied via .NET user-secrets / environment variables, exactly the convention already established for `Jwt:Key` and the local Postgres dev credentials (`docs/DEVELOPMENT_DATABASE.md`) — never in `appsettings.json`.
- **Webhook signature verification is mandatory and unconditional** before any processing (`ADR-0018` §3), using Stripe's own SDK verification helper (constant-time comparison, built-in replay-window tolerance).
- All Stripe API calls are over TLS, inherent to Stripe's SDK — nothing TutorFlow configures.
- Application logs must never contain a `client_secret`, the Stripe secret key, or the webhook signing secret, even though none of them are raw card data.
- Admin endpoints (§15) require an authorization policy — **this policy does not exist yet** (§H: Identity has no `Admin` role defined anywhere in the codebase today, only `Student`/`Tutor`). A dedicated finance/payments Admin sub-role is *recommended* but `docs/OPEN_QUESTIONS.md` #2 (Admin sub-roles) remains unresolved — V1 Admin payment endpoints should gate on a general `Admin` capability once one exists, with finer-grained sub-roles deferred, consistent with not inventing #2's resolution.

## 15. Admin payment/dispute operations required for V1

**V1 Admin surface is read-only.** Because dispute-evidence handling is manual, through Stripe's own dashboard (`ADR-0004`), and every actual remediation action (refund override, evidence submission, connected-account troubleshooting) happens there — not in TutorFlow — there is no write/action endpoint to specify for V1 beyond visibility:

- View a `Payment`'s details and current status, by `PaymentId` or `BookingId`.
- List/filter `Payment`s (by student, tutor, status).
- View dispute status for a `Payment` (reflects Stripe's state via webhooks — read-only).
- View a tutor's `TutorConnectedAccount` onboarding/capability status (helps Admin support a tutor stuck mid-onboarding).

**Not specified:** any write-capable admin action (e.g. a manual refund override for an edge case). This may be worth adding in a future iteration but is not invented here, consistent with "do not invent additional business policies."

## 16. Integration events/contracts with Bookings

### Outbound (`Payments` publishes → `Bookings` consumes)

```csharp
// TutorFlow.Shared (shape only — not implemented until Wolverine is introduced)
public sealed record PaymentSucceeded(Guid PaymentReferenceId, Guid BookingId, Guid EventId, DateTimeOffset OccurredAtUtc) : IIntegrationEvent;
public sealed record PaymentFailed(Guid PaymentReferenceId, Guid BookingId, string? Reason, Guid EventId, DateTimeOffset OccurredAtUtc) : IIntegrationEvent;
public sealed record PaymentRefunded(Guid PaymentReferenceId, Guid BookingId, Guid EventId, DateTimeOffset OccurredAtUtc) : IIntegrationEvent;
```

- `PaymentSucceeded` → triggers `Bookings`' existing `ConfirmBooking.Handler` (already built, already idempotent — no `Bookings`-side change needed beyond a new consumer wrapper, §H).
- `PaymentRefunded` → triggers `Bookings`' existing `ApplyRefund.Handler` (already built, already idempotent — same, no change beyond a new consumer wrapper).
- `PaymentFailed` → **no corresponding `Bookings`-side handler exists yet.** `ADR-0015` §2's table says this should drive `PendingPayment → Cancelled`, the same transition `CancelBooking.Handler` performs — but that handler requires a `callerUserId` for ownership-checking and the `CancelledByUserId` audit field, and a webhook has no such caller. **New implementation prerequisite, not a conflict** (§H): `Bookings` needs a new internal handler (or a system-triggered path through `CancelBooking`) for webhook-triggered cancellation, with `CancelledByUserId` left `null` or a sentinel "system" value — this is a small, additive `Bookings`-side change, not a change to `CancelBooking`'s existing shape.

### Inbound (`Bookings` → `Payments`)

- **PaymentIntent-creation request (checkout)**: a synchronous, server-to-server HTTP call, `POST /api/payments/intents`, made by `Bookings` (never the frontend) after it has already transitioned the booking to `PendingPayment` (`ADR-0019`). Request: `{ BookingId, StudentUserId }` — **never amount or currency**. Response: `{ PaymentReferenceId: Guid, ClientSecret: string }`.
- **Refund request (cancel / no-show)**: a plain integration event, not a call:

  ```csharp
  // TutorFlow.Shared
  public sealed record RefundRequested(Guid BookingId, Guid PaymentReferenceId, Guid EventId, DateTimeOffset OccurredAtUtc) : IIntegrationEvent;
  ```

  Published by `Bookings` when its already-locked cancellation/no-show policy determines a refund is warranted (`ADR-0015` §6); consumed by `Payments` whenever, with no synchronous response expected — the cancel endpoint has already returned `200 Cancelled` to the user by this point.

### `Payments` → `Bookings`: the reciprocal read-only query

```csharp
// TutorFlow.Shared
public interface IBookingCheckoutEligibilityQuery
{
    Task<BookingCheckoutEligibility> ValidateAsync(Guid bookingId, Guid callerStudentUserId, CancellationToken ct);
}

public sealed record BookingCheckoutEligibility(
    bool IsEligible, string? Reason, int? AmountMinorUnits, string? Currency, Guid? TutorUserId);
```

Implemented by `Bookings`, consumed via DI by `Payments` — the same shape as `ADR-0014`'s existing three query contracts, a fourth instance, not a widening of that ADR's scope (read-only, idempotent, no side effect). `IsEligible` requires the booking to exist, be owned by `callerStudentUserId`, and be in status **`PendingPayment`** specifically (not `Held`) — this ordering requirement is what prevents a bypass of `Bookings`' own checkout handler (`ADR-0019`, "Ordering is the security boundary"). `Payments` uses `AmountMinorUnits`/`Currency` from this query, and only from this query, to create the Stripe PaymentIntent — never values supplied in the HTTP request body above.

### Prerequisite shared by all three outbound events

None of `PaymentSucceeded`/`PaymentFailed`/`PaymentRefunded` can be delivered until Wolverine is introduced — `ADR-0015`'s "Messaging infrastructure" section already anticipated this: *"Whichever module — `Bookings` or `Payments` — is implemented second is responsible for introducing Wolverine... since that is the first moment a real publisher and a real consumer coexist."* `Bookings` already exists; `Payments` is now that second module. Introducing Wolverine (with its transactional outbox) is therefore a `Payments`-side implementation prerequisite, not a new decision.

## 17. What Payments owns vs. what Bookings owns

See §0 above for the full list. One-line summary: `Payments` owns the money-movement machinery and its own three entities, including the Stripe API call itself and its idempotency key; `Bookings` owns the booking lifecycle, its own ownership/state checks, and the agreed price (which it exposes to `Payments` only via the read-only `IBookingCheckoutEligibilityQuery`, never by letting `Payments` trust a caller-supplied value); neither reaches into the other's schema or adds a `ProjectReference`. Checkout crosses the boundary via a synchronous HTTP call (`Bookings` → `Payments`, orchestrated entirely by `Bookings`); refunds cross it via a plain integration event (`ADR-0019`).

## 18. What remains deferred because #12/#13/#29 are open

- **No `Payout` entity/table in V1.** Destination-charge transfers to the tutor's connected account happen automatically at payment-success time (part of the charge itself); Stripe Connect Express then pays out to the tutor's bank **on Stripe's own default payout schedule**, entirely outside TutorFlow's code, unless and until #13 (payout frequency/minimum/fees) is resolved to customize that schedule. V1 ships "tutors get paid" correctly without building any payout-specific code, riding Stripe's own default rails.
- **No earnings-accrual/risk-window logic.** The destination-charge transfer happens immediately at payment success under Stripe's default behavior; if the business later wants to *withhold* funds until some risk window passes (rather than Stripe's immediate transfer), that requires either Stripe's delayed-transfer controls or a separate hold-and-release mechanism — not built now, deferred to #12's resolution.
- **No VAT/tax computation or invoice generation.** PaymentIntent amounts are charged exactly as `Bookings` snapshots them (the pro-rata trial price, `BOOKINGS_SPEC.md` §1.3), with no tax line item. Receipts beyond Stripe's own optional default receipt email are not built — deferred to #29's resolution.

---

## A. Payments domain specification

See §0, §1 above.

## B. Payments state machine

See §2 above.

## C. Stripe integration boundary

See `ADR-0018` in full. The cross-module call shape is now fully specified (`ADR-0019`): checkout is a synchronous server-to-server HTTP call, `Bookings → Payments`, orchestrated entirely by `Bookings` (the frontend only ever calls `Bookings`); refund-request is a plain integration event. `Payments` independently validates every checkout call via `IBookingCheckoutEligibilityQuery` (read-only, `Bookings`-owned) rather than trusting caller-supplied amount/currency. Internally, Stripe SDK calls are isolated behind a narrow `Payments`-internal abstraction (`ADR-0018` §4).

## D. Cross-module contracts/events

See §16 above.

## E. Required API endpoints

| # | Method & path | Auth | Purpose | Status |
|---|---|---|---|---|
| 1 | `POST /api/payments/intents` | `Student` role; caller is `Bookings` (server-to-server), never the frontend directly | Create-or-retrieve the Stripe PaymentIntent for a `PendingPayment` booking. Request: `{ BookingId, StudentUserId }`. Response: `{ PaymentReferenceId, ClientSecret }`. Validates via `IBookingCheckoutEligibilityQuery` — rejects if the booking isn't `PendingPayment`, isn't owned by the caller, or doesn't exist | Specified, `ADR-0019` |
| 2 | `POST /api/payments/webhooks/stripe` | None (Stripe signature instead of JWT) | Receive and process Stripe webhook events | Specified, `ADR-0018` §3 |
| 3 | `POST /api/payments/connected-accounts` | `Tutor` role, caller's own `UserId` | Create/resume Stripe Express onboarding; returns Stripe's hosted onboarding redirect URL | Specified, §8 |
| 4 | `GET /api/payments/connected-accounts/me` | `Tutor` role, caller's own `UserId` | Read the caller's own onboarding/capability status | Specified, §8 |
| 5 | *(none — event, not an endpoint)* | — | Refund request (cancel / no-show) — `RefundRequested` integration event, published by `Bookings`, consumed by `Payments`. No HTTP surface | Specified, `ADR-0019` |
| 6 | `GET /api/payments/admin/payments/{id}` | `Admin` (policy does not exist yet, §H) | Read-only payment detail | Specified, §15 |
| 7 | `GET /api/payments/admin/payments` | `Admin` | Read-only filtered list (by student/tutor/status) | Specified, §15 |
| 8 | `GET /api/payments/admin/disputes` | `Admin` | Read-only dispute visibility | Specified, §15 |
| 9 | `GET /api/payments/admin/connected-accounts/{tutorUserId}` | `Admin` | Read-only connected-account status, for tutor support | Specified, §15 |

Endpoint #2 (webhooks) is necessarily unauthenticated in the JWT sense — its "authentication" is Stripe signature verification (`ADR-0018` §3), not `[Authorize]`. Endpoint #1 still requires a valid `Student` JWT even though its only real caller is `Bookings`' own backend code (which forwards the original caller's identity) — the actual bypass protection is the `PendingPayment`-only eligibility check (`ADR-0019`), not a service-to-service auth scheme.

## F. Database entities/tables/indexes

Dedicated `payments` PostgreSQL schema, `PaymentsDbContext`, matching every prior module's `AddDbContext`/`HasDefaultSchema` convention.

- **`Payments`** — columns per §1.1. Indexes: `PK(Id)`; `UNIQUE(BookingId)`; `UNIQUE(StripePaymentIntentId)`; `IX(StudentUserId)`; `IX(TutorUserId)`. No index on `Status` alone at V1 scale (same reasoning as `Bookings`' own indexing decision — always queried alongside a `BookingId`/`StudentUserId`/`TutorUserId`, never scanned by status alone).
- **`TutorConnectedAccounts`** — columns per §1.2. Indexes: `PK(Id)`; `UNIQUE(TutorUserId)`; `UNIQUE(StripeConnectedAccountId)`.
- **`PaymentWebhookEvents`** — columns per §1.3. Indexes: `PK(Id)`; `UNIQUE(StripeEventId)`; `IX(PaymentId)` (nullable FK-less reference, for looking up a payment's full event history).

No exclusion constraint, no generated column, no `btree_gist` — `Payments` has no overlapping-interval concurrency problem (`ADR-0018` §4 Consequences). All concurrency safety here is unique-constraint- and conditional-update-based, simpler than `Bookings`' shape.

## G. Security and failure-handling requirements

See §10, §11, §14 above.

## H. Explicit list of implementation prerequisites

1. **`Bookings` needs a new `IHttpClientFactory`-based client calling `Payments`' `/api/payments/intents`** (`ADR-0019`) — does not exist today; `CheckoutBooking.Handler` currently returns `501` unconditionally for the `Held` branch and must be changed to perform the call described in `ADR-0019`'s refined ordering.
2. **`Bookings` needs to implement `IBookingCheckoutEligibilityQuery`** (`ADR-0019`) — the read-only query `Payments` will consume via DI.
3. **`Bookings` needs a new internal handler for webhook-triggered cancellation** (§16) — `CancelBooking.Handler` assumes a user caller; a system/webhook-triggered `PendingPayment → Cancelled` transition needs its own path, with `CancelledByUserId` null/sentinel. Small, additive change to `Bookings`, not a change to existing handler shapes.
4. **Wolverine must be introduced for the first time** (§16) — by `Payments`, per `ADR-0015`'s own anticipated sequencing. This is the first real publisher+consumer pair in the codebase, needed for `PaymentSucceeded`/`PaymentFailed`/`PaymentRefunded`/`RefundRequested`.
5. **Identity has no `Admin` role/policy today** (§14) — needed before the V1 Admin visibility endpoints (§15) can be properly authorized. Does not block anything else.
6. **Commission/take-rate is undecided** (`ADR-0018` §1) — blocks the exact `application_fee_amount` parameter on PaymentIntent creation. Not one of #12/#13/#29, but genuinely blocking and not invented here; tracked as a Product Owner item (`docs/OPEN_QUESTIONS.md` #34).
7. **Stripe Connect charge-type confirmation** — destination charges are *recommended* (`ADR-0018` §1) but not yet confirmed by the Architect as final (`docs/OPEN_QUESTIONS.md` A32).
8. **Data-retention policy for payment audit records** is undecided (§13) — does not block the first implementation slice, but should be resolved before production (`docs/OPEN_QUESTIONS.md` #35).

## I. Remaining architectural/product decisions that genuinely block implementation

**`ADR-0019` is resolved — no longer a blocker.** What remains:

- **Commission/take-rate** — Product Owner item (#34), blocks `application_fee_amount` specifically. Not #12/#13/#29.
- **Admin role/policy existence** — trivial to add, but doesn't exist yet; blocks Admin endpoint authorization specifically.
- **Stripe Connect charge-type** — recommended (destination charges) but not Architect-confirmed (A32).

**Explicitly not blocking** (already resolved, restated for clarity): the checkout/refund-request call mechanism (`ADR-0019`); Stripe Connect Express account type; EUR-only currency; dispute-evidence process (manual Admin); refund scope (full-refund-only); the cancellation/no-show policy that decides *whether* a refund is requested; SCA/3DS approach (delegated entirely to Stripe's client-side flow).

## J. Consistency with existing module conventions (verified against the actual codebase, 2026-10-04)

Every pattern below was confirmed against `Identity`/`Tutors`/`Availability`/`Bookings`' actual source, not assumed. `Payments`, when implemented, follows all of them with no deviation and no competing pattern introduced:

| Convention | Existing precedent (verified) | `Payments` follows it by |
|---|---|---|
| Local `Result`/`Result<T>`, not shared | `src/Modules/{Identity,Tutors,Availability,Bookings}/.../Result.cs` — four independent copies, confirmed, no shared base | A fifth local copy, same shape |
| Feature-file shape: `Command`/`Response`/`Validator`/`internal sealed class Handler(...)` | Every `Features/*/*.cs` file in all four modules | Same shape for every endpoint in §E |
| FluentValidation + `ValidationFilter<T>` endpoint filter | Local `Infrastructure/ValidationFilter.cs` per module (confirmed in all four) | A fifth local copy |
| `ClaimsPrincipalExtensions.TryGetUserId`, local `RoleNames`/`PolicyNames` | Duplicated per module by established, explicit convention (flagged repeatedly, never consolidated) | Same duplication — not a new pattern, not consolidated here either |
| One `DbContext` per module, dedicated schema, `ApplyConfigurationsFromAssembly` | `{Identity,Tutors,Availability,Bookings}DbContext.cs`, confirmed | `PaymentsDbContext`, schema `payments` |
| EF Core migrations via `dotnet ef migrations add`/`database update`, raw SQL only where EF has no fluent mapping | Established workflow; `Bookings`' migration mixes EF DDL with raw SQL only for the exclusion constraint | `Payments` needs **no** raw SQL at all — plain EF-mapped tables, simpler than `Bookings`' schema (`ADR-0018` §4 Consequences) |
| `internal` handlers + `AssemblyInfo.cs` with `[assembly: InternalsVisibleTo("...Tests")]` for test access | Confirmed present in both `Identity` and `Bookings` | Same pattern for `Payments.Tests` |
| xUnit v3 + Testcontainers + `WebApplicationFactory<Program>`, locally-minted JWTs | `{Identity,Tutors,Availability,Bookings}ApiFixture.cs` | A `PaymentsApiFixture`, same shape |
| **Zero `ProjectReference` between business modules** | Verified by grep across every module `.csproj` in the repository: none exist — only `TutorFlow.Shared` | `Payments.csproj` references only `TutorFlow.Shared` (+ the Stripe SDK, an external package, see below) — no reference to `Bookings`, `Tutors`, `Availability`, or `Identity` |
| No repository-abstraction over EF Core, no mediator library | Confirmed absent everywhere; `CLAUDE.md` anti-patterns list explicitly forbids both | Not introduced here either — handlers use `PaymentsDbContext` directly, no `Mediator`/MediatR |

### New infrastructure this specification requires, and why existing patterns don't cover it

Per the instruction to explain and pause before adding anything new: two things outside the existing pattern set are required, and both are consequences of **already-Accepted** decisions, not new asks:

1. **The Stripe .NET SDK (`Stripe.net`)** — no existing module pattern "solves" calling an external payment provider's API; this is the unavoidable, singular consequence of `ADR-0004` (Accepted, this session's earlier work) already choosing Stripe as the provider. There is no competing/alternative architecture being introduced — this is the one library that lets `Payments` do the one thing `ADR-0004` already decided it must do.
2. **Wolverine** (transactional outbox + messaging) — already named as the project's intended default in `CLAUDE.md`'s own Tech Stack section ("recommended default, not yet added... Add when the first cross-module event or first module with >5 features needs it"). `ADR-0015` already anticipated that whichever module is implemented second introduces it; `Payments` is now that module. This is not a new framework choice being made now — it is executing a choice the project already made before this session started.

Neither of these requires new approval in the sense of "should we use this at all" — that was already decided (`ADR-0004`, `CLAUDE.md`). What remains genuinely open is **how** `Bookings` and `Payments` call each other synchronously at all (`ADR-0019`) — which is a cross-module *call mechanism* question, not an infrastructure/library question.
