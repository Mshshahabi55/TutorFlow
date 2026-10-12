# TutorFlow Frontend Implementation Roadmap

Derived from `PREPLY_UI_UX_AUDIT.md`, `PREPLY_COMPONENT_INVENTORY.md`, `PREPLY_FORM_DATA_ENTRY_SPEC.md`, `PREPLY_WIZARD_COMPONENT_INVENTORY.md`, and `TUTORFLOW_WIZARD_ARCHITECTURE.md`. Research/specification only — no UI, no React/Next.js components, no backend changes made as part of this document.

Priority tiers per the user's own framing:
- **P0** — foundation / design system / navigation / auth / core marketplace
- **P1** — tutor discovery / profile / booking / checkout / lessons
- **P2** — messaging / learning / classroom / tutor dashboard
- **P3** — advanced polish / insights / edge cases

This roadmap respects TutorFlow's existing modular-monolith architecture: every frontend item below is sequenced *after* the backend module it depends on has its first real feature (per `CLAUDE.md`'s "Next Steps" — every module is currently an empty stub). No frontend item should be started before its listed backend dependency exists, and no frontend work should drive a backend module to add features speculatively (`CLAUDE.md`'s own anti-pattern).

---

## P0 — Foundation / design system / navigation / auth / core marketplace

| Item | Depends on (backend) | Notes |
|---|---|---|
| Design system primitives (`Button`, `FormField`, `Accordion`, typography/color/spacing tokens) | none | TutorFlow's own tokens — do not reverse-engineer Preply's unmeasured visual values (audit §5–7) |
| `AppNavBar`, `AppFooter` | none (static) | Minimal V1: logo, primary nav, login/account menu, basic footer links — skip the SEO link-farm footer columns until `P3` |
| Auth UI (login/register) | `Identity` — first real feature | JWT bearer already wired at the host level (`CLAUDE.md`); `Identity` module itself needs its first real registration/login feature built before this UI has anything to call |
| Tutor onboarding wizard shell + first steps (Account, BasicInfo) | `Identity`, `Tutors` (first features) | Build `WizardShell`/`WizardStep`/`WizardFooter` here — reused by every later wizard step and by the (simpler) trial-booking wizard |
| Tutor search (basic — no advanced filters yet) | `Search`, `Tutors`, `Subjects` (first features) | Ship a minimal list/grid with the one filter TutorFlow is confident about (subject) before building the fuller filter bar — the exact Preply filter set was **not reliably observable** (audit §27.1) and needs its own product decision, not a guess baked into `P0` |

## P1 — Tutor discovery / profile / booking / checkout / lessons

| Item | Depends on (backend) | Notes |
|---|---|---|
| `TutorCard`, full `TutorProfilePage` (About, subjects, reviews placeholder, `BookingPanel`) | `Tutors`, `Subjects`, `Availability` (pricing/availability features) | `ReviewList` can ship as an empty state until `Reviews` has real data |
| Trial booking wizard (`DurationStep`, `TimeSlotPicker`, `PaymentMethodSelector`, `CheckoutStep`) | `Bookings`, `Availability`, `Payments` (first real features, per `ADR-0013`/`ADR-0019`) | Reuses the `WizardShell` built in `P0`; this is the shorter of the two wizard instances — see `TUTORFLOW_WIZARD_ARCHITECTURE.md` |
| Booking confirmation screen | `Bookings` | Straightforward read of the booking just created |
| My Lessons (student) — upcoming list, cancel/reschedule actions | `Bookings`, `Lessons` | Maps to `ADR-0015`/`ADR-0017`'s existing cancel/reschedule rules — UI surfaces those rules, does not reinterpret them |
| Remaining tutor-onboarding wizard steps (Subjects, Specialties, Photo, Education, Certifications, Description, Video, Availability, Pricing, Preview, Submit, `CompletionState`) | `Tutors`, `Subjects`, `Availability` (fuller features) | Biggest single chunk of net-new UI work; sequence sub-steps by whichever backend write-endpoints land first |
| Basic tutor dashboard (status badge: Draft/Under Review/Approved/Rejected; calendar read) | `Tutors`, `Availability` | Full dashboard (earnings, students list) is `P2` |

## P2 — Messaging / learning / classroom / tutor dashboard

| Item | Depends on (backend) | Notes |
|---|---|---|
| Messaging UI (composer, conversation view, inbox) | `Messaging` (currently an empty stub — needs its first real feature) | Preply's messaging UI detail was **not reliably observable** this session (audit §19) — this needs either a follow-up audit pass or an independent TutorFlow UX design, not a guess |
| Review submission + display | `Reviews` (currently an empty stub) | Post-lesson prompt; feeds `TutorCard`/`TutorProfilePage`'s `RatingStars` |
| Subscription package picker + management | `Subscriptions`, `Payments` | Pricing/cadence pattern (6/12/20 lessons per 28 days) confirmed at a business-model level (audit §11) but TutorFlow's own package structure is a Product Owner decision, not a copy of Preply's numbers |
| Full tutor dashboard (calendar, students list, earnings, messages entry point) | `Tutors`, `Bookings`, `Lessons`, `Payments` | Layout itself was **not reliably observable** (audit §15) — design fresh against TutorFlow's actual data, not a guessed Preply layout |
| Learning/progress UI | `Learning` (currently an empty stub) | No Preply-sourced detail at all for this area; fully a TutorFlow product design exercise |
| Classroom (live lesson room) | `Lessons` (needs real-time/video infrastructure — not yet designed anywhere in TutorFlow's docs) | Largest unscoped item on this roadmap; treat as its own spec effort before estimating, not a roadmap line item yet |

## P3 — Advanced polish / insights / edge cases

| Item | Depends on (backend) | Notes |
|---|---|---|
| Marketing pages (How It Works, Become a Tutor landing, testimonials, FAQ accordion, benefit/feature cards) | none (static/CMS-like) | Content-only; no domain dependency |
| Full footer SEO link columns, location/subject landing pages | `Subjects`, `Search` | Growth/SEO tactic, not core UX — correctly last |
| Notifications UI (in-app notification center) | `Notifications` (currently an empty stub) | Backing events already exist conceptually (`BookingConfirmed` example in `CLAUDE.md`); UI is additive polish |
| Accessibility hardening pass | n/a (cross-cutting) | TutorFlow should set its own WCAG 2.1 AA bar regardless of Preply's unknown accessibility posture (audit §22) |
| Loading/empty/error state polish across all pages | n/a (cross-cutting) | None of Preply's specific treatments were observable (audit §21) — design independently |
| Responsive refinement beyond MVP breakpoints | n/a (cross-cutting) | Only one Preply responsive fact was confirmed (mobile app step-reordering in booking, audit §20) — not a basis for a full responsive spec |

---

## Sequencing notes

- **The tutor-onboarding wizard and the trial-booking wizard are the two biggest UI investments and both sit partly in `P0`/`P1`.** They share the `WizardShell`/`WizardFooter`/`FormField` primitives (built once in `P0`), which is the main reason the design-system foundation is `P0` rather than deferred.
- **No frontend item in this roadmap should push a backend module to add speculative features.** Every "depends on" column names the minimal real feature needed, consistent with `CLAUDE.md`'s instruction to build backend features only when an actual requirement drives them — this roadmap *is* that driving requirement for the modules listed, but the backend work itself is out of scope for this document.
- **Items flagged "not reliably observable" in the audit (filters, dashboards, messaging UI, classroom) should not be estimated with false confidence.** Where this roadmap lists such an item, treat the backend-dependency column as a floor, not a complete spec — a short, focused design pass (possibly a follow-up audit with real browser access, see audit §27's recommendation) should precede implementation, not be skipped in favor of guessing.
