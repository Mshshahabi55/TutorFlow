# State Machines

The overall skeleton shape of each machine below is now **Accepted** (see `ADR-0003`, `ADR-0004`, `ADR-0010`). Several of the business rules parameterizing specific transitions are now locked for V1 (cancellation notice window, no-show grace period/consequences, partial refunds, dispute evidence process — all resolved 2026-10-04, see `ADR-0015` §6 and `ADR-0004`'s "Product Owner decisions locked for V1" section). **Who triggers lesson completion** remains **`[OPEN — Product Owner]`** and is called out inline — **do not invent that** when scaffolding; implement the accepted states/transitions and leave that one open transition rule as an explicit TODO tied back to this document.

The Booking machine's full implementation detail (persistence, concurrency, cross-module contracts) lives in `docs/domain/BOOKINGS_SPEC.md` and `ADR-0013`–`ADR-0017`.

**`docs/domain/BOOKINGS_SPEC.md` §2 is the single authoritative source for the Booking transition table** — this document no longer maintains its own copy. (Architecture review, 2026-10-04: this document's previous table disagreed with `BOOKINGS_SPEC.md` §4's concrete API rules and with `ADR-0015`'s table on three points — a missing `Held → Cancelled` row, `Completed`'s reachability, and whether `Refunded` is reached directly from `Confirmed` or via `Cancelled`. Maintaining one table instead of three removes the ability for them to silently drift apart again.)

---

## Booking state machine

### Accepted skeleton

`Held → PendingPayment → Confirmed → (Rescheduled | InProgress) → Completed`, with `Held`/`PendingPayment` also side-exiting to `Cancelled`/`Expired`, `Confirmed` also side-exiting to `Cancelled`/`NoShow`, and `Cancelled` conditionally reaching `Refunded` (webhook-driven, never direct from `Confirmed`). This skeleton is accepted; the exact triggers/rules for several transitions remain open, and the state-shape mechanics (which edges exist, in what order, with what terminal guarantees) are specified in full in `BOOKINGS_SPEC.md` §2 — see that document for the authoritative table, terminal-state list, and the `Completed`/`Refunded` reachability resolution.

### Coupling to Payment state machine

A `Booking` should never unilaterally decide it's `Refunded` — that's downstream of `Payments` actually processing a refund. **Resolved** (`ADR-0015` §5): `Bookings` requests a refund via command/event to `Payments`; `Payments` processes it and emits a payment-refunded event; `Bookings` then (and only then) transitions `Cancelled → Refunded` — never `Confirmed → Refunded` directly, since a refund can only ever apply to a booking that was cancelled after being `Confirmed` (money actually captured). This confirms the direction of coupling — **resolved, Product Owner decision 2026-10-04** (`ADR-0015` §6): a cancellation ≥24 hours before the booking's `StartUtc` warrants a full refund; <24 hours warrants none; V1 is full-refund-only, no partial tiers.

A tutor no-show also triggers a full refund request from `Bookings` to `Payments`, but via a different path: `BookingStatus.NoShow` (reached from `InProgress`/`Confirmed` directly) is itself terminal with no outbound edge, so a no-show-triggered refund never produces a `Booking.Status` of `Refunded` — it is recorded entirely within `Payments`' own state machine, the same pattern already used for post-completion disputes. See `docs/product/TRIAL_BOOKING_WORKFLOW.md` and `ADR-0015` §6 for the full no-show policy (10-minute grace period, tutor-reported/manual for V1, student-no-show has no refund consequence).

---

## Payment state machine

### Proposed states
`Created → RequiresAction → Processing → Succeeded | Failed`; `Succeeded → RefundPending → Refunded`; `Succeeded → Disputed → UnderReview → Won | Lost`.

### Notes

- `RequiresAction` exists specifically for SCA/3DS flows common in the European market — this is not optional for an EU-facing payment flow.
- All transitions into `Succeeded`/`Failed`/`Refunded`/`Disputed`/`Won`/`Lost` are **webhook-driven**, never set from an API call initiated by the frontend alone.
- **Resolved — Product Owner decision, 2026-10-04** (`ADR-0004`, `ADR-0015` §6): partial refunds are out of scope for V1 — full-refund-only. The `RefundPending → Refunded` transition always refers to the full captured amount.
- **Resolved — Product Owner decision, 2026-10-04** (`ADR-0004`): dispute evidence submission is a manual Admin process for V1 — no automated dispute-evidence generation is built. The `Disputed → UnderReview` transition surfaces to an Admin who handles evidence through Stripe's own dashboard/API by hand.
- **`[OPEN — Architect]`**: retry policy for `Failed` (e.g. subscription renewal retry schedule) — ties into `SUBSCRIPTION_JOURNEY.md`'s open renewal-failure question.
- The payment provider is now Stripe (`ADR-0004`, Accepted) — exact state names and webhook event mapping are refined against Stripe's literal webhook taxonomy (e.g. `payment_intent.*` events) when `Payments` is scaffolded; this table remains the provider-agnostic shape in the meantime.
- Payment state remains independent from Booking state regardless of provider — `Bookings` and `Payments` react to each other's events, never read each other's tables (`ADR-0004`).

---

## Subscription state machine (most tentative of the four)

### Proposed skeleton — explicitly a skeleton, not a design

`Active → PastDue → Cancelling (cancel-at-period-end) → Cancelled`, with `Active → Expired` if not renewed and no explicit cancellation flow applies.

Every transition's trigger and side-effect (does `PastDue` block scheduling new lessons against existing credit? does `Cancelling` still grant the final cycle's credits?) depends on cancellation/rollover rules that are **`[OPEN — Product Owner]`** per `SUBSCRIPTION_JOURNEY.md`. This skeleton exists only so the Architect has a starting shape to react to once those product rules land — it is not ready to inform a schema.

