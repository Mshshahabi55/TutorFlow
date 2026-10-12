# Feature Inventory

Every Preply capability discovered across all reverse-engineering passes this project, with verification label. Features already covered in depth elsewhere in this directory are listed here with a cross-reference rather than restated in full.

| Feature | Verification | Detail / cross-reference |
|---|---|---|
| Search (filters, sort, pagination) | OBSERVED | `01-real-user-observations.md` [3] |
| Tutor cards (price, rating, badges, descriptor tags, active-student count, lessons-taught count) | OBSERVED | `01-real-user-observations.md` [3] |
| Tutor public profile | OBSERVED | `01-real-user-observations.md` [4] |
| Tutor badges (Professional, Super Tutor, "Top X%") | OBSERVED (presence) / DOCUMENTED (eligibility criteria) | `03-tutor-journey.md` |
| Descriptor tags (e.g. "Engaging and encouraging") | OBSERVED | New finding, real-browser pass; not previously documented anywhere |
| Review subcategory ratings (Clarity/Progress/Preparation/Reassurance), shown publicly | OBSERVED | Upgraded from NOT VERIFIED this pass — `01-real-user-observations.md` [4] |
| Availability calendar widget (presence) | PARTIALLY OBSERVED | Widget visible; time-slot click behavior NOT VERIFIED |
| Trial lessons (25/50 min, one per tutor, duration locked) | DOCUMENTED | `02-student-journey.md` |
| Trial rejection + balance transfer | DOCUMENTED | `02-student-journey.md`, `09-edge-cases.md` |
| "2 free tutor trials" messaging | OBSERVED (raw text only) | Meaning not interpreted — `16-product-decisions-and-open-questions.md` |
| Booking / checkout / payment | DOCUMENTED | `11-payments-subscriptions-and-economy.md` |
| Payment methods (cards, Apple Pay, Google Pay, PayPal, iDEAL) | DOCUMENTED | `11-payments-subscriptions-and-economy.md` |
| Taxes & Fees checkout line | DOCUMENTED | `11-payments-subscriptions-and-economy.md` |
| My Lessons / Schedule | DOCUMENTED | `02-student-journey.md` |
| Lesson auto-confirmation (15min/72h rule) | DOCUMENTED | `08-state-machines.md` |
| Preply Classroom (video, chat, notes, whiteboard, materials, screen share) | DOCUMENTED | `12-classroom-messaging-reviews-notifications.md` |
| TalkNow (speaking-practice tool) | DOCUMENTED (existence only) | Depth NOT VERIFIED — flagged for future research |
| Lesson Insights (Classroom-adjacent analytics) | DOCUMENTED (existence only) | Depth NOT VERIFIED — flagged for future research |
| Messaging | DOCUMENTED | `12-classroom-messaging-reviews-notifications.md` |
| Reviews (eligibility, written text, subcategories) | DOCUMENTED + OBSERVED (subcategory display) | `12-classroom-messaging-reviews-notifications.md` |
| Notifications (settings page, categories) | PARTIALLY DOCUMENTED | `02-student-journey.md` — exact channel/type list NOT VERIFIED |
| Subscriptions (28-day cycle, pause, extra lessons, multi-tutor, credit transfer) | DOCUMENTED | `11-payments-subscriptions-and-economy.md` |
| Tutor Insights dashboard (6 sections) | DOCUMENTED | `03-tutor-journey.md` |
| Tutor earnings / commission (100% trial, 18–33% standard) | DOCUMENTED | `11-payments-subscriptions-and-economy.md` |
| Tutor withdrawal (methods, minimums, fees) | DOCUMENTED | `11-payments-subscriptions-and-economy.md` |
| Identity verification (payout-gated) | DOCUMENTED | `03-tutor-journey.md`, `04-platform-trust-and-safety.md` |
| Withdrawal secondary verification (SMS/Veriff) | DOCUMENTED | `03-tutor-journey.md` |
| Profile Score (0–100%) | DOCUMENTED | `03-tutor-journey.md` |
| Super Tutor (90-day rolling, 9 criteria) | DOCUMENTED | `03-tutor-journey.md` |
| Professional Tutor (static credential) | DOCUMENTED | `03-tutor-journey.md` |
| Baseline tutor search-visibility standard (response time, absences, reschedule/cancel rate) | DOCUMENTED | `03-tutor-journey.md` |
| Block / Report | DOCUMENTED | `04-platform-trust-and-safety.md` |
| Account standing (3 tiers) | DOCUMENTED | `04-platform-trust-and-safety.md` |
| Harassment policy / content moderation | DOCUMENTED | `04-platform-trust-and-safety.md` |
| 100% Satisfaction Guarantee / disputes | DOCUMENTED | `04-platform-trust-and-safety.md` |
| Age restrictions | DOCUMENTED, medium confidence | `04-platform-trust-and-safety.md` |
| Google Calendar integration (tutor AND student side) | DOCUMENTED | `02-student-journey.md`, prior session |
| Tutor Application wizard | DOCUMENTED (7 sections, official) vs. an older, unverified 17-step TutorFlow assumption | `07-wizard-inventory.md` |
| Cancellation (12h rule) | DOCUMENTED | `09-edge-cases.md` |
| Help Center / Support | OBSERVED (structure) / DOCUMENTED, medium confidence (contact mechanism) | `01-real-user-observations.md` [5] |
| Language/currency switcher | OBSERVED | `01-real-user-observations.md` [1] |

## Not yet investigated at all

TalkNow and Lesson Insights are confirmed to exist by name only — no help article was fetched describing either in any depth. Both are explicitly flagged as requiring a dedicated future research pass before any TutorFlow roadmap placement beyond the lowest priority tier (see `15-dependency-graph-and-roadmap.md`).
