# Dependency Graph and Roadmap

Preserves the dependency graph and P0–P3 prioritization established across this project's prior reverse-engineering passes. This document does **not** re-prioritize anything merely because it is being reorganized here — new items discovered since the last roadmap pass are explicitly marked `DOCUMENTATION GAP / REQUIRES PRODUCT OWNER REVIEW` rather than silently inserted into an existing priority tier.

## Dependency graph (as previously established, unchanged)

```text
Identity
  └─→ Tutor Onboarding ──→ Tutor Approval (needs an Admin role — doesn't exist yet)
                                  ──→ Tutor Profile ──→ Search
                                                           └─→ Availability
                                                                  └─→ Booking
                                                                         └─→ Payment ──→ [Stripe Elements — DONE this project]
                                                                                └─→ BookingConfirmed event ──→ Lesson
                                                                                        ├─→ Student/Tutor Dashboard
                                                                                        └─→ Classroom

Independent branch:
  Subscriptions ──→ Tutor Accept/Decline (needs a Balance/Credit concept)

Independent of the above spine (parallel, no hard blocking dependency on the main chain):
  Messaging — no hard dependency beyond Identity
  Reviews — depends on Lesson reaching a Completed state (now newly concrete: the 15min/72h
            auto-confirmation rule, see 08-state-machines.md)
  Notifications — benefits from existing early, no hard blocking dependency
  Learning — depends on Classroom existing (homework/lesson-plans live inside the lesson room)
```

**New, confirmed refinement this pass:** Review eligibility's dependency on "Lesson reaching Completed" is no longer abstract — it now has a concrete, sourced trigger rule (lesson auto-confirms 15min/72h after end) that Lessons' own transition logic must implement before Reviews can ever correctly gate on it.

## P0 — required for core product

- **Lesson auto-confirmation → My Lessons.** ✅ **STATUS: IMPLEMENTED** (V1 scope). The Product Owner approved a Classroom-independent V1 rule — `Scheduled → Completed` at `EndUtc + 15 minutes`, flat, applying regardless of Classroom (which doesn't exist yet) — recorded as an **INTENTIONAL DEVIATION** from Preply's real Classroom-presence-aware rule, not a guess. Implemented as a lazy, read-time conditional-UPDATE sweep (same mechanism as Bookings' hold-expiry check), triggered from `GetMyLessons`. `/my-lessons` + `GET /api/lessons/me` are real, tested, ownership-enforced, and now correctly show `Completed` lessons with no manual action required. Revisit this rule once Classroom exists.
- **Classroom.** The actual tutoring-delivery experience does not exist at all. Confirmed the single largest remaining build in this entire roadmap; sequenced after Lesson auto-confirmation because Lessons must exist as a real, completable entity first.
- **Real browser payment (Stripe Payment Element).** **DONE this project** — moved from P0-open to MATCHED; see `13-tutorflow-current-state-and-parity.md`.

## P1 — required for complete major journeys

- **Tutor application review/approval pipeline — ✅ STATUS: IMPLEMENTED** (ADR-0028). Real `Admin` role (Identity, internal-provisioning-only per `ROLES_AND_PERMISSIONS.md`), five separate admin HTTP endpoints over the already-existing, already-fully-tested `TutorApplication` state machine (`BeginReview`/`Approve`/`Reject`/`Publish`/`Suspend`), a minimal admin review-queue UI (`/admin/tutor-applications`), server-enforced self-action guard, and regression tests for every transition/authorization case. Admin review *UI/UX* is this project's own engineering proposal (Preply's admin tooling is not publicly observable) — not reverse-engineered parity. Admin sub-roles, a reinstate-from-Suspended action, and an audit log remain unbuilt (tracked in ADR-0028).
- Block/report feature (small, independent, no architectural risk).
- Tutor Insights dashboard, read-only MVP (Overview + Earnings sections first, reusing existing data).
- Subscriptions (core: 28-day cycle + auto-renew + cancel first; defer pause/transfer/multi-tutor to a second pass per the existing recommendation in `docs/product/SUBSCRIPTION_JOURNEY.md` not to scaffold persistence before pricing/cycle rules exist — those rules are now DOCUMENTED, so this recommendation's blocking condition is resolved).
- Social login (Google/Facebook/Apple ID).
- Messaging (still a stub, independently buildable). **Notifications — ✅ STATUS: IMPLEMENTED for its first slice (ADR-0029).** Real transactional email infrastructure (Postmark, `IEmailSender` abstraction) and the abandoned-checkout recovery email (`18-screenshot-evidence.md` Part C, real email evidence) are built, tested end-to-end (Bookings → Wolverine → Notifications), and idempotent. The tutor-message notification email (Part C, SS49–SS50) remains unbuilt — a separate roadmap item.
- **Reviews — ✅ STATUS: IMPLEMENTED,** including subcategory ratings (added in a follow-up pass after an independent parity audit re-verified the official source in full). Written review: real eligibility/submission/display, matching the documented Preply model exactly (Student↔Tutor relationship, 3rd completed lesson, one editable review). Subcategory ratings (Clarity/Progress/Preparation/Reassurance): real, separate eligibility (≥1 completed lesson, not 3), real once-per-calendar-month-capped submission (never editable), real per-dimension aggregate public display — matches the DOCUMENTED mechanics (S26) directly. Submission-via-Messages (the written review's other documented entry point) remains blocked on Messaging; reviewer-identity display remains an anonymous-only scoped simplification; the tutor's own-dashboard subcategory breakdown remains NOT VERIFIED, not built.
- Age/residency restriction enforcement.
- Rate limiting beyond what already exists (IP-level, on top of the already-confirmed-correct per-account lockout).

## P2 — important parity

- Time off frontend wiring (backend already real and tested).
- Profile Score, Super/Professional Tutor criteria computation.
- Account-standing tiering (voluntary hide / auto-hide / full suspension — currently one undifferentiated state).
- Google Calendar sync (tutor and student side).
- Cancellation-window spec correction (24h → 12h) — pure documentation fix, zero code risk, should land before any refund-building work. Now confirmed by direct OBSERVED evidence at real Preply checkout (`18-screenshot-evidence.md` SS38), not just a help-center article — the strongest evidence tier this directory uses.
- Review-dispute path.
- Taxes & Fees presentation — blocked on a Product Owner decision, not merely an engineering task.
- Tutor Dashboard (unified home page beyond the existing Availability-only page).
- Search: descriptor tags, "active student"/"lessons taught" card stats (the latter depends on Lessons/Reviews existing first).

## P3 — advanced parity

- TalkNow and Lesson Insights — **explicitly not yet scoped**; both require a dedicated research pass before any priority placement beyond this lowest tier. Do not build either speculatively.
- Price-display copy fix ("€X 50-min lesson" vs. "/hour") — cosmetic, no architecture impact, can land whenever convenient.
- Language/country switcher.

## Documentation gaps / requires Product Owner review (newly surfaced, not yet prioritized)

- **Tutor Application wizard step count** — screenshot evidence now confirms Preply's real flow has 8 steps and TutorFlow's *implemented* wizard also already has 8 steps; the remaining open question is step **content grouping**, not count. Still a structural product decision, not a simple backlog item. See `07-wizard-inventory.md`, `18-screenshot-evidence.md`, and `16-product-decisions-and-open-questions.md`.
- **Meaning of "2 free tutor trials"** — unclear whether this implies a TutorFlow-relevant allowance concept or is specific to Preply's own account-state messaging. Not prioritized until interpreted.
- **Commission figures (100% trial / 18–33% standard)** — now available as a concrete reference, but adopting them is a Product Owner choice, not an automatic roadmap insertion.