---

## Lesson state machine

### Accepted states (resolves A6)

`Scheduled → InProgress → Completed`, with `Scheduled/InProgress → Cancelled | NoShow`.

**`Lesson` has its own independent lifecycle — it is not merely a projection of `Booking`'s state.** `Booking` and `Lesson` can influence each other through integration events (e.g. a confirmed `Booking` causes `Lessons` to create a `Scheduled` `Lesson`; a cancelled `Lesson` may cause `Bookings` to react), but each module owns and persists its own state independently. This is consistent with the 1:1 Booking↔Lesson cardinality decided in `ADR-0010` — since every occurrence gets its own `Lesson` row regardless of whether it came from a trial or a subscription, there is no projection-only shortcut available anyway.

### Notes

- The states overlap in name with Booking's `Confirmed/InProgress/Completed/NoShow` sub-states by design — they describe the same real-world moment from two different bounded contexts' perspectives, not a shared table.
- Review eligibility (`Reviews` module) should key off `Lesson.Completed`, not `Booking.Completed` — this is now safe to assume given the 1:1 cardinality in `ADR-0010`.
- Still **`[OPEN — Product Owner]`**: who/what marks a Lesson `Completed` (automatic at scheduled end time, tutor confirms, both parties confirm) — not invented here.
- **Resolved — Product Owner decision, 2026-10-04**: no-show grace period is 10 minutes; declaration is tutor-reported/manual for V1 (no classroom-attendance telemetry exists yet to automate this — do not invent automated detection). A student no-show has no refund consequence (the tutor is paid in full); a tutor no-show triggers a full refund to the student. See `ADR-0015` §6 and `docs/product/TRIAL_BOOKING_WORKFLOW.md` for the full policy and its mechanics on the `Booking` side (this is the same no-show policy, not a separate Lesson-level one — `Lesson.NoShow` and `Booking.NoShow` describe the same real-world event from each module's own state, per the note above).
