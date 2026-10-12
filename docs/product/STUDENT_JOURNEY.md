# Student Journey

## End-to-end flow

```
Landing Page
  → Find Tutors
  → Search / Filters
  → Tutor Profile
  → (Message | Favorite | Book Trial)
  → [Book Trial path] Select Trial Duration → Select Date → Select Time
  → Checkout → Payment
  → Booking Confirmation
  → Classroom (at lesson time)
  → Trial Completion
  → Continue Learning
  → Subscription (optional)
  → My Lessons → Schedule Lessons → Classroom → Lesson Completion
  → Review / Learning / Next Lesson
```

This is the marketplace's primary conversion funnel. The trial-booking segment (Select Trial Duration → Payment → Confirmation) is treated as its own transactional workflow, not simple CRUD — see `TRIAL_BOOKING_WORKFLOW.md`.

## Stage detail

### 1. Discovery (Landing → Find Tutors → Search/Filters)

Student arrives, states or refines intent (subject, language, price, availability, native speaker, etc. — full filter list in `DOMAIN_MODEL.md` under Search). Guest-accessible. No decision needed here beyond what's in `NAVIGATION_AND_PAGE_MAP.md`.

### 2. Evaluation (Tutor Profile)

Student reviews profile: bio, video, pricing, reviews, availability preview. Can Message, Favorite, or Book Trial. Favorite and Message require a Student account (prompt to sign up/login if Guest) — **`[OPEN — Product Owner]`**: can a Guest start a trial booking and be prompted to create an account mid-flow (common marketplace pattern), or must signup happen before booking starts? This materially changes the Trial Booking workflow's entry state.

### 3. Trial booking (Select Duration/Date/Time → Checkout → Payment → Confirmation)

See `TRIAL_BOOKING_WORKFLOW.md` in full. Summary: slot selection against the tutor's real-time availability, a temporary hold while checkout/payment completes, payment-provider-confirmed (webhook-authoritative) booking confirmation, with explicit handling for hold expiry, concurrency (two students can't hold/pay for the same slot), and idempotency (retried payment confirmations don't double-book or double-charge).

### 4. Trial lesson (Classroom → Completion)

At the scheduled time, both parties join the Classroom (video/chat owned by `Lessons`; materials/homework owned by `Learning` — see `ADR-0006`). On completion, the Lesson transitions to `Completed` (see `STATE_MACHINES.md`), unlocking: review eligibility, "Continue Learning" prompts, and tutor earnings accrual.

### 5. Conversion (Continue Learning → Subscription | My Lessons)

Post-trial, student is prompted toward either (a) booking more one-off lessons, or (b) subscribing to a recurring plan with that tutor. See `SUBSCRIPTION_JOURNEY.md` — the exact conversion offer (pricing, discount vs one-off, which comes first in the UI) is a Product Owner decision, not assumed here.

### 6. Ongoing (My Lessons → Schedule → Classroom → Completion → Review/Next)

Recurring loop. Reschedule/Cancel flows must respect `Availability` and `Bookings` rules. **Resolved (Product Owner, 2026-10-04)**: cancellation ≥24h before the lesson is a full refund, <24h is no refund (full-refund-only, no partial tiers); reschedule is allowed at most once and only ≥12h before the lesson, otherwise the cancellation policy applies instead — see `ADR-0015` §6 and `ADR-0017`. Reschedule *fee* amounts beyond that remain **`[OPEN — Product Owner]`**. Review is gated on a real completed Lesson/Booking relationship (see `DOMAIN_MODEL.md` Reviews module note) — no reviews without a verified lesson.

## States to design for at every stage (not deferred to "polish later")

Per instruction, loading/empty/error/validation/success/disabled/permission states must be considered now, not bolted on at the end. Concretely for this journey:

- **Empty states**: no search results for a filter combination; no favorites yet; no lessons yet (new student); no messages yet.
- **Error states**: payment failure mid-checkout; slot no longer available when checkout completes (lost the hold race); tutor becomes unavailable after favoriting; classroom fails to connect.
- **Permission states**: Guest trying to message/book (redirect-to-auth, not a dead end); Student trying to access another student's lesson/classroom.
- **Validation states**: trial duration/time selection against real constraints (min notice, max window — see `DOMAIN_MODEL.md` Availability).

See `docs/design/UX_STATES_PRINCIPLES.md` for the cross-cutting policy.
