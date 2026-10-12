# Subscription Journey

## Flow (proposed shape)

```
Trial Completion / Continue Learning prompt
  → Student chooses a tutor-specific subscription plan
  → Checkout / Payment (reuses Payments, not Bookings — subscriptions are not a booking)
  → Subscription Active
  → Periodic lesson credits granted (ledger entries, not a mutable counter)
  → Student schedules lessons against available credit
  → Credit consumed on lesson scheduling/completion (exact trigger — [OPEN — Product Owner])
  → Renewal at cycle boundary (auto-renew assumed, not confirmed)
  → Cancellation / pause / plan change
```

## Why a ledger, not a counter

A `lessonsRemaining` integer on a `Subscription` row cannot answer "why does the student have 3 credits instead of 4" after the fact, cannot support partial refunds or credit expiry cleanly, and cannot support disputes ("I was charged for a credit I never got"). The required model is a **credit ledger**: every grant, consumption, expiry, and adjustment is its own immutable entry; the "balance" is a derived sum, not a stored field that gets mutated in place. This gives an audit trail for free and matches how Payments must already behave with money.

This is the only part of the subscription design treated as settled here — everything else below is explicitly open.

## What is NOT decided here

The brief is explicit that pricing, cancellation, rollover, and credit rules "must remain explicit product decisions and must not be invented." Accordingly, all of the following are **`[OPEN — Product Owner]`**:

- Subscription cycle length (the brief mentions a 28-day cycle as current product direction/inspiration, not a confirmed rule)
- Price per plan / per subject / per tutor
- How many lesson credits a cycle grants, and whether that's fixed or tutor-configurable
- Whether unused credits roll over to the next cycle, expire, or partially roll over
- Cancellation policy: effective immediately vs. end-of-cycle, and whether unused credits are refunded
- Whether a student can hold subscriptions with multiple tutors simultaneously
- Whether a subscription auto-renews by default and what the renewal-failure (payment declined) grace period is
- Plan upgrade/downgrade mid-cycle behavior (proration — almost certainly needed, but rule unset)

## Architecture questions

- **`[OPEN — Architect]`** Credit ledger ownership: lives in `Subscriptions` (recommended — it's the module's core invariant), with `Bookings`/`Lessons` consuming credit via an integration event/command rather than writing to the ledger directly.
- **Resolved** (`ADR-0010`): a subscription-covered lesson relates to `Booking`/`Lesson` exactly like a trial — each scheduled occurrence still creates its own `Booking` (going through the same slot-hold/concurrency protection as trial booking, per `ADR-0003`) and its own `Lesson`, just skipping the `Payments` step because credit already covers it. A `Subscription` is never itself a `Lesson`; it produces a series of `Booking` → `Lesson` pairs, one per occurrence.
- **`[OPEN — Architect]`** Renewal payment failure handling needs the same webhook-authoritative treatment as initial payment (see `STATE_MACHINES.md`) — a subscription must not silently stay `Active` on a failed renewal charge.

## Recommendation (non-binding)

Do not design or scaffold `Subscriptions` persistence until at least the pricing/cycle/rollover/cancellation questions above have Product Owner answers — the ledger *shape* is stable, but getting the *entry types* (what counts as a grant vs. an adjustment vs. an expiry) wrong before those rules exist would mean rework, not iteration.
