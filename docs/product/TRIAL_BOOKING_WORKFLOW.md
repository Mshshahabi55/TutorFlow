# Trial Booking Workflow

Trial booking is the marketplace's highest-stakes workflow: it's the first money a student spends, the first real signal a tutor gets, and the one place double-booking or double-charging directly damages trust. It is explicitly **not** simple CRUD ("create a Booking row"). This document describes the required workflow *shape*; it does not finalize any pricing/fee numbers (Product Owner). The mechanical shape below is now specified in full in `docs/domain/BOOKINGS_SPEC.md` and `ADR-0013`–`ADR-0017`; this document remains the narrative/product-facing walkthrough.

## Actors and modules involved

- **Bookings** — owns the booking/slot-hold lifecycle
- **Availability** — source of truth for whether a slot is actually bookable right now
- **Payments** — owns payment intent creation, provider webhook handling
- **Lessons** — created once a booking is confirmed, event-driven and asynchronous, never blocking confirmation (`ADR-0016`)
- **Notifications** — booking confirmation, reminders (consumes integration events, doesn't drive the workflow)

## Required workflow shape

```
1. Select Trial Duration + Date + Time
   → Student picks a candidate slot from Availability's computed open slots
   → Availability must reflect: tutor's weekly rules, time off, existing confirmed bookings,
     existing holds from OTHER in-flight checkouts, min-notice / max-booking-window rules

2. Temporary slot hold
   → Bookings creates a Held booking for this (tutor, slot) pair
   → Hold has a TTL (checkout timeout) of 10 minutes by default, stored as an explicit
     expiration timestamp and sourced from configuration (ADR-0003, Accepted)
   → While held, the slot must not be offered to a second student as open — enforced by a
     PostgreSQL exclusion constraint on (tutor_user_id, interval) for active holds/confirmed
     bookings (ADR-0003, mechanics finalized in ADR-0013), not an after-the-fact application check

3. Checkout
   → Student reviews price/duration, confirms
   → Transition: Held → PendingPayment

4. Payment
   → Payments creates a Stripe PaymentIntent (ADR-0004, Accepted)
   → Frontend completes payment (incl. SCA/3DS where required)
   → CRITICAL: the frontend's "payment succeeded" signal is NOT authoritative.
     Booking confirmation must wait for the provider's server-to-server webhook.

5. Webhook-driven confirmation
   → Payments receives + verifies the webhook signature
   → Payments publishes a payment-succeeded integration event (idempotent — see below)
   → Bookings, on receiving that event, transitions PendingPayment → Confirmed
   → Only NOW does a Lesson get created (exactly one Lesson per confirmed Booking —
     ADR-0010) / the slot become permanently unavailable to others

6. Expiry (unhappy path)
   → If payment doesn't complete within the hold TTL (same clock, checkout does not grant a
     fresh window — ADR-0013): Held/PendingPayment → Expired
   → Slot returns to Availability's open pool
   → [OPEN — Product Owner]: does an expired hold notify the student ("your slot was released")?

7. Booking Confirmation (happy path)
   → Student sees confirmation; Notifications fires (booking confirmed, reminder scheduling)

8. Post-confirmation changes
   → Reschedule: creates a NEW Booking via the same hold/availability-check path, and the
     original transitions to a terminal Rescheduled status, linked by RescheduledFrom/ToBookingId
     (ADR-0017) — it is a new slot negotiation, not a plain field update. Resolved (PO, 2026-10-04):
     at most once per booking, must be requested >=12 hours before the original's start, otherwise
     the cancellation policy applies instead. Whether the new booking requires fresh payment, and
     any reschedule fee amount, remain [OPEN — Product Owner]
   → Cancellation: interacts with Payments (refund requested by Bookings, processed and confirmed
     by Payments via its own event — ADR-0015). Resolved (PO, 2026-10-04): >=24 hours before start,
     full refund; <24 hours, no refund. V1 is full-refund-only (no partial tiers).
   → No-show: resolved (PO, 2026-10-04) — 10-minute grace period, tutor-reported/manual (no
     attendance telemetry exists yet); student no-show has no refund consequence, tutor no-show
     triggers a full refund to the student.
```

## Hard requirements (not negotiable without an ADR)

- **Timezone correctness**: all booking times stored in UTC. Student timezone and tutor timezone are separate metadata, not derived from the stored UTC value. Display conversion happens at the edge (API/frontend), never baked into persisted data.
- **Concurrency protection**: two students must not be able to both acquire a hold on the same (tutor, slot). This needs a real concurrency mechanism (DB-level uniqueness/locking on the module's own schema, or an equivalent guarantee) — not an application-level "check then write" race.
- **Idempotency**: payment-provider webhooks can and do arrive more than once for the same event. Processing the same webhook twice must not double-confirm a booking, double-publish an integration event side-effect, or double-credit a tutor's earnings.
- **Payments is authoritative over frontend**: no code path confirms a booking purely because the browser said payment succeeded.
- **Booking and Payment are separate state machines** (see `STATE_MACHINES.md`) that react to each other's events — Bookings doesn't reach into Payments' data, and vice versa.

## Resolved since this document was first written

- Hold TTL duration — 10 minutes, see `ADR-0003`.
- Booking↔Lesson relationship — always 1:1, including for recurring/subscription-driven lessons — see `ADR-0010`.
- Concurrency mechanism specifics — PostgreSQL exclusion constraint on `(tutor_user_id, interval)`, lazy per-tutor expiry sweep inside the hold transaction, conditional-update state transitions — see `ADR-0003`, finalized in `ADR-0013`.
- Payment provider choice — Stripe + Stripe Connect — see `ADR-0004`.
- Refund-direction coupling — Bookings requests a refund, Payments processes it and confirms via its own event; Bookings never decides `Refunded` unilaterally — see `ADR-0015`.
- Reschedule mechanism — creates a new linked Booking rather than mutating the original row — see `ADR-0017`.
- How Bookings validates a slot against Availability without duplicating its calculation logic — a narrow, read-only, synchronous cross-module query contract (`ITutorAvailabilityQuery`) — see `ADR-0014`. (Two prerequisite gaps in the already-built `Availability` module remain before this is implementable — see `BOOKINGS_SPEC.md` §5 and the `Availability` row in `DOMAIN_MODEL.md`.)
- How/when a Lesson gets created from a confirmed Booking, and how duplicate creation is prevented — see `ADR-0016`.
- **Product Owner decisions, 2026-10-04**: trial pricing formula (pro-rata from hourly rate, see `BOOKINGS_SPEC.md` §1.3); cancellation notice window and refund rule (≥24h full refund, <24h none, full-refund-only — `ADR-0015` §6); reschedule limit and notice window (once, ≥12h, else cancellation policy applies — `ADR-0017`); no-show grace period and consequences (10 minutes, tutor-reported, asymmetric refund — `ADR-0015` §6); partial refunds out of scope for V1 (`ADR-0004`); per-student simultaneous-hold limit (3, global — `BOOKINGS_SPEC.md` §4/§6); no separate Booking-level dispute concept in V1 (`BOOKINGS_SPEC.md` §10).

## Open questions this workflow surfaces

- **`[OPEN — Product Owner]`** Reschedule *fee* amounts, if any, beyond the no-refund cancellation fallback; whether a reschedule carries the original price forward or re-prices.
- **`[OPEN — Product Owner]`** Whether a Guest can enter this flow before creating an account (see `STUDENT_JOURNEY.md`).
- **`[OPEN — Product Owner]`** Exact earnings accrual/risk-window timing, payout frequency/minimum threshold/fees, VAT/tax/invoicing — Product/Legal decisions required before production payout/tax implementation, tracked in `docs/OPEN_QUESTIONS.md`; none block this workflow's own trial-booking mechanics.

## Explicitly not decided here

This document still defines the *shape* of correctness (what must be true), not an exhaustive restatement of every parameterizing rule — see the ADRs cross-referenced above for each locked decision's full detail and rationale. Genuinely still undecided: reschedule fee amounts, exact earnings-accrual timing, payout frequency/minimum/fees, VAT/tax/invoicing, and provider choice beyond Stripe itself (Connect account type is locked — Express — but no further provider-level decisions are made here).
