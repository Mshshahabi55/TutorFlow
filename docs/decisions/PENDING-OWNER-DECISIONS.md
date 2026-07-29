# Pending Owner Decisions

**Purpose:** every product decision currently blocking further backend implementation, consolidated into one sign-off document. Nothing here is new analysis — Sections 1 and 2 restate questions already recorded in `ADR-025` and `ADR-021` (both Proposed); Sections 3 and 4 name the minimal decisions needed before either area can even get a first ADR draft, since neither has any governing document yet. Per `CLAUDE.md`'s Stop Conditions, no implementation code exists or will be written against anything below until the relevant question is answered here (or in the ADR it comes from). Architecture for each area is already prepared and isolated behind interfaces where that was possible without deciding the undecided part — see "Already prepared, unblocked by this decision" under each section.

Answer inline (fill in the `→` blank) or reference a separate written decision. A "No" or "not yet" answer is a complete, valid answer — it keeps the current scope frozen, it does not block anything else in this document.

---

## 1. Scheduling — Recurring Availability, Buffer Time, Booking Notice
*Source: `docs/adr/ADR-025-scheduling-recurring-availability-and-booking-constraints.md` (Proposed, 2026-07-29)*

| # | Question | → Decision |
|---|---|---|
| 1 | Recurring availability shape: (A) authoring convenience that materializes into ordinary discrete `AvailabilitySlot` rows [recommended], or (B) a first-class, independently-editable `RecurrenceRule` aggregate? | → |
| 2 | Does Option A's "Student still books one already-concrete slot, no series ever exposed" design satisfy `PRODUCT_REQUIREMENTS.md` SCH-4's intent, or does SCH-4 need revisiting? | → |
| 3 | Should a Tutor be able to withdraw/delete an unconsumed `AvailabilitySlot` (no such capability exists today)? If so, any guard beyond "never withdraw an already-consumed slot"? | → |
| 4 | Blocked dates: skip-a-single-occurrence, block-a-whole-date, or both? | → |
| 5 | "Working hours" — confirm: no new Domain concept, purely a UI label over Option A's generation step [recommended], or an actual independently-stored template? | → |
| 6 | Buffer time: per-Tutor optional value [recommended]; what platform default if unset — zero (opt-in only) or some non-zero value? | → |
| 7 | **Minimum booking notice** (`DOMAIN_MODEL.md` Open Question 8): is there a floor? If so, how many hours/minutes before a slot's start? | → |
| 8 | **Maximum booking horizon**: is there a ceiling? If so, how many days ahead? Platform-wide (Admin-configured) [recommended] or per-Tutor? | → |
| 9 | Once Q7/Q8 are set, does the same check apply to `RescheduleSessionCommand`'s new time [recommended, to prevent bypass via reschedule]? | → |

**Highest-impact single question:** Q7 (minimum booking notice) — currently a Student can book a session starting one minute from now. Answering just Q7/Q8 is independent of Q1–Q6 and can ship alone.

**Already prepared, unblocked by this decision:** Phase 8a's same-Tutor overlap guard (`AvailabilitySlot.Declare`) and the month-calendar UX (`MonthCalendarGrid`) are shipped and require no revision — the ADR extends both, never replaces them.

---

## 2. Learning Plans & Payments
*Source: `docs/adr/ADR-021-learning-plans-enrollment-and-settlement-architecture.md` (Proposed, 2026-07-27) — supersedes `ADR-020` (Proposed) in full. This is one decision surface: Learning Plans cannot ship without payments, and the only proposed payments design in this repo is inside this ADR.*

