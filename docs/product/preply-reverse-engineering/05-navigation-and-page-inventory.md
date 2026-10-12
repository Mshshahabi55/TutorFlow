# Navigation and Page Inventory

Cross-references the pre-existing `docs/product/NAVIGATION_AND_PAGE_MAP.md` (brief-derived module-mapping document — still architecturally useful, not Preply-verified). This document records what navigation/pages were actually confirmed to exist on the real Preply product, with verification labels; it does not restate that older file's TutorFlow-module-ownership mapping (see `14-architecture-mapping.md` for that).

## Anonymous navigation

| Element | Verification | Detail |
|---|---|---|
| Header nav | OBSERVED | "Find tutors" / "For business" / "Become a tutor" / "Proven Progress" |
| Language selector | OBSERVED | 20+ languages |
| Currency selector | OBSERVED | EUR/USD/GBP/BRL/PLN/UAH |
| Log In | OBSERVED | Present, not forced |
| Footer | OBSERVED | About Us / For Students / For Tutors / For Companies / support contact / social / app badges |
| Full persistent nav beyond the above | NOT VERIFIED | Only the homepage's own header/footer were observed; no exhaustive click-through of every nav item was performed |

## Student navigation (authenticated)

| Element | Verification |
|---|---|
| Account menu → Profile data settings | DOCUMENTED |
| Account menu → Notification settings | DOCUMENTED |
| My Lessons (Upcoming) / "Schedule" (app) | DOCUMENTED |
| Messages | DOCUMENTED |
| Subscription/Balance management | DOCUMENTED |
| Help ("?" icon, desktop) | DOCUMENTED, medium confidence |
| Full persistent authenticated nav structure | NOT VERIFIED |

## Tutor navigation (authenticated)

| Element | Verification |
|---|---|
| Everything a Student has, plus: | — |
| Insights (dashboard home) | DOCUMENTED — 6 sections, see `03-tutor-journey.md` |
| Withdraw Money (wallet icon) | DOCUMENTED |
| Identity verification (profile banner / withdraw-flow triggered) | DOCUMENTED |
| Profile Visibility toggle | DOCUMENTED |
| Availability/Calendar | DOCUMENTED |
| A dedicated "Students" list page | NOT VERIFIED — may not exist as its own page |
| Full persistent authenticated nav structure | NOT VERIFIED |

## Page Inventory

| Page | Persona | URL (if known) | Entry point | Verification | TutorFlow equivalent |
|---|---|---|---|---|---|
| Homepage | Anonymous | preply.com/en/ | Direct | OBSERVED | TutorFlow homepage — exists |
| Subject search results | Anonymous | preply.com/en/online/english-tutors (pattern) | "Find tutors" / subject link | OBSERVED | TutorFlow `/find-tutors` — exists |
| Public tutor profile | Anonymous/Student | preply.com/en/tutor/{id} | Search card click | OBSERVED | TutorFlow `/tutors/[slug]` — exists |
| Become-a-Tutor landing | Anonymous | preply.com/en/teach | "Become a tutor" nav | OBSERVED | TutorFlow `/become-a-tutor` — exists |
| Help Center home | Anonymous | help.preply.com/en/ | "?" / direct | OBSERVED | No TutorFlow equivalent exists |
| Registration (student) | Anonymous | — | Tutor-profile CTA | DOCUMENTED | TutorFlow `/signup` — exists |
| Tutor application wizard | Tutor | — | Become-a-Tutor CTA | DOCUMENTED (7 sections) / BLOCKED (real UI) | TutorFlow onboarding wizard — exists, see `07-wizard-inventory.md` for the step-count conflict |
| Settings → Profile data | Student | — | Account menu | DOCUMENTED | Not yet built (see `13-tutorflow-current-state-and-parity.md`) |
| Settings → Notifications | Student | preply.com/en/settings/notification (pattern) | Account menu | DOCUMENTED | Notifications module is a stub |
| My Lessons / Schedule | Student | — | Main nav | DOCUMENTED | No TutorFlow "My Lessons" page exists |
| Messages | Student/Tutor | — | Main nav | DOCUMENTED | Messaging module is a stub |
| Tutor Insights | Tutor | — | Main nav | DOCUMENTED | Tutor Dashboard doesn't exist |
| Withdraw Money | Tutor | — | Wallet icon | DOCUMENTED | Payments has no earnings/payout concept |
| Identity verification | Tutor | — | Profile banner / withdraw flow | DOCUMENTED | Doesn't exist |
| Terms of Service | Anonymous | — (URL not found this pass) | Footer (presumed) | NOT VERIFIED | — |

## What remains unverified at the navigation level

The exact, complete persistent navigation structure for authenticated Student and Tutor roles (every menu item, every dropdown) was never fully observed — only the specific pages/features reachable from official documentation were confirmed to exist at all. Treat the tables above as "confirmed to exist," not "confirmed to be the complete set."
