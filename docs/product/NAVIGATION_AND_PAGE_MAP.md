# Navigation & Page Map

Page inventory per surface, as named in the product brief. This is an information-architecture map, not wireframes or visual design — see `docs/design/` for UI direction.

## Public Website (unauthenticated-first, but usable while logged in)

| Page | Purpose | Primary module(s) behind it |
|---|---|---|
| Home | Marketing entry, value prop, subject shortcuts, CTA into Find Tutors / Become a Tutor | Subjects (featured), Tutors (featured/highly-rated) |
| Find Tutors | Search + filter entry point | Search, Subjects, Availability (for filter facets) |
| Tutor Profile | Public tutor detail page | Tutors, Reviews, Availability (for booking widget) |
| Subjects | Subject browse/landing pages (e.g. "English tutors", "Math tutors") | Subjects, Search |
| How It Works | Static/marketing | — |
| For Business | Marketing (B2B angle) — **`[OPEN — Product Owner]`**: is B2B a real v1 product surface (different contracts, invoicing, seats) or purely a marketing page with no backend behind it? | Unclear — see Open Questions |
| Become a Tutor | Entry into tutor application | Tutors (application start) |
| Login / Signup | Auth entry | Identity |
| Help / Support | Static/FAQ, possibly a support ticket entry point | Notifications? Messaging? — **`[OPEN — Architect]`**: is "Support" its own thing or Messaging with an Admin participant? |
| Language / Currency switcher | Locale/currency preference | Cross-cutting — **`[OPEN — Product Owner]`**: which languages/currencies are actually supported in v1 (see `PRODUCT_VISION.md`) |

## Student App (authenticated, role = Student)

| Page | Purpose | Primary module(s) |
|---|---|---|
| Home / Dashboard | Upcoming lessons, recommended tutors, continue-learning prompts | Lessons, Bookings, Tutors |
| Find Tutors / Search / Advanced Filters | Same as public, now personalized (favorites, history) | Search, Students |
| Tutor Profile | Same page, now with Message/Favorite/Book actions enabled | Tutors, Messaging, Students |
| Favorite Tutors | Saved tutors list | Students |
| Messaging | Conversations with tutors | Messaging |
| Trial Booking → Checkout → Confirmation | See `TRIAL_BOOKING_WORKFLOW.md` | Bookings, Availability, Payments |
| My Lessons / Calendar | Upcoming + past lessons, reschedule/cancel entry points | Lessons, Bookings |
| Classroom | In-lesson experience | Lessons (live session/classroom lifecycle + video abstraction) — resolved, see `ADR-0006` |
| Learning | Materials, homework, progress | Learning (durable content, split from Classroom's live session per `ADR-0006`) |
| Reviews | Leave/view reviews for completed lessons | Reviews, Lessons |
| Subscription | Manage active subscription(s) | Subscriptions |
| Payment Methods | Saved payment methods | Payments |
| Notifications | In-app notification center | Notifications |
| Profile / Account Settings | Student profile + account management | Students, Identity |

## Tutor App (authenticated, role = Tutor)

| Page | Purpose | Primary module(s) |
|---|---|---|
| Onboarding Wizard (17 steps) | See `TUTOR_JOURNEY.md` | Tutors |
| Profile / Description / Languages / Subjects / Specialties / Experience / Education / Certificates / Intro video | Post-publish profile management (same fields as onboarding, editable) | Tutors, Subjects |
| Pricing | Rate management | Tutors |
| Availability / Time off / Booking settings | Scheduling configuration | Availability |
| Calendar | Tutor's own schedule view | Availability, Bookings, Lessons |
| Students | List of the tutor's own students | **Resolved (2026-10-04, A39)**: `Bookings`-owned read projection (distinct students with a `Confirmed`/`Completed` booking) — see `docs/product/TUTOR_DASHBOARD_SPEC.md` |
| Messages | Same Messaging module, tutor-side view | Messaging |
| Lessons | Lesson history/upcoming, tutor-side | Lessons |
| Earnings / Payouts | Balance, payout history, payout method | Payments |
| Insights | Performance analytics (profile views, conversion, ranking signals) | **Resolved (2026-10-04, A39)**: BFF composition of `Search`/`Bookings`/`Reviews` reads, no owning module — see `docs/product/TUTOR_DASHBOARD_SPEC.md` |
| Notifications | Same as Student, tutor-side | Notifications |
| Account settings | Tutor account/credential management | Identity, Tutors |

## Admin

See `ADMIN_JOURNEY.md` for workflow detail. Page inventory per the brief: Dashboard, Students, Tutors, Tutor Applications, Tutor Verification, Tutor Profiles, Suspended Accounts, Subjects, Languages, Specialties, Search Management, Bookings, Lessons, Subscriptions, Payments, Refunds, Disputes, Payouts, Reviews, Messages, Support, Reports, Promotions, Notifications, Audit Logs, Feature Flags, System Settings.

**Resolved** (`ADR-0008`, Accepted): Admin is not one of the 14 bounded contexts, and no 15th "Admin" module is created. **Composed Admin** is the adopted model — each owning module exposes admin-facing endpoints/contracts (e.g. `Tutors` exposes `POST /admin/tutors/{id}/verify`), and the Admin UI is a BFF/aggregation layer with no business logic of its own. Promotions, Audit Logs, Feature Flags, and System Settings still don't map to any existing module and remain explicitly unresolved — see `docs/OPEN_QUESTIONS.md`.

## Navigation structure

**`[OPEN — Product Owner]`**: exact primary nav items, their order, and mobile nav collapse behavior are UX decisions not made here — this document is the page inventory the nav must cover, not the nav itself.