| # | Question | → Decision |
|---|---|---|
| 1 | **Gate.** Does the owner widen v1 scope to include payments at all? *(Everything below is moot if "no.")* | → |
| 2 | Does an Enrollment-driven multi-session purchase satisfy `PRODUCT_REQUIREMENTS.md` SCH-4 ("no recurring bookings"), or does SCH-4 need amending? | → |
| 3 | Ratify "Enrollment & Billing" as a fifth bounded context (extends `ADR-002`)? | → |
| 4 | Is a Learning Plan a platform-wide catalog entry (any Tutor sharing its Subject), or scoped to specific Tutors? | → |
| 5 | Insufficient Tutor availability at checkout: block checkout entirely [recommended], or support partial fulfillment/waitlisting? | → |
| 6 | Post-payment slot-generation partial failure: "Enrollment Active, generation incomplete, Admin-visible flag" [recommended], or auto-refund/auto-retry? | → |
| 7 | Enrollment Cancel/Pause: auto-cancel already-generated future Sessions, or leave for manual cleanup? Who may Pause — Admin only, or also Tutor/Student? | → |
| 8 | Renewal: create a new `Enrollment`, or extend/reactivate an existing `Completed`/`Expired` one? | → |
| 9 | Refund proration: a formula (e.g. pro-rated by `RemainingSessions`), or always an Admin manual judgment call? | → |
| 10 | Settlement per-session amount: even split, zero commission [recommended], or a platform commission — and if so, what rate (`BUSINESS_MODEL.md` Open Question 5)? | → |
| 11 | Does the current no-payment, single-session `BookSessionCommandHandler` path retire once Enrollments ship, or stay as a parallel path? | → |
| 12 | Audit every new event type in full [recommended], or a narrower subset? | → |
| 13 | Stripe explicitly excluded (per `ADR-018`) — confirming, not asking. | *(no action needed)* |

**Already prepared, unblocked by this decision:** the frontend Learning Plan components (`LearningPlanCard`, `PlanBadge`, `PlanFeatureList`) are built and wired as honest "coming soon" placeholders everywhere a real plan would eventually render — no rework needed once Q1–Q3 are answered, only a real data source to point them at.

---

## 3. Reviews & Ratings
*Source: none — explicitly out of scope per `PRODUCT_REQUIREMENTS.md` Decision C.12. No ADR exists because no scope decision authorizes drafting one yet.*

| # | Question | → Decision |
|---|---|---|
| 1 | **Gate.** Reopen Decision C.12 and bring reviews/ratings into v1 scope at all? | → |
| 2 | If yes: who may leave a review — any Student/Parent-Guardian with a `Completed` Session against that Tutor only, or anyone? | → |
| 3 | Can a Tutor respond to a review? Can Admin/Staff remove one (and under what standard — abuse only, or any reason)? | → |
| 4 | Does a Tutor's aggregate rating factor into Discovery search ranking (a new, currently-nonexistent ranking concept — `DOMAIN_MODEL.md` Open Question 11 territory)? | → |
| 5 | Is a rating a plain 1–5 star average, or does it need sub-categories (e.g. punctuality, communication) — a materially larger design? | → |

**Architecture prepared without deciding the above:** none yet — there is no safe partial implementation of a feature explicitly marked out of scope. The one thing already true: `TutorDto`/`SessionDto` have no rating field, and no frontend surface fabricates one (`TrustIndicators`' own doc comment is explicit that no such data exists in this API).

---

## 4. Disputes, Abuse Reports, Moderation
*Source: none — never mentioned in `PRODUCT_REQUIREMENTS.md`, `DOMAIN_MODEL.md`, or any Accepted ADR. `ADR-003`'s `ResolveBookingConflict` permission exists in the enum with zero implementation behind it (`CLAUDE.md`'s own cautionary example of what not to repeat) — this section is that permission's actual design work, not yet done.*

| # | Question | → Decision |
|---|---|---|
| 1 | **Gate.** Bring any form of dispute/report/moderation capability into v1 scope? | → |
| 2 | Scope: (a) a Student/Tutor reporting the *other party* (abuse/no-show dispute), (b) an Admin unilaterally cancelling/reassigning a Session ("resolve booking conflict," the already-named-but-unimplemented permission), or both? | → |
| 3 | What actions can an Admin actually take on a disputed Session — cancel only, forced reschedule, refund (blocked on Section 2's payment decision), suspend a party? | → |
| 4 | Does a report/dispute need its own Domain aggregate + audit trail (new bounded context, same category of decision `ADR-002`/`ADR-022`/`ADR-023` each required), or is it an extension of the existing Session/Oversight surface? | → |

**Architecture prepared without deciding the above:** `Permission.ResolveBookingConflict` already exists in the enum (flagged in `CLAUDE.md` as currently ungoverned) — any eventual implementation has a permission name to attach to already; nothing else is safe to build ahead of Q1–Q4.

---

## How to use this document

- Fill in a `→` line, or write "see [external decision doc/ticket]" and link it.
- Partial answers are fine — e.g. answering only Section 1 Q7–Q9 unblocks minimum/maximum booking notice without deciding recurring availability at all.
- Once a section's gating question(s) are answered, implementation resumes exactly where its ADR left off — no re-design needed, only ratification.
