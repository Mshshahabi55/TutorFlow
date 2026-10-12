# ADR-0017: Reschedule Mechanism — New Booking Linked to the Original

**Status:** Proposed — requires Architect sign-off before `Bookings` is scaffolded

## Context

`docs/domain/STATE_MACHINES.md` has long flagged this as open: *"does rescheduling mutate this Booking (keeping one row with history) or create a new Booking referencing the old one? Affects audit trail and reporting."* This is a mechanical/architectural question, not a business-policy one. (The business policy — notice window and reschedule limit — is now locked for V1; see "Product Owner decision locked for V1" below. Fee amounts remain `[OPEN — Product Owner]`.)

**Correction (architecture review, 2026-10-04):** the version of this ADR originally circulated for sign-off described creating the new row and flipping the original without specifying a transaction boundary or an ordering between the two writes. That gap allows two concrete failure modes: (a) a crash between the two writes leaves an orphaned new booking with no corresponding flip on the original — two live reservations where there should be one; (b) two concurrent reschedule requests against the same `Confirmed` booking can both pass the "must be `Confirmed`" precondition, both create a new linked booking, and then race on flipping the original — the loser's new row is orphaned, with no original ever pointing back at it. §"Mechanics" below is added to close both.

## Decision

Rescheduling **creates a new `Booking` row and transitions the original to a terminal `Rescheduled` status**, rather than mutating the original row's `StartUtc`/`EndUtc` in place.

- The original `Booking` transitions `Confirmed` → `Rescheduled` (terminal; no further transitions out of `Rescheduled`) and gains a `RescheduledToBookingId` (`Guid?`) pointing at the new row.
- The new `Booking` is created via the *same* hold-creation path as any other booking (`ADR-0013`) — it is validated against `Availability` (`ADR-0014`) and protected by the same exclusion constraint — and carries a `RescheduledFromBookingId` (`Guid?`) pointing back at the original.
- The new `Booking` starts at `Held`/`PendingPayment` exactly like a fresh booking would, **unless** payment was already settled on the original, in which case it can be created directly at `Confirmed` without a new payment step — the exact rule for *when* a reschedule requires new payment vs. carries the original payment forward is a pricing/policy question, `[OPEN — Product Owner]`, not decided here. **Whichever branch applies, the mechanics below (one transaction, flip-first ordering) are identical** — the open pricing question only affects what `Status` the new row starts in, never how the two writes are sequenced or committed.

## Product Owner decision locked for V1 (2026-10-04): eligibility window and reschedule limit

Two of this ADR's previously-open items (reschedule limits; the notice-window portion of notice-windows/fees/limits) are now resolved. **Fee amounts remain open** — see "What this ADR does not decide" below.

