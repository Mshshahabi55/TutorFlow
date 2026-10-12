# Bookings Module — Implementation Specification

Status: **specification/review-readiness — not implemented.** This document, together with `ADR-0013` through `ADR-0017`, is the complete design for the `Bookings` module's first implementation slice (trial booking only — see §9 for sequencing). It does not implement `Bookings`, `Payments`, or `Lessons`.

**Revision note (architecture review, 2026-10-04):** a strict architecture review found one critical concurrency gap and several high-severity consistency/completeness issues in the original version of this specification and `ADR-0013`–`ADR-0017`. All are corrected in place below (and in the ADRs themselves) rather than tracked as a separate errata list — this document and its ADRs are the current, corrected design. §2 in particular is now the single authoritative Booking transition table; `docs/domain/STATE_MACHINES.md` and `ADR-0015` defer to it rather than restating their own.

**Revision note (Product Owner decisions, 2026-10-04):** the trial pricing formula, cancellation/reschedule notice windows, no-show rules, partial-refund scope, the per-student hold limit, and the Booking-side dispute question are now locked for V1 — see §10 for the consolidated list and the referenced sections/ADRs for each decision's detail and rationale. None are implemented by this update; each locked item explicitly states what the corresponding handler must still be changed to enforce.

Every section below distinguishes three kinds of statements:

- **Accepted** — already decided by an existing ADR; restated here for completeness.
- **Specified** — a new technical/architectural decision this document proposes (tracked in `ADR-0013`–`ADR-0017`, requiring sign-off).
- **`[OPEN — Product Owner]`** — a business-policy question this document deliberately does not answer.

## 0. What Bookings owns and does not own

**Owns:** the `Booking` aggregate, slot holds, the booking lifecycle/state machine, the `StudentUserId`/`TutorUserId` references (Identity `UserId`, never a FK), selected lesson duration, selected UTC start/end, cancellation/reschedule lifecycle, hold expiration, concurrency protection against double-booking, and booking-level history (via status timestamps, not implemented today as a full audit log).

