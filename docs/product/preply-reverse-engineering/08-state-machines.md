# State Machines

All Preply-side state machines below are reconstructed from **DOCUMENTED** official sources — none were directly browser-observed (none are observable without an authenticated account and real transactions). TutorFlow-side states are drawn from direct repository inspection (current as of this project's most recent audit) and are cited as such.

## TutorApplication (Tutor onboarding)

```text
Draft
  → Submitted            [DOCUMENTED — review begins; official sources disagree on duration,
                           3 vs. 5 business days, see 07-wizard-inventory.md]
  → Approved / Rejected  [DOCUMENTED — rejection reasons known, exact rejection UI NOT VERIFIED.
                           Resubmission after rejection: NOT VERIFIED (corrected 2026-10-06 —
                           neither official source states the mechanism; TutorFlow's own
                           ReopenApplicationForRevision is a TutorFlow design decision, not
                           confirmed Preply parity, see ADR-0028)]
  → Published            [OBSERVED — a real public tutor search page + a real public tutor
                           profile were directly browsed anonymously (01-real-user-observations.md):
                           approved/active tutors are genuinely publicly discoverable. Whether
                           Preply's own back-office models "Approved" and "Published" as two
                           separate admin actions (vs. one) is NOT VERIFIED/not publicly observable
                           — corrected 2026-10-06, see ADR-0028]

Published → Voluntarily Hidden   [DOCUMENTED — tutor-initiated, instant, reversible]
Published → Automatically Hidden [DOCUMENTED — system-triggered: no-show reports, 24h non-reply,
                                   harassment/fraud reports, broken intro video; self-resolves for
                                   the responsiveness case, needs Support for the rest]
Any → Suspended/Terminated       [DOCUMENTED, medium confidence — ToS breach; balance forfeited]
Published → Auto-suspended       [DOCUMENTED, medium confidence — 180 days inactivity]
```

TutorFlow's current implementation: a single `Status` field (Draft/Submitted/UnderReview/Approved/Rejected/Published/Suspended) with only one undifferentiated `Suspend()` transition — does not distinguish the three Preply-confirmed severities above (voluntary hide / auto-hide / full suspension). See `13-tutorflow-current-state-and-parity.md`.

## Booking / Trial

```text
Available slot
  → Held            [TutorFlow: OBSERVED in own code — 10-minute TTL, exclusion-constraint-backed]
  → PendingPayment   [TutorFlow: OBSERVED in own code — checkout transition]
  → Confirmed        [TutorFlow: OBSERVED in own code — webhook-driven, never frontend-asserted]
  → Expired          [TutorFlow: OBSERVED in own code — lazy per-tutor sweep (ADR-0013) AND, as
                       of ADR-0029, a new time-driven BookingHoldExpirySweepService — the lazy
                       sweep alone cannot serve as a notification trigger, see ADR-0029 §3]
  → Cancelled        [TutorFlow: OBSERVED in own code]
  → Rescheduled       [TutorFlow: OBSERVED in own code — creates a new linked Booking]

Expired → BookingHoldExpired (integration event) → Notifications.SendAbandonedCheckoutRecoveryEmail
  [TutorFlow: OBSERVED in own code, ADR-0029 — the TutorFlow equivalent of the OBSERVED Preply
   abandoned-slot recovery email (18-screenshot-evidence.md SS51–SS52). Trigger timing is an
   INTENTIONAL ENGINEERING DECISION (60s sweep interval), not a Preply-verified number — Preply's
   own delay/retry/scheduler internals remain NOT VERIFIED.]
```

Preply-side confirmation: the booking/payment/confirmation sequence itself (slot → hold-equivalent → pay → confirmed) is **DOCUMENTED** at the level of "trial lessons are paid at booking, and the lesson then appears in My Lessons" — Preply's own internal hold/TTL/concurrency mechanics are **unknown and were never investigated** (per this project's standing rule not to speculate about a third party's internal implementation). TutorFlow's mechanics here are an independently-designed implementation of the *observable* requirement, not a reverse-engineered copy of Preply's internals.

**Tutor accept/decline gate — a confirmed, deliberate TutorFlow deviation.** Preply's booking flow includes a tutor accept/decline step (DOCUMENTED) that TutorFlow's state machine does not have — this is tracked as an INTENTIONAL DEVIATION pending the balance/credit system it depends on (see `16-product-decisions-and-open-questions.md`).

## Payment

```text
Created → RequiresAction → Processing → Succeeded | Failed
```

TutorFlow's own implementation: **OBSERVED in own code**, matches this shape, backed by real Stripe PaymentIntents and a real Payment Element in the browser (built this project). Preply's own internal payment state machine: **never investigated** — out of scope by this project's own rule (Preply's payment UX is the benchmark; Stripe is TutorFlow's independently-chosen implementation).

## Lesson

```text
Scheduled
  → auto-Confirmed  [DOCUMENTED — ≈15 min after scheduled end inside Classroom, 72h otherwise,
                      unless the student confirms sooner]
```

This is a **newly-sourced, concrete finding with no prior TutorFlow equivalent**: TutorFlow's `Lessons` module currently only ever creates a row in `Scheduled` status (via the `BookingConfirmed` consumer, built this project) — no auto-confirmation transition exists at all. See `13-tutorflow-current-state-and-parity.md` and the P0 roadmap item in `15-dependency-graph-and-roadmap.md`.

Who/what marks a lesson `Completed`/`NoShow` beyond the auto-confirmation rule above remains **NOT VERIFIED** in further detail (e.g., exact no-show declaration UI).

## Review

```text
(no review possible) → Eligible (after 3rd completed lesson with that tutor)  [DOCUMENTED]
  → Submitted  [DOCUMENTED]
  → Tutor can Respond  [DOCUMENTED]
  → Subcategory score can be challenged only via Support, policy-violation grounds only [DOCUMENTED]
```

## Subscription

```text
(none) → Active            [DOCUMENTED — 28-day cycle, 4-lesson minimum]
Active → Paused            [DOCUMENTED — up to 20 days, once per cycle, can still take lessons]
Paused → Active            [DOCUMENTED — "Renew subscription now", or automatically at cycle end]
Active → Cancelled         [DOCUMENTED — no fee, no lock-in, already-booked lessons protected]
Cancelled → Active         [DOCUMENTED — resubscribe anytime]
```

Credit-transfer-between-tutors is a parallel, cross-cutting action rather than a state transition of a single subscription — see `11-payments-subscriptions-and-economy.md`.

## Account standing (cross-reference)

Fully detailed in `04-platform-trust-and-safety.md` — repeated here only as a pointer since it's a genuine state machine in its own right (three independent severities, not a simple suspended/active binary).

## What is NOT reconstructed here

Messaging and Notification "states" (read/unread, delivered/failed) were not investigated with enough depth to produce a state machine — see `12-classroom-messaging-reviews-notifications.md` for what is and isn't known.