- **A booking may be rescheduled at most once.** The booking being rescheduled must itself not already be a rescheduled-into booking that has itself been rescheduled again — i.e. `RescheduledFromBookingId` chains of length >1 are not permitted in V1. (A booking that was itself the *target* of a reschedule may still be cancelled normally; it simply cannot be rescheduled a second time.)
- **The reschedule request must be made ≥12 hours before the original booking's `StartUtc`.** This is an *eligibility* check on the reschedule endpoint itself, separate from and in addition to the existing `Confirmed`-only state-transition guard.
- **If the request is made <12 hours before `StartUtc`, the reschedule is not permitted — the cancellation policy applies instead** (`ADR-0015` §6: ≥24h-before-`StartUtc` full refund, <24h no refund, evaluated against the *original* booking's `StartUtc`). Since any instant <12 hours before `StartUtc` is also <24 hours before it, a late reschedule attempt that falls back to cancellation will, under the current cancellation policy, always resolve to "no refund" — this is a direct mechanical consequence of the two thresholds, not a separate rule being invented here. A client encountering this should present it to the user as "this booking can no longer be rescheduled; cancelling it now will not be refunded," not silently redirect to a cancellation.
- **The one-reschedule limit and the 12-hour window are both *new* eligibility checks on the reschedule endpoint, on top of the existing `Confirmed`-only guard** — they do not change the atomic flip-first mechanics in "Mechanics" below, which remain exactly as originally specified. None of this is implemented by this update; `RescheduleBooking.cs` does not yet check either condition.

## Mechanics: one transaction, flip-first ordering

Rescheduling is **one database transaction** containing exactly these steps, in this order:

1. **Conditionally transition the original booking**: `UPDATE bookings.bookings SET status = 'Rescheduled' WHERE id = @originalId AND status = 'Confirmed'` (the same conditional-`UPDATE`-with-affected-row-count pattern as every other state transition, `ADR-0013` §3 — no read-then-write gap).
2. **Verify exactly one row was affected.** If the affected-row count is `0`, the original was not `Confirmed` at the moment this statement ran (already rescheduled by a concurrent request, already cancelled, etc.) — **abort the transaction immediately and return a conflict (`409`) without creating anything.** This is the critical ordering choice: the flip is attempted *first*, before any new row exists, so a losing request never creates an orphan.
3. **Only if step 2 confirms exactly one row changed**, create the replacement `Booking` row (validated against `Availability` per `ADR-0014`, protected by the exclusion constraint per `ADR-0013` at `INSERT` time exactly like any other hold-creation) and set `RescheduledToBookingId` on the original to the new row's `Id`, and `RescheduledFromBookingId` on the new row to the original's `Id`.
4. **Commit both operations together.** If the new-row `INSERT` fails for any reason — including a `23P01` exclusion violation, because the requested new slot was taken in the interim — the entire transaction rolls back, including the flip from step 1. The original is left exactly as it was (`Confirmed`), not stranded in `Rescheduled` with no replacement. The client sees a single `409` and may retry with a different target slot.

This ordering means the only way to ever observe an original in `Rescheduled` status is if a replacement row genuinely exists and is linked — there is no intermediate, observable state where the original is `Rescheduled` but the replacement does not yet exist, and no way for a losing concurrent request to create an unlinked orphan booking. It also means the new-row-at-`Confirmed` branch (no payment step) carries no additional risk despite having no TTL safety net: if that branch's `INSERT` fails or the flip loses the race, the whole transaction rolls back and nothing is left behind, with or without a TTL.

## Rationale

- Reuses 100% of the existing hold/concurrency/availability-validation machinery for the new slot — no special-cased "mutate a confirmed row's interval" path that would need its own exclusion-constraint re-validation logic.
- Gives a clean, queryable audit trail for free: "this booking was rescheduled to/from that booking" is a plain foreign-key-shaped (though not an actual EF FK, since both ends are the same table) lookup, not reconstructed from a history log.
- Correctly handles the case where a `Lesson` was already created for the original `Confirmed` booking before the reschedule: the original's `BookingCancelled`-shaped signal (or a dedicated `BookingRescheduled` event — see `ADR-0016`) tells `Lessons` to retire the old `Lesson`; the new `Booking`'s eventual `BookingConfirmed` event creates a new one. Mutating the original row in place would instead require `Lessons` to somehow detect and reconcile an in-place time change on an already-existing `Lesson`, which is more complex and blurs "each module owns its own state" (`A6`).
- Matches the pattern already used elsewhere in this codebase for "this state requires a new, linked record rather than mutating history" (e.g. `Tutors`' rejected-application handling keeps the rejected record rather than resetting it in place).

## What this ADR does not decide

- Reschedule **fee amounts**, if any — resolved only in part: the notice window (≥12h) and the one-reschedule limit are locked above; whether a late or repeated reschedule attempt should carry an explicit fee (beyond simply falling back to the no-refund cancellation outcome) remains `[OPEN — Product Owner]`.
- Whether a rescheduled booking's new slot carries the original's price forward or is re-priced (`[OPEN — Product Owner]`).
- Whether `Lessons` receives a distinct `BookingRescheduled` event or is expected to infer the same outcome from `BookingCancelled` + a later `BookingConfirmed` — left to `Lessons`' own design, per `ADR-0016`.

## Consequences

- `Booking` gains two nullable self-referencing identifier columns: `RescheduledFromBookingId`, `RescheduledToBookingId` (plain `Guid?` columns, not EF navigation properties — consistent with this project's convention of avoiding FK-shaped coupling even within a single aggregate's own table where a simple opaque reference suffices).
- `Rescheduled` is confirmed as a terminal status (no transitions leave it), matching the single authoritative transition table in `BOOKINGS_SPEC.md` §2.
- The reschedule API endpoint (`BOOKINGS_SPEC.md` §4) returns the **new** booking's identifier; clients should follow it, not poll the original.
- The reschedule handler requires an explicit `Database.BeginTransactionAsync` (or an EF Core execution-strategy-wrapped equivalent) spanning steps 1–4 above — it is **not** expressible as a single `SaveChangesAsync` call, because step 2's abort-before-create branching requires inspecting the conditional update's result before deciding whether to add the new entity at all.

## Required concurrency test (specification — not implemented; `Bookings` does not exist yet)

Extends `BOOKINGS_SPEC.md` §9: two concurrent `POST /api/bookings/{id}/reschedule` requests against the same `Confirmed` booking (different target slots, so neither loses on the exclusion constraint itself) — exactly one must succeed with `201` and a correctly bidirectionally-linked pair; the other must receive `409` with **no booking row left behind** (assert the total row count for that student/tutor is "original + exactly one new row," not "original + two new rows, one orphaned"). A second variant targets the *same* new slot for both concurrent requests, to confirm the exclusion-constraint rejection path (step 4's rollback) also leaves no orphan.