**Does not own:** tutor profile (`Tutors`), availability rules/time off/booking settings (`Availability`), payment processing (`Payments`), lesson/classroom lifecycle (`Lessons`), subscription lifecycle (`Subscriptions`), credentials/roles (`Identity`). Every cross-module reference is a plain `Guid` (Identity `UserId`, or another module's own ID used opaquely) — never an EF foreign key or navigation property, matching every module built so far.

## 1. Domain model

### 1.1 Aggregate root: `Booking`

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `StudentUserId` | `Guid` | Identity `UserId` — external reference, no FK |
| `TutorUserId` | `Guid` | Identity `UserId` — external reference, no FK |
| `Status` | `BookingStatus` | see §2 |
| `DurationMinutes` | `int` | the selected lesson duration — a configured value (§3), not a hardcoded enum |
| `StartUtc` | `DateTimeOffset` | |
| `EndUtc` | `DateTimeOffset` | `StartUtc + DurationMinutes`; stored explicitly (not computed in C# on every read) because it backs the generated `during` range column (`ADR-0013`) |
| `HeldAtUtc` | `DateTimeOffset` | when the `Held` row was created |
| `HoldExpiresAtUtc` | `DateTimeOffset?` | set at creation (`HeldAtUtc` + configured TTL, §3); cleared (set null) once the booking leaves `Held`/`PendingPayment` |
| `ConfirmedAtUtc` | `DateTimeOffset?` | |
| `CancelledAtUtc` | `DateTimeOffset?` | |
| `CancelledByUserId` | `Guid?` | **Added** (architecture review, 2026-10-04) — the Identity `UserId` of whichever party (student or tutor) called the cancel endpoint; set if and only if `Status` is `Cancelled` or (having passed through `Cancelled`) `Refunded`. Without this, `Bookings` cannot answer "who cancelled" for notifications, reporting, or any future trust/abuse signal — a basic piece of the "booking-level history" this module already owns per §0, that the original field list omitted. |
| `CancellationReason` | `string?` | free text for now — no structured reason-code taxonomy invented |
| `PriceAmountMinorUnits` | `int` | **Changed from `int?`** (architecture review, 2026-10-04) — see §1.3: a price is guaranteed to exist before a `Booking` row is ever inserted (the hold-creation handler requires `ITutorPricingQuery` to return a price first, §4 endpoint 1), so this is never legitimately absent for any persisted row. Modeling it as non-nullable turns "a row was inserted without a price" from a silent possibility into a compile-time/constraint-time impossibility. |
| `PriceCurrency` | `string` (3) | **Changed from `string?`** — same reasoning as `PriceAmountMinorUnits` above |
| `PaymentReferenceId` | `string?` | opaque correlation with `Payments`, see `ADR-0015` |
| `SubscriptionReferenceId` | `Guid?` | opaque external reference to `Subscriptions`, for subscription-driven occurrences (`ADR-0010`) — not populated by anything in this slice, but the column exists so `Subscriptions` integration does not require a migration later |
| `RescheduledFromBookingId` | `Guid?` | see `ADR-0017` |
| `RescheduledToBookingId` | `Guid?` | see `ADR-0017` |
| `CreatedAtUtc` / `UpdatedAtUtc` | `DateTimeOffset` | standard bookkeeping, matching every other module's entities |

No `LessonId` column — see `ADR-0016` §4. No separate "hold" entity — a hold **is** a `Booking` row in `Held` status (`ADR-0003`, already accepted).

### 1.2 Value objects

- **`BookingStatus`** — enum, see §2. Stored as `character varying`, matching `Tutors`' and `Availability`'s existing enum-as-string convention.
- **`Money`** (`AmountMinorUnits: int`, `Currency: string`) — a **module-local** value object, mirroring `Tutors.Domain.Money` exactly (not promoted to `TutorFlow.Shared`; A13 remains explicitly deferred until `Payments`' design is reviewed — unchanged by this specification, see §10).

### 1.3 Why `Booking` stores money, and what it does not store

**Specified:** `Booking` stores a **price snapshot** (`PriceAmountMinorUnits` + `PriceCurrency`) captured from `Tutors`' current hourly rate (via `ITutorPricingQuery`, `ADR-0014`) at the moment a hold is created. This is required because tutor pricing can change after a booking is made, and the booking must remember what was agreed *for that booking* — independent of `Payments`' own bookkeeping.

This does **not** duplicate `Payments`' ownership of money: `Payments` (not implemented) will own the actual `PaymentIntent`/charge/refund amounts and all provider-specific state (`ADR-0004`); `Booking.PriceAmountMinorUnits` is the *agreed price*, used to request that amount from `Payments` and to display it, not a payment-processing record. Both follow the same system-wide "integer minor units + ISO currency, never floating point" rule (`CLAUDE.md`), which is a cross-cutting primitive-level rule, not something unique to `Payments`.

**Resolved — Product Owner decision, 2026-10-04:** the trial price is derived **pro-rata from the tutor's current hourly rate** by requested duration (`priceMinorUnits = round(hourlyRateMinorUnits * durationMinutes / 60)`), using deterministic minor-unit rounding (round-half-up, since partial cents cannot be charged). Platform-subsidized pricing and a flat trial price independent of the hourly rate are both explicitly **rejected** for V1. **Rationale:** pro-rata keeps a tutor's trial and full-rate pricing consistent (no separate "trial price" for a tutor to set or for the platform to subsidize), and is the simplest formula that needs no new configuration surface. This matches `Tutors.Infrastructure.TutorPricingQuery`'s existing implementation exactly — it was built as a placeholder pending this decision and requires no change now that the decision confirms it.

The formula lives entirely inside `Tutors`' implementation of `ITutorPricingQuery` (`ADR-0014`) — `Bookings` only ever calls the interface and stores the returned `TutorPrice` as a snapshot. This keeps the formula replaceable (e.g. if a future iteration wants tiered or platform-subsidized trial pricing) without any change to `Bookings`, the query contract, or the `Booking` schema.

### 1.4 Domain events (shapes only — not published until the consuming module exists and Wolverine is introduced)

| Event | Published when | Consumed by |
|---|---|---|
| `BookingConfirmed` | `PendingPayment`/`Held` → `Confirmed` | `Lessons` (`ADR-0016`); eventually `Notifications` |
| `BookingCancelled` | any → `Cancelled` | `Lessons`; eventually `Notifications` |
| `BookingExpired` | `Held`/`PendingPayment` → `Expired` (by the sweep, `ADR-0013`) | eventually `Notifications` (resolves `docs/OPEN_QUESTIONS.md` #15's *mechanism*, not its product answer) |

Exact payloads: `BookingConfirmed` is specified in full in `ADR-0016` §2. `BookingCancelled`/`BookingExpired` carry at minimum `BookingId`, `StudentUserId`, `TutorUserId`, and the relevant timestamp — not fully specified here since nothing consumes them yet; finalized when `Notifications`/`Lessons` are actually built.

### 1.5 Invariants

- A `Booking`'s `StartUtc` must be strictly before `EndUtc` (mirrors `WeeklyAvailabilityRule`/`TimeOff`'s existing start-before-end invariant pattern).
- `DurationMinutes` must be one of the currently-configured allowed trial durations (§3) — enforced at creation, not a database constraint (configuration can change; a stale booking's duration remains valid for *that booking* even if the configured set later changes).
- A tutor cannot have two `Held`/`PendingPayment`/`Confirmed` rows with overlapping `[StartUtc, EndUtc)` intervals — enforced by the database (`ADR-0013`, corrected 2026-10-04 to include `PendingPayment`), not solely in C#.
- **`HoldExpiresAtUtc` is non-null if and only if `Status` is `Held` or `PendingPayment`**, and must be cleared to `null` the instant a row transitions to `Confirmed`, `Cancelled`, `Expired`, or `Rescheduled`. This is restated from `ADR-0013` §1a as a hard precondition for that ADR's expiry sweep to remain correct — every handler that writes `Status` must write `HoldExpiresAtUtc` in the same statement, never leave it stale.
- `RescheduledToBookingId` is set if and only if `Status` is `Rescheduled`; `RescheduledFromBookingId` is set if and only if this row was itself created by a reschedule (independent of current `Status` — a rescheduled-into booking can later be cancelled or rescheduled again, and keeps the back-link regardless).
- `CancelledByUserId` and `CancelledAtUtc` are both non-null if and only if `Status` is `Cancelled`, or `Status` is `Refunded` (since the only path to `Refunded` passes through `Cancelled`, §2) — never set for any other status.
- `PriceAmountMinorUnits`/`PriceCurrency` are always non-null from creation onward — no transition ever clears them (the agreed price is retained for the lifetime of the row, including terminal states, for display/history purposes).

### 1.6 Persistence model

- Dedicated `bookings` PostgreSQL schema, `BookingsDbContext`, `DbSet<Booking> Bookings` — matching every prior module's `AddDbContext`/`HasDefaultSchema` convention exactly.
- A single `Bookings` table (no separate "Hold" table — holds and confirmed bookings are the same row across its lifecycle, per `ADR-0003`).
- Constraints and indexes: see `ADR-0013` §1 and §4 in full (exclusion constraint — corrected to span `Held`/`PendingPayment`/`Confirmed` — generated `during` column, `btree_gist` extension, plain indexes on `student_user_id`/`tutor_user_id`). The initial migration will contain both ordinary EF-generated DDL and a block of raw SQL for the PostgreSQL-specific pieces — expected and reviewed as such, not a code smell.
- `PriceAmountMinorUnits`/`PriceCurrency` are mapped `NOT NULL` (§1.1, §1.3) — the database enforces the same guarantee the creation handler already provides, rather than relying on application code alone.
- No `Booking.LessonId`, no `Booking.PaymentIntentId`-typed-to-Stripe column — see §0, `ADR-0015`, `ADR-0016`.

## 2. Booking state machine

**This section is the single authoritative source for every `Booking` state transition.** `docs/domain/STATE_MACHINES.md` and `ADR-0015` both defer to the table below rather than maintaining their own; if either document ever appears to say something different, this table governs. (Architecture review, 2026-10-04: the previous versions of this table, `STATE_MACHINES.md`'s table, and `ADR-0015`'s table disagreed with each other on three points — a missing `Held → Cancelled` row, whether `Completed` can transition to `Cancelled`/`Refunded`, and whether `Refunded` is reached directly from `Confirmed` or via `Cancelled`. All three are resolved below, mechanically — none of the resolutions invent a refund/cancellation business rule; they only fix which edges exist and where they originate.)

### Skeleton

```
          ┌─────────────────────────────────────────────┐
  ──> Held┤                                              │
          ├──> PendingPayment ──> Confirmed ──┬──> Rescheduled      (terminal)
          │         │                 │       ├──> InProgress ──> Completed   (terminal, no outbound edge)
          │         │                 │       ├──> NoShow            (terminal)
          │         │                 └──> Cancelled ──> Refunded   (terminal; webhook-driven, conditional)
          │         ├──> Cancelled (terminal; never produces a refund)
          │         └──> Expired   (terminal)
          └──> Cancelled (terminal; never produces a refund)
          └──> Expired   (terminal)
```

`Cancelled` is terminal with respect to anything `Bookings`' own API can do to it (the cancel endpoint never accepts a `Cancelled` booking, §4 endpoint 6) — its only outbound edge, to `Refunded`, is driven exclusively by a `Payments` webhook, never by a client call, and only exists at all if a refund was requested and the booking being cancelled had reached `Confirmed`.

### Transition table

| From | To | Trigger | Mechanism | Status |
|---|---|---|---|---|
| — | `Held` | Student selects a slot and requests a hold | Trial-hold creation endpoint (§4 endpoint 1) | **Accepted** (`ADR-0003`) |
| `Held` | `PendingPayment` | Student confirms checkout | Checkout endpoint; requires `HoldExpiresAtUtc > now()` (§4 endpoint 5, `ADR-0013` §3) | **Accepted** |
| `Held` | `Cancelled` | Student or tutor cancels before checkout | Cancel endpoint (§4 endpoint 6). **Never produces a refund** — no payment was ever requested for a `Held` booking | **Specified** (fixes a gap in the original table — the API spec always allowed this; the table didn't show it) |
| `Held` | `Expired` | Hold TTL elapses before checkout | Lazy sweep (`ADR-0013` §2, not a background job) | **Accepted**; mechanism **specified** in `ADR-0013` §2 |
| `PendingPayment` | `Confirmed` | `Payments` webhook-driven success event | Event-driven, idempotent conditional update (`ADR-0015` §4) | **Accepted** direction (`TRIAL_BOOKING_WORKFLOW.md`); mechanics **specified** in `ADR-0015` |
| `PendingPayment` | `Cancelled` | Student or tutor explicitly cancels before payment succeeds | Cancel endpoint. **Never produces a refund** — by definition, payment has not yet succeeded while still `PendingPayment` (success is exactly what moves it to `Confirmed`), so there is nothing captured to refund | **Specified** (`ADR-0015` §2); distinct-from-`Expired` reporting value is `[OPEN — Product Owner]` |
| `PendingPayment` | `Expired` | Payment not completed within the (same) hold TTL | Lazy sweep (`ADR-0013` §2, corrected to cover `PendingPayment` too) — same clock as the `Held` TTL, no second TTL concept | **Specified** (`ADR-0013` §2) |
| `Confirmed` | `Cancelled` | Student or tutor cancels a confirmed booking | Cancel endpoint. Does **not** by itself imply a refund — see the next row and `ADR-0015` §5 | **Specified** (fixes the original table's ambiguous `Confirmed`/`Completed` → `Cancelled`/`Refunded` grouping) |
| `Cancelled` | `Refunded` | A `Payments` webhook confirming a refund it processed, *only* after `Bookings` itself requested one | Event-driven (`ADR-0015` §4–§5). **The only path to `Refunded`** — there is no direct `Confirmed → Refunded` edge. Only reachable if the `Cancelled` booking had previously been `Confirmed` (money actually captured); cancelling from `Held`/`PendingPayment` can never reach `Refunded` | **Specified**; *whether* a refund is requested for a given cancellation remains `[OPEN — Product Owner]` #6/#18 |
| `Confirmed` | `Rescheduled` | Student reschedules | Reschedule endpoint; creates a new linked `Booking` in one transaction, original becomes terminal (`ADR-0017`, corrected for atomicity) | **Specified** |
| `Confirmed` | `InProgress` | Lesson start time reached | `[OPEN — Architect]`, unchanged — time-driven vs. event-driven trigger not decided; **not required for the v1 trial-booking slice** (§9) | Open (non-blocking) |
| `InProgress` | `Completed` | Lesson end reached / marked complete | `[OPEN — Product Owner]` who/what marks it, unchanged; not required for v1 | Open (non-blocking) |
| `InProgress`/`Confirmed` | `NoShow` | One party doesn't join | `[OPEN — Product Owner]`, unchanged; not required for v1 | Open (non-blocking) |

### Terminal states

`Rescheduled`, `Expired`, `Completed`, `NoShow`, and `Refunded` have **no outbound edge at all** — no mechanism in this spec ever transitions a row out of any of them. `Cancelled` has exactly one possible outbound edge (`→ Refunded`), reachable only via a `Payments` webhook, never via any `Bookings` API call.

**`Completed` is resolved as fully terminal — it does not transition to `Cancelled` or `Refunded`.** This fixes the original table's `Confirmed`/`Completed` → `Cancelled`/`Refunded` row, which the concrete API spec (§4 endpoint 6) already contradicted by excluding `Completed` from the cancel endpoint's allowed source states. This is an architectural modeling decision, not a business-policy one: `Booking` and `Payment` are separate state machines reacting to events, never reading each other's tables (`A6`); a post-completion dispute refund is something `Payments`' *own* state machine already models (`Succeeded → Disputed → UnderReview → Won/Lost`, `docs/domain/STATE_MACHINES.md`) and can resolve entirely on its own side without `Bookings` needing a corresponding transition — `Booking.Status = Completed` already correctly records the commercial/scheduling fact that the lesson happened, and a later dispute doesn't retroactively change that fact. If a future product decision requires `Bookings` to reflect post-completion disputes/refunds in its own state, that is a new, explicit decision to make then — it is not assumed here.

**Clarification on scope:** `InProgress`, `Completed`, and `NoShow` are schema-supported (the enum has these values, so no migration is needed later to add them) but their transition *triggers* are explicitly **not required** for the first `Bookings` implementation slice to work end-to-end — a trial booking's observable lifecycle for v1 is `Held → PendingPayment → Confirmed`, with `Cancelled`/`Expired`/`Refunded`/`Rescheduled` as the side-exits actually exercised. This is consistent with "do not invent" — building working transitions for undecided triggers would mean guessing the policy.

### Coupling to Payment and Lesson state machines

**Accepted/confirmed by this spec:** `Booking`, `Payment`, and `Lesson` are three independent state machines (`A6`, `ADR-0010`, `ADR-0015` §5) that react to each other's *events*, never read each other's tables. `Payment`'s own states (`Created`, `RequiresAction`, `Processing`, `Succeeded`, `Failed`, `RefundPending`, `Refunded`, `Disputed`, ...) are entirely internal to `Payments` — `Booking.Status` never mirrors them one-to-one (see §1's note on SCA/3DS not needing its own `Booking` status).

## 3. Trial booking flow (v1)

```
Tutor Profile
  → Student chooses trial duration            (configured set, e.g. {25, 50} minutes — §3.1)
  → Student chooses an available slot          (queried from Availability directly — §5)
  → POST /api/bookings/trial-holds             (creates Held row — §4, §6)
  → POST /api/bookings/{id}/checkout           (Held → PendingPayment, calls Payments — §4, ADR-0015)
  → [frontend completes payment with Payments] (out of scope — Payments not implemented)
  → Payments webhook → BookingConfirmed consumer → PendingPayment → Confirmed  (ADR-0015)
  → Bookings publishes BookingConfirmed         (Lessons creates exactly one Lesson — ADR-0016)
```

### 3.1 Trial durations are configuration, not a hard-coded domain assumption

25 and 50 minutes are the currently-intended values, but nothing in the existing docs (`TUTOR_JOURNEY.md`, `TRIAL_BOOKING_WORKFLOW.md`) states them as a final, binding rule — they read as product direction, not an accepted constraint. **Specified:** `Bookings` reads the allowed duration set from configuration (`Bookings:AllowedTrialDurationsMinutes`, an options-bound `int[]`, following the same `TimeProvider`/Options-pattern convention already used for `Availability`'s `BookingSettings` bounds), validated at hold-creation time against whatever the current configured set is. `25` and `50` are the *default* configuration values, not baked into the domain model, the database, or an enum.

## 4. API specification

All endpoints under `/api/bookings`, JWT bearer auth (Host-level, already wired). No new role/policy beyond `Student`/`Tutor` role checks already established by `Identity` — this module defines its own locally-scoped policy constants (`Bookings.StudentOnly`, `Bookings.TutorOnly`, `Bookings.StudentOrTutorOnly`), per the now-three-times-repeated convention from `Identity`/`Tutors`/`Availability` (flagged again in §10 as a compounding, not-yet-addressed concern).

| # | Method & path | Auth | Purpose |
|---|---|---|---|
| 1 | `POST /api/bookings/trial-holds` | Student | Create a `Held` booking |
| 2 | `GET /api/bookings/{id}` | Student or Tutor on that booking | Read one booking |
| 3 | `GET /api/bookings/me/as-student` | Student | List the caller's bookings as a student |
| 4 | `GET /api/bookings/me/as-tutor` | Tutor | List the caller's bookings as a tutor |
| 5 | `POST /api/bookings/{id}/checkout` | Student (owner) | `Held` → `PendingPayment`, begins payment |
| 6 | `POST /api/bookings/{id}/cancel` | Student or Tutor on that booking | Cancel from an allowed state |
| 7 | `POST /api/bookings/{id}/reschedule` | Student (owner) | Create a linked replacement booking (`ADR-0017`) |

No dedicated "expire" endpoint — expiration is the lazy sweep (`ADR-0013` §2), never client-triggered. No payment-confirmation HTTP endpoint — that path is event-driven (`ADR-0015`), not public API surface.

### 1. `POST /api/bookings/trial-holds`

- **Auth:** authenticated user with the `Student` role (dual-role Student+Tutor users are explicitly permitted, per every prior module's convention — a user is never assumed single-role).
- **Request:** `{ tutorUserId: Guid, durationMinutes: int, startUtc: DateTimeOffset }`
- **Response (201):** `{ id: Guid, status: "Held", startUtc, endUtc, holdExpiresAtUtc, priceAmountMinorUnits, priceCurrency }`
- **Validation (FluentValidation, shape only):** `tutorUserId` not empty; `durationMinutes` non-zero; `startUtc` not in the past.
- **Domain/handler validation (business rules, not shape):**
  - `durationMinutes` must be in the currently configured allowed set (§3.1) → `400`.
  - `tutorUserId` must not equal the caller's own `UserId` (a tutor cannot book themselves) → `400`.
  - `ITutorAvailabilityQuery.ValidateSlotAsync` (`ADR-0014`) must return valid → `400` with the query's `Reason` (e.g. "tutor not bookable", "outside availability", "inside minimum notice window", "beyond maximum booking window") if not.
  - `ITutorPricingQuery.GetCurrentPriceAsync` must return a price; if the tutor has no configured pricing yet, → `400` ("tutor pricing not configured").
  - **Resolved — Product Owner decision, 2026-10-04:** the caller's (student's) count of existing `Held`/`PendingPayment` bookings, **summed across all tutors**, must be `< 3` → `400`/`409` ("maximum concurrent holds reached") if not. **Rationale:** closes the abuse vector (`docs/OPEN_QUESTIONS.md` #32) of a student indefinitely squatting on inventory across many tutors without ever completing a booking, while still allowing the legitimate case of a student comparing a couple of tutors' trial slots at once. The limit is global, not per-tutor — a student cannot work around it by spreading holds across different tutors. An older hold is **never** auto-expired to make room for a new one; the new hold is simply rejected until the student's existing hold either expires naturally (TTL, `ADR-0013`) or is explicitly cancelled. **Not yet implemented** — `CreateTrialHold.cs` does not perform this check today; this is the locked V1 policy it must enforce once implemented.
  - The `INSERT` itself may fail with PostgreSQL `23P01` (exclusion violation) if another hold/booking won the race in the interim → caught, mapped to **`409 Conflict`** ("slot no longer available").
- **Idempotency:** not idempotent by design — each call creates a new hold; the client is expected to call this once per user-initiated "book this slot" action. (Compare with `AddTimeOff`'s identical non-idempotent-POST convention in `Availability`.)
- **State-transition rule:** creates the row directly in `Held`; no prior state.

### 2. `GET /api/bookings/{id}`

- **Auth:** caller must be `StudentUserId` or `TutorUserId` on the booking → otherwise `403` (exists-but-forbidden, not `404`, consistent with not leaking existence vs. ownership ambiguity... **`[OPEN — Architect, low stakes]`**: whether a non-owner should see `403` or `404` is a minor, deferred UX choice, not resolved here — either is defensible; flagged so it isn't silently assumed by whoever implements this).
- **Response (200):** full booking projection (all fields in §1.1 except nothing — this is the "it's mine, show everything" view).
- **Error cases:** `404` if the ID doesn't exist at all; `401` unauthenticated.

### 3. / 4. `GET /api/bookings/me/as-student` / `GET /api/bookings/me/as-tutor`

- **Auth:** `Student` role / `Tutor` role respectively (matching `Availability`'s `/me` self-service pattern — these are "my own data" endpoints, not general-purpose tutor/student lookup).
- **Response (200):** list of booking summaries (lighter projection than `GET /{id}` — `id`, counterpart `UserId`, `status`, `startUtc`, `endUtc`, `durationMinutes`).
- Pagination: **`[OPEN — Architect, low stakes]`** not specified — out of scope for a v1 trial-booking slice where volume per user is small; flagged so it isn't silently decided later without a look.

### 5. `POST /api/bookings/{id}/checkout`

- **Auth:** caller must be the booking's `StudentUserId`.
- **Request:** empty body.
- **Response (200):** `{ paymentReferenceId: string, clientSecret: string }` — `paymentReferenceId` is `Payments`' opaque `Payment.Id` (not a Stripe ID, `ADR-0015` §1); `clientSecret` is the Stripe `client_secret` relayed through `Bookings` unmodified, for one-time client-side use with Stripe.js/Elements. **Mechanism resolved (`ADR-0019`, 2026-10-04):** `Bookings` is the only caller the frontend ever talks to for checkout. `Bookings`' own handler, after its ownership/state checks below, makes a synchronous server-to-server HTTP call (`IHttpClientFactory`-based, not a `Shared` command interface) to `Payments`' `POST /api/payments/intents`, forwarding only `{ BookingId, StudentUserId }` — never the amount or currency, which `Payments` independently derives via the reciprocal read-only `IBookingCheckoutEligibilityQuery` (`TutorFlow.Shared`, implemented by `Bookings`). This endpoint is no longer blocked on anything other than `Payments` actually existing (§9).
- **State-transition rule:** `Held` → `PendingPayment`, only if the hold has not yet expired (re-checks `HoldExpiresAtUtc > now()` as part of the same conditional update, `ADR-0013` §3) → `409` if already expired.
- **Idempotency (refined by `ADR-0019`, 2026-10-04 — supersedes the `ADR-0015` §3 text below; see that ADR for the full mechanics):** the handler reads current status *before* calling `Payments`. Only `Held` or `PendingPayment` may proceed; any other state is `409`. If `Held`, it is conditionally transitioned to `PendingPayment` first (`WHERE Status = 'Held'`); if already `PendingPayment`, this step is skipped. **`Payments` is then called on every attempt, including retries** — never short-circuited by an already-stored `PaymentReferenceId` — because the Stripe `client_secret` is never persisted (`ADR-0018` §2) and a legitimate retry (e.g. page reload before completing the Stripe challenge) has no other way to obtain a fresh one. `BookingId` is passed to `Payments` as the idempotency key for the create-intent call itself, so a retry can never create a second real `PaymentIntent` — `Payments`' own idempotency, not `Bookings` skipping the call, is what prevents duplication.

### 6. `POST /api/bookings/{id}/cancel`

- **Auth:** `StudentUserId` or `TutorUserId` on the booking.
- **Request:** `{ reason: string? }`
- **Response (200):** updated booking projection with `Status: Cancelled`.
- **State-transition rule:** allowed from `Held`, `PendingPayment`, `Confirmed` → `Cancelled` (matches §2's authoritative table exactly: three rows, all terminal or leading only to the conditional `Refunded` edge). Not allowed from any terminal state (`Cancelled`, `Expired`, `Completed`, `Refunded`, `Rescheduled`, `NoShow`) → `409`.
- **Side effect:** the conditional `UPDATE` sets `CancelledAtUtc` and **`CancelledByUserId`** (the caller's own `UserId`, from the JWT — whichever of `StudentUserId`/`TutorUserId` matched) in the same statement, per §1.1/§1.5.
- **Idempotency (documented explicitly, architecture review 2026-10-04):** calling `cancel` a second time on an already-`Cancelled` (or any other terminal) booking returns `409`, **not** an idempotent no-op — this is a deliberate asymmetry with `checkout`/`confirm` (which treat a repeat call as a no-op success). Cancellation is user-initiated and one-directional; a second cancel attempt on an already-cancelled booking is treated as "nothing to do, and the caller should know that" rather than silently swallowed, so a client-side double-click or retry surfaces as a clear, harmless `409` rather than a false "I cancelled it again" `200`.
- **Resolved — Product Owner decision, 2026-10-04 (`ADR-0015` §6):** cancelling a `Confirmed` booking **≥24 hours before its `StartUtc`** triggers a full-refund request to `Payments`; cancelling **<24 hours before `StartUtc`** does not — no refund is requested, ever, for a late cancellation. V1 is **full-refund-only**; there are no partial-refund tiers. Cancelling from `Held`/`PendingPayment` never has anything to refund regardless of timing (no payment was captured yet). **Not yet implemented** — the `CancelBooking` handler does not yet issue any refund request at all (`Payments` doesn't exist); this is the locked V1 policy it must follow once that integration exists. See `ADR-0015` §6 for the full mechanical detail, including why no-show (a different transition entirely) is explicitly excluded from this window.

### 7. `POST /api/bookings/{id}/reschedule`

- **Auth:** `StudentUserId` on the booking.
- **Request:** `{ newStartUtc: DateTimeOffset }` (duration carries over from the original; changing duration on reschedule is out of scope/`[OPEN — Product Owner]` if ever needed).
- **Response (201):** the **new** booking's projection, `Status: Held` (or `Confirmed`, per `ADR-0017`'s open payment-carry-forward question) — plus `RescheduledFromBookingId` pointing at the original.
- **State-transition rule:** allowed only from `Confirmed` (rescheduling a mere `Held`/`PendingPayment` hold is just "cancel and rebook," not a distinct operation). One database transaction, flip-first ordering (`ADR-0017`, corrected): the original's conditional `UPDATE ... WHERE status = 'Confirmed'` is attempted first; if it affects zero rows (lost a concurrent race, or no longer `Confirmed`) the handler aborts and returns `409` **without creating a new row**; only if it affects exactly one row does the handler create the new `Booking` and link both rows, then commit both writes together.
- **Resolved — Product Owner decision, 2026-10-04 (`ADR-0017`):** two additional eligibility checks, on top of the `Confirmed`-only guard above:
  - **At most one reschedule per booking** — rejected (`409`) if the original already has a non-null `RescheduledFromBookingId` (i.e. it is itself the product of an earlier reschedule).
  - **The request must be made ≥12 hours before the original's `StartUtc`** — if made <12 hours before, the reschedule is rejected; the cancellation policy (`ADR-0015` §6) applies instead, which at that notice distance always resolves to "no refund" since <12h is also <24h. The client must surface this as "cannot reschedule; cancelling now is not refundable," not silently convert the request into a cancellation itself.
  - **Not yet implemented** — `RescheduleBooking.cs` does not yet check either condition; this is the locked V1 policy it must enforce once implemented. The underlying one-transaction, flip-first mechanics are unchanged by this decision.
- **Validation:** the new slot goes through the *exact same* validation as endpoint #1 (`ITutorAvailabilityQuery`, exclusion constraint) — no shortcut.
- **Idempotency:** not idempotent by design, consistent with `trial-holds` (endpoint #1) — a retried reschedule call is indistinguishable from a second, genuine reschedule attempt and is handled by the same flip-first ordering: the first call's flip wins, the retry's flip affects zero rows and returns `409` with no orphan created (`ADR-0017`).
- **`[OPEN — Product Owner]`**: reschedule *fee* amounts, if any, beyond the no-refund cancellation fallback above — unchanged, not invented.

## 5. Booking ↔ Availability contract

**Specified** (`ADR-0014`): `Bookings` depends on `ITutorAvailabilityQuery` (defined in `TutorFlow.Shared`, implemented by `Availability`, injected via DI — no project reference between the two modules).

### What each side is authoritative for

| Concern | Authority | Mechanism |
|---|---|---|
| Tutor bookability (published/suspended) | `Availability`'s local read model (`ADR-0011`) | inside `ValidateSlotAsync` |
| Weekly availability rules | `Availability` | inside `ValidateSlotAsync`, reusing `AvailabilityCalculator` |
| Time off | `Availability` | inside `ValidateSlotAsync` |
| Booking settings (min notice, max window, timezone/DST) | `Availability` (`ADR-0012`) | inside `ValidateSlotAsync` — DST correctness is entirely `AvailabilityCalculator`'s existing, already-hardened responsibility (see the Availability hardening pass); `Bookings` neither duplicates nor re-derives it |
| Existing `Booking` holds/confirmed bookings, adjacency/overlap (hold-creation time) | `Bookings` | PostgreSQL exclusion constraint (`ADR-0013`) — **not** part of `ValidateSlotAsync` |
| Existing `Booking` holds/confirmed bookings, adjacency/overlap (browse/list time) | `Bookings` (exposed to `Availability` via `IActiveBookingIntervalsQuery`) | New reciprocal query contract — see below (`ADR-0014` correction) |

**The authoritative final check is the database exclusion constraint, not the `Availability` query.** `ValidateSlotAsync` is advisory-but-required: it must pass before a hold-creation attempt is even made (it enforces business-eligibility rules the database constraint knows nothing about — a database constraint cannot tell you "the tutor doesn't work Tuesdays"), but it can be stale by the time the `INSERT` runs (another request could have taken the slot microseconds later). The exclusion constraint is what actually, finally, atomically decides — exactly the two-layer design `ADR-0003` already implied ("Availability is the source of truth for whether a slot is actually bookable right now" for *business eligibility*; the database is the source of truth for *concurrency safety*).

### Required `Availability`-side prerequisites (none implemented by this document)

Four distinct pieces of work are required in `Availability` before `Bookings` can be implemented against it. All are out of scope here ("do not modify unrelated modules") and are restated in `docs/domain/DOMAIN_MODEL.md`'s `Availability` row and sequenced in §9:

1. **Generalized duration-aware slot validation.** `Availability`'s current `AvailabilityCalculator.ComputeOpenSlotStarts` only enumerates slots at the tutor's own configured `DefaultSlotDurationMinutes` — it has no notion of validating an arbitrary *requested* duration (25/50 minutes, which need not match a tutor's default slot length). `ITutorAvailabilityQuery`'s implementation needs a generalized check — "does a slot of exactly this duration, starting at exactly this instant, fit entirely within an available window, clear of time off, within min-notice/max-window bounds" — a (small) generalization of the existing calculation, not a new one, and not something `Bookings` reimplements.
2. **A new public, non-self-service browse-slots endpoint.** Every endpoint `Availability` exposes today is self-service (`[Authorize(Policy = "Availability.TutorOnly")]`, reading the *caller's own* `UserId`) — there is no existing endpoint for "show me tutor X's slots" as a different user would need for browsing. `ITutorAvailabilityQuery` sidesteps this for the *validation* step (an in-process DI call, not an HTTP endpoint), but the student-facing browse UI needs this new endpoint regardless.
3. **`IActiveBookingIntervalsQuery` consumption in that new endpoint** (`ADR-0014` correction, 2026-10-04): the new browse-slots endpoint from (2) must call this reciprocal, `Bookings`-owned query contract and pass its result into `ComputeOpenSlotStarts`'s existing `occupiedIntervals` parameter — otherwise it will show a slot as open that `Bookings` already holds (`Held`/`PendingPayment`/`Confirmed`), satisfying `ADR-0003`'s original requirement that *"Availability must treat active holds owned by Bookings as unavailable."* `Availability`'s existing self-service `/me/slots` endpoint is unaffected — this applies only to the new browse endpoint from (2).
4. **The `ADR-0011` publish-event pipeline must actually be wired** (`Tutors` publishes on `Published`, `Availability` consumes into `TutorBookabilityRecords`) **before `ITutorAvailabilityQuery` can return a correct "bookable" answer for any real tutor.** No consumer exists yet (`TutorBookabilityProjector` is present but nothing calls it), so the table is empty today for every tutor. Per `ADR-0014`'s correction, **absence of a record is treated as "not bookable" (fail closed)** — `ValidateSlotAsync` must never default to `true` for a missing record. Until this pipeline is wired, every `Bookings` hold-creation attempt will correctly fail with "tutor not bookable," by design, not by bug.

## 6. Concurrency and idempotency design

Full mechanics in `ADR-0013`; summarized against each required guarantee:

| Requirement | Design |
|---|---|
| Two students booking the same tutor/time | PostgreSQL exclusion constraint on `(tutor_user_id, during)` scoped to `Held`/`PendingPayment`/`Confirmed` (`ADR-0013`, corrected 2026-10-04 to include `PendingPayment`) — the second `INSERT` is rejected by the database (`23P01`), caught, mapped to `409` |
| Two concurrent holds for overlapping intervals | Same constraint — "hold" and "booking" are the same row/constraint, no separate mechanism needed |
| A hold confirmed after another booking took the slot | Cannot happen, **and this claim now actually holds** (architecture review, 2026-10-04 — the original `Held`/`Confirmed`-only constraint scope left a real gap here during the `PendingPayment` window; fixed by widening the constraint, see `ADR-0013` §1). Confirming only ever flips `Status` on an *existing*, still-protected row (`UPDATE ... WHERE status = 'PendingPayment'`); it never creates a new interval, so it never contends with the exclusion constraint. The scenario "the slot was already taken by someone else" is prevented earlier, at hold-creation time, not at confirm time — and the row stays protected for the entire `Held → PendingPayment → Confirmed` lifetime, with no gap |
| Expired holds incorrectly blocking slots | Lazy per-tutor sweep before every hold-creation attempt, now covering both `Held` and `PendingPayment` (`ADR-0013` §2, corrected) — not a background job, required for correctness |
| Duplicate confirmation requests | Conditional `UPDATE ... WHERE status = 'PendingPayment'`; zero rows affected + already-`Confirmed` ⇒ idempotent no-op (`ADR-0015` §4) |
| Duplicate payment/webhook processing | Same mechanism — `Bookings`' idempotency does not depend on `Payments`' own webhook deduplication (defense in depth) |
| Duplicate/retried checkout calls creating a second real `PaymentIntent` | `Bookings` calls `Payments` on every attempt (never short-circuits); `BookingId` as the `Payments`-side idempotency key is what actually prevents duplication (`ADR-0015` §3, refined by `ADR-0019`, 2026-10-04) |
| Concurrent double-reschedule of the same `Confirmed` booking | One transaction, flip-original-first ordering — if the flip affects zero rows, the handler aborts before creating the replacement row, so no orphan is ever created (`ADR-0017`, corrected) |
| A student squatting on inventory via unlimited concurrent holds | Application-level check (not a database constraint): reject a new hold if the caller already has ≥3 `Held`/`PendingPayment` bookings globally — Product Owner decision, 2026-10-04, §4 endpoint 1. Deliberately *not* a DB-level mechanism like the exclusion constraint above, since this is an abuse-prevention business rule, not a concurrency-safety invariant; an older hold is never auto-expired to make room |

### Idempotency semantics per endpoint (documented explicitly, architecture review 2026-10-04)

| Endpoint | Repeat-call behavior |
|---|---|
| `trial-holds` (create) | **Not idempotent by design** — each call creates a new row; a double-click produces one `201` and one `409` (exclusion constraint), not two successful holds |
| `checkout` | **Status transition is idempotent** (skips the `Held`→`PendingPayment` flip if already there); **the call to `Payments` is not skipped on retry** — it is made on every attempt so the frontend can always obtain a fresh `clientSecret`, relying on `Payments`' own `BookingId`-derived idempotency key to prevent a duplicate real `PaymentIntent` (refined by `ADR-0019`, 2026-10-04; see `ADR-0015` §3) |
| confirm (webhook-driven, no public endpoint) | **Idempotent no-op** — a duplicate webhook delivery for an already-`Confirmed` booking affects zero rows and is treated as success, not re-published (`ADR-0015` §4) |
| `cancel` | **Not idempotent — returns `409`** on a retry against an already-terminal booking. Deliberate asymmetry with `checkout`/confirm: see §4 endpoint 6 |
| `reschedule` | **Not idempotent by design**, same as `trial-holds` — a retry races the same flip-first conditional update as a genuine concurrent attempt and loses cleanly with no orphan (`ADR-0017`) |

**Transaction boundaries:** hold-creation wraps the expiry-sweep `UPDATE` and the `INSERT` in one transaction, default `READ COMMITTED`. State transitions (`checkout`, `confirm`, `cancel`) are each a single conditional `UPDATE`, inherently atomic, no explicit transaction needed. No operation in this module requires `SERIALIZABLE` isolation — unlike `Availability`'s `ReplaceWeeklyRules`, every `Bookings` write either has a database constraint to lean on or is a single-statement conditional update with no read-then-write gap.

**Retry behavior:** on a `23P01` exclusion violation, the handler does **not** automatically retry with a different slot (that would be inventing a "suggest an alternative" product feature) — it returns `409` and leaves the student to pick a different slot, consistent with "do not invent unresolved product policies."

## 7. Booking ↔ Payments contract

Full detail in `ADR-0015`. Summary: `Bookings` stores an opaque `PaymentReferenceId`; `PendingPayment → Confirmed` and `Cancelled → Refunded` (never `Confirmed → Refunded` directly, §2) are driven exclusively by `Payments`-originated webhook events, never by the frontend; SCA/3DS "requires action" has no `Booking`-side state; webhook processing is idempotent via conditional `UPDATE`, with transient local failures surfaced as retryable HTTP errors so the provider redelivers (`ADR-0015` §4, added 2026-10-04). Checkout itself checks current status before ever calling `Payments`, and uses `BookingId` as the `Payments`-side idempotency key so a retry after a partial local failure cannot create a duplicate `PaymentIntent` (`ADR-0015` §3, added 2026-10-04). Not implemented — `Payments` does not exist.

## 8. Booking ↔ Lessons contract

Full detail in `ADR-0016`. Summary: `Lessons` creation is event-driven (`BookingConfirmed`), asynchronous (does not block `Booking` confirmation), deduplicated by `Lessons`' own unique index on `BookingId` (not by `Bookings`), and `Booking` never stores a `LessonId`. Not implemented — `Lessons` does not exist.

## 9. Testing specification

Follows the established `xUnit v3` + `Testcontainers.PostgreSql` + `WebApplicationFactory<Program>` + locally-minted-JWT convention from `Identity`/`Tutors`/`Availability` (`BookingsApiFixture`, same shape as `AvailabilityApiFixture`).

### Domain unit tests (no DB)

- `Booking` creation invariants: start-before-end, duration-must-be-in-configured-set.
- State-transition guards: every row in §2's table that has a "from" precondition, both the allowed and a disallowed source state.
- `ADR-0017` reschedule: original becomes `Rescheduled` (terminal — verify no further transition succeeds), new booking carries the link.
- Price-snapshot immutability: changing a (test-double) tutor price after a `Booking` is created does not change the already-stored snapshot.

### Integration tests (Testcontainers Postgres, through the real HTTP pipeline)

- **Happy-path trial booking**: create hold → checkout → (simulate payment-succeeded event/handler call directly, since `Payments` doesn't exist) → `Confirmed`.
- **Invalid slot**: `ITutorAvailabilityQuery` test double returns invalid → `400` with the reason surfaced.
- **Unavailable/unbookable tutor**: same mechanism, bookability-specific reason.
- **Expired hold**: create a hold with a past `HoldExpiresAtUtc` (via direct DB manipulation, matching `Availability`'s precedent for forcing specific time-based states) → attempting checkout → `409`; attempting a *new* hold for the same tutor/interval afterward succeeds (proves the sweep actually un-blocks it).
- **Payment failure** (simulated via direct handler invocation, no real `Payments`): `PendingPayment` → `Cancelled`/`Expired` per whichever the test simulates.
- **Payment success** (same simulation approach): `PendingPayment` → `Confirmed`, verify `BookingConfirmed`-shaped data is ready to publish (asserted directly, since nothing is wired to actually publish yet).
- **Duplicate confirmation**: call the confirm handler twice for the same booking → second call is a no-op, not an error, not a double side-effect.
- **Concurrent holds for the same tutor/overlapping interval**: `Task.WhenAll` two hold-creation requests for the same tutor/overlapping times → exactly one `201`, the other `409` — reframed per the Availability hardening lesson learned: assert the *invariant* (never two `201`s, never a `500`) rather than assuming perfect timing overlap every run; **plus** a deterministic DB-level test (two `BookingsDbContext`s, manual sequencing, directly provoking `23P01`) proving the exact exception shape the handler's catch clause expects — mirroring exactly how the Availability hardening pass caught a real exception-wrapping bug this same way.
- **`PendingPayment` is protected** (new, architecture review 2026-10-04 — regression test for `ADR-0013`'s corrected constraint scope): create a `Held` booking, transition it to `PendingPayment` (simulated), then attempt a second, overlapping hold-creation request for the same tutor → must be rejected `409`. This test must fail against the original `Held`/`Confirmed`-only constraint scope and pass against the corrected one — it exists specifically to prove the gap identified by the architecture review stays closed.
- **Sweep expires a stale `PendingPayment` row** (new): force a booking to `PendingPayment` with a past `HoldExpiresAtUtc` (direct DB manipulation) → a new hold for the same tutor/interval succeeds afterward, proving the sweep un-blocks `PendingPayment` rows, not only `Held` ones.
- **Concurrent double-reschedule** (new, `ADR-0017`): two concurrent `reschedule` requests against the same `Confirmed` booking targeting different new slots → exactly one `201` with a correctly bidirectionally-linked pair, the other `409`, and the total row count for that booking is "original + exactly one new row" — never an orphaned, unlinked second new row. A second variant targets the *same* new slot for both requests, to confirm the exclusion-constraint-rejection path also leaves no orphan and the original remains `Confirmed` (rolled back, not stranded in `Rescheduled`).
- **Exclusion constraint DDL assertion** (new): a focused test querying PostgreSQL's catalog after migration to assert the constraint's `WHERE` clause literally includes `PendingPayment` — guards against a future migration edit silently narrowing the scope back down without a test failing.
- **`Completed`/`Cancelled`/`Refunded` transition guards** (new, `ADR-0017`/§2 reconciliation): a domain-unit test asserting the cancel handler rejects a `Completed` source state (`409`), and that no code path exists that would move a `Completed` booking to `Cancelled` or `Refunded` — the regression test for the contradiction the architecture review found between the original §2 table and §4's endpoint rule.
- **Overlapping bookings**: two different students, same tutor, genuinely overlapping intervals → second rejected.
- **Adjacent (touching, non-overlapping) bookings**: same tutor, back-to-back intervals (one ends exactly when the other starts) → both succeed (`[)` half-open semantics, `ADR-0013`).
- **DST spring-forward / fall-back**: delegated correctness — since slot *validation* defers entirely to `Availability`'s already-DST-hardened `AvailabilityCalculator` (via `ITutorAvailabilityQuery`), `Bookings`' own tests only need a test double proving it *calls* the query and respects its answer; they do not need to re-derive DST scenarios (that would be exactly the duplication this spec was told to avoid). One test should confirm `Booking.StartUtc`/`EndUtc` round-trip correctly through a DST boundary when persisted (a straightforward UTC-storage check, not a DST *computation* test).
- **Cancellation/reschedule state rules**: cancel from each allowed and each disallowed state; reschedule from `Confirmed` creates a correctly-linked pair; reschedule from `Held` is rejected.
- **Authorization**: non-owner `GET`/`cancel`/`checkout`/`reschedule` attempts; dual-role (Student+Tutor) user can book as a student and separately appear in their own tutor booking list if also a tutor; unauthenticated → `401`.
- **Idempotency**: `checkout` called twice; `cancel` called twice on an already-cancelled booking (should be a clean error, not a crash).
- **PostgreSQL constraint enforcement**: a focused test asserting the migration actually created the exclusion constraint and that a raw conflicting `INSERT` (bypassing the handler, direct SQL) is rejected by the database itself — proving the constraint exists and works independent of application code.
- **Transaction/retry behavior**: the expiry-sweep-then-insert transaction behaves correctly when the sweep finds nothing to expire (no-op) and when it does (frees the slot within the same transaction as the new insert), for both `Held` and `PendingPayment` stale rows.
- **Checkout idempotency ordering** (refined by `ADR-0019`, 2026-10-04): assert a retried `checkout` call against an already-`PendingPayment` booking skips the status-flip `UPDATE` (still `PendingPayment`, zero rows affected) but **does** call the (test-double) `Payments` client again, relaying whatever `PaymentReferenceId`/`clientSecret` that call returns — never served from a locally-stored value.
- **Webhook transient-failure retryability** (new): simulate the confirm handler's conditional `UPDATE` throwing a transient (non-constraint) DB exception → the endpoint must return a `5xx`, not swallow it into a `200`.
- **Composition-root DI resolution sanity test** (new, low-severity hardening): once `Bookings` is scaffolded, one test resolving `ITutorAvailabilityQuery`, `ITutorPricingQuery`, and `IActiveBookingIntervalsQuery` from the Host's built service provider, asserting each resolves to the expected owning module's implementation — catches an accidental module-registration-order regression at test time rather than at first real request.
- **Fail-closed bookability default** (new, `ADR-0014` correction): with no `TutorBookabilityRecords` row for a given tutor, a hold-creation attempt against that tutor must fail with "tutor not bookable," never succeed — the regression test for the fail-closed default this review added.
- **Booking → Lessons integration contract**: since `Lessons` doesn't exist, this is tested as a **contract test**: given a `Confirmed` booking, the data that *would* populate a `BookingConfirmed` event is fully and correctly derivable from the `Booking` row (asserted directly against the record shape in `ADR-0016` §2) — proving the contract is satisfiable without requiring a live consumer.

## 10. Summary of what remains open

### Resolved since this spec was first written (Product Owner decisions, 2026-10-04)

None of the following are implemented by this update — each is the locked V1 policy the referenced handler must enforce once built:

- Exact trial-duration pricing formula — pro-rata from the tutor's hourly rate, deterministic minor-unit rounding, no platform subsidy or flat price (§1.3).
- Cancellation notice window and refund rule — ≥24h before `StartUtc`: full refund; <24h: no refund; full-refund-only, no partial tiers (§4 endpoint 6, `ADR-0015` §6).
- Reschedule limit and notice window — at most once per booking, ≥12h before `StartUtc`, else the cancellation policy applies instead (§4 endpoint 7, `ADR-0017`).
- No-show declaration and grace period — tutor-reported/manual (no attendance telemetry exists yet), 10-minute grace period; student no-show: tutor keeps the payment; tutor no-show: student gets a full refund without `Booking.Status` ever becoming `Refunded` (`ADR-0015` §6; see `docs/domain/STATE_MACHINES.md`).
- Partial refunds — out of scope for V1; the `Payments`↔`Bookings` refund contract remains full-refund-only (`ADR-0004`, `ADR-0015` §6).
- Per-student simultaneous-hold limit — capped at 3 concurrent `Held`/`PendingPayment` bookings globally across all tutors; the 4th is rejected, never auto-expiring an older hold (§4 endpoint 1, §6).
- `Booking`-side dispute window — **resolved as not needed**: no separate Booking-level dispute concept in V1; payment disputes are handled entirely by `Payments`/Stripe, lesson-quality complaints route through a future Support/Reviews mechanism referencing `BookingId`, and no terminal `Booking` state is ever reopened for a dispute (unchanged from the already-accepted "`Completed` has no outbound edge" architecture, §2).

### Still open — not decided here, do not invent

- Who/what marks `InProgress`/`Completed` (`docs/OPEN_QUESTIONS.md` #17, and the equivalent question now applies to `Booking.Status` too, not only `Lesson.Status`).
- Reschedule *fee* amounts, if any, beyond the no-refund cancellation fallback (`ADR-0017`).
- Guest trial booking before signup (#5) — this spec assumes an authenticated `Student` for every hold-creation request; if guest-before-signup is later approved, the auth requirement on endpoint #1 changes, not the state machine.
- Exact earnings accrual/risk-window timing, payout frequency/minimum threshold/fees, VAT/tax/invoicing/tutor-tax-residency — Product/Legal decisions required before production payout/tax implementation, tracked in `docs/OPEN_QUESTIONS.md`; none block `Bookings`' own first implementation slice.
- The repeated per-module role/policy-constant duplication (`Identity.TutorOnly`, `Tutors.TutorOnly`, `Availability.TutorOnly`, and now `Bookings.StudentOnly`/`Bookings.TutorOnly`/`Bookings.StudentOrTutorOnly`) — flagged again, now four times over; still not resolved by this spec (out of scope, per explicit instruction not to consolidate role constants in this pass).
