# Preply UI/UX Audit

**Purpose:** Benchmark Preply's UX quality, information architecture, and interaction patterns so TutorFlow can reach comparable usability — with its own branding, implementation, copy, and business rules. This is a research/specification document only; nothing here is implemented.

**Method note (read first):** This audit was produced using public web fetches (`WebFetch`/`WebSearch`) against Preply's public marketing pages, help-center articles, and third-party reviews. **No browser-automation/screenshot tool was available in this session** — see §24 "Evidence log" and the per-section notes below. Logged-in-only flows (checkout, student/tutor dashboards, in-wizard screens) could not be directly observed; they are reconstructed from help-center articles and reviewer write-ups and are marked accordingly. Any visual measurement (exact px, exact hex) that cannot be verified this way is marked **"Not reliably observable"** or **"approximate."** Nothing here reproduces Preply copy, code, or assets verbatim — structural/behavioral description only.

---

## 1. Executive summary

Preply's UX is built around three compounding ideas: (1) a **low-friction entry funnel** — browse tutors without an account, pay only when booking a trial; (2) a **trial-first conversion model** — one paid trial lesson per tutor, not a free sample, which both filters intent and monetizes the first interaction; (3) **subscription-based retention** — after a successful trial, the student commits to a recurring N-lessons-per-28-days package rather than booking lesson-by-lesson. The tutor side mirrors this with a single, linear, no-interview **application wizard** that front-loads trust signals (photo, video, credentials) before a human review gate.

For TutorFlow, the highest-leverage patterns to adopt are: the trial-lesson-as-paid-commitment model (already reflected in TutorFlow's `Bookings`/`Payments` domain — see `ADR-0003`, `ADR-0015`), a tutor-search experience with strong filter affordances and transparent tutor cards, and a tutor-onboarding wizard that is strictly linear with clear completion gating per step. The biggest gaps are: TutorFlow has no `Subscriptions` UI/flow defined yet, no classroom/video product, and no messaging UI — all currently empty backend stubs (per `CLAUDE.md`).

## 2. Benchmark principles

- **Parity in experience, not pixels.** Match information architecture, step sequencing, field-level behavior, and perceived polish. Do not match exact colors, exact copy, or asset files.
- **Trust-building before commitment.** Every Preply flow that asks for money or a long-term decision (trial booking, subscription purchase, tutor application) front-loads credibility signals (reviews, video, credentials, response time) immediately before the decision point.
- **One primary action per screen.** Preply's step-based flows (trial booking, tutor wizard) rarely present more than one meaningful decision per screen; secondary actions (back, skip-if-allowed) are visually subordinate.
- **Progressive disclosure over exhaustive forms.** Long forms (tutor application) are chunked into single-topic steps rather than one long scroll.
- **Human review as a trust gate, not a blocker UX.** The tutor-application review wait (~5 business days per Preply's help center) is communicated as a status, not hidden.

## 3. Visual system observations

**Not reliably observable at pixel precision** without a rendering/screenshot tool. Structurally, from fetched markup/description:

- Clean, light background, single accent color used sparingly for primary CTAs and selected states (commonly associated with Preply's brand green in public material — exact hex **not reliably observable** here).
- Card-based layout is the dominant content unit: tutor cards, feature cards, testimonial cards, FAQ accordion rows.
- Statistic callouts ("100,000+ tutors", "300,000+ reviews") used repeatedly as trust reinforcement near CTAs — a pattern, not a visual detail.

## 4. Layout system

- **Homepage:** full-width header → hero (headline + CTA + stat row) → language-tutor directory grid → "How it works" 3-step section → benefit section → tutor-carousel/testimonial section → become-a-tutor teaser → footer with 6+ link columns.
- **Search/results:** Not reliably observable directly (search results page fetch returned 404 in this session for the exact path tried); from general marketplace convention and third-party review descriptions: left or top filter bar, main content as a vertical list/grid of tutor cards, likely a sticky "Filters"/"Sort" bar on scroll. **Needs direct observation** before being treated as ground truth for implementation.
- **Tutor profile:** two-column pattern reported by reviewers — left column: photo, summary stats (lessons taught in last 48h, response time, reviews count), intro video; right column/body: About, teaching experience, subjects with per-subject descriptions, certificates/education, reviews list, and a persistent booking panel (price + "Book trial lesson" CTA), commonly sticky on scroll on desktop.
- **How It Works (public):** hero → 3-step process → 4-card benefit grid → 3-card tool showcase → testimonials → featured tutor cards → 9-item FAQ accordion → footer.
- **Footer:** consistent across public pages — multi-column link groups (About, For Students, For Tutors, For Companies, Learn, location/subject SEO links), contact address, social links, legal links.

## 5. Typography

**Not reliably observable** (no rendered/CSS-level access this session). General pattern inferable from structure: a distinct larger/bold weight for hero headlines and step numbers, a secondary weight for card titles, body copy at a smaller regular weight. Exact font family, sizes, and weights are **approximate/unknown** — flag as a design-system decision for TutorFlow's own typography scale, not a copy target.

## 6. Colors

**Not reliably observable** at hex precision. Observed pattern only: one accent color for primary CTAs/links/selected states, neutral grays for body text and borders, white/light backgrounds throughout public marketing pages (no dark mode observed or implied). TutorFlow should define its own palette (see `dataviz`/`artifact-design` conventions already in this environment for palette discipline) rather than attempt to match Preply's exact brand color.

## 7. Spacing

**Not reliably observable** at token precision (no 4px/8px grid confirmation possible from fetched content). Structural spacing pattern: generous section padding on marketing pages, card grids with consistent gutter, accordion rows with visible dividers. Treat as **approximate**; TutorFlow should adopt its own spacing scale (e.g., a standard 4px/8px Tailwind-style scale) rather than reverse-engineer Preply's.

## 8. Components

See `PREPLY_COMPONENT_INVENTORY.md` for the full catalogued list with states/variants. Summary of major components identified from structure/behavior evidence: nav bar w/ language+currency selectors, primary/secondary buttons, tutor card, filter controls (search/select/checkbox-group, price range — **exact control type per filter not reliably observable**), accordion (FAQ), carousel (tutor cards, testimonials), stat callout, step indicator (3-step "how it works," not confirmed as a true wizard progress bar on that page), video embed, rating/stars display, sticky booking panel, multi-column footer.

## 9. Form/data-entry patterns

See `PREPLY_FORM_DATA_ENTRY_SPEC.md` for the field-by-field inventory. At a pattern level: registration collects email+password (or OAuth via Google/Facebook) before anything else; the tutor application is one long multi-section form broken into logical groups (photo, certifications, education, description, video, calendar/availability, rate) rather than a single scrolling form — sources describe it as sectioned, but **whether each section is a distinct wizard "step" with its own URL/progress state, versus an accordion-style single-page form, is not reliably observable this session** and is flagged as an open question (§27).

## 10. Calendar patterns

- **Tutor availability setup:** tutors set weekly recurring availability plus timezone (help-center "Setting up your availability" article). Exact UI (grid vs. list vs. drag-select) **not reliably observable**.
- **Student slot selection:** date/time picker presented after duration selection, during trial booking; shows only slots the tutor has marked available, in the student's local timezone. Exact calendar widget visual (month grid vs. day-list) **not reliably observable**.
- TutorFlow's `Availability` module (weekly recurring rules, per `DOMAIN_MODEL.md`) is structurally compatible with this pattern already.

## 11. Booking patterns

Confirmed step sequence (help-center-sourced, high confidence):
1. Tutor profile → "Book trial lesson" (desktop) / "Buy trial lesson" (mobile app, different label).
2. Choose duration — 25 or 50 minutes. Not changeable after confirmation.
3. Choose date/time from the tutor's available slots.
4. Choose payment method, or draw from an existing lesson-credit balance with that tutor if one exists.
5. Confirm → redirected to a checkout screen → "Schedule lesson" finalizes.
6. Lesson appears in "My Lessons (Upcoming)" and on the user's calendar.

Constraints: one trial per tutor; reschedule/cancel allowed ≥12 hours before start (credit returns to balance with that tutor; <12h forfeits it, per help-center). This maps closely to TutorFlow's existing `ADR-0013`/`ADR-0017` 12-hour reschedule-notice and `ADR-0015` §6 24-hour cancellation-refund windows — note the **asymmetry**: Preply's reschedule/cancel-for-credit window is 12h, while TutorFlow's refund-eligible cancellation window is 24h; this is a deliberate TutorFlow business-rule divergence already locked, not a gap to fix.

## 12. Search/filter patterns

Not directly observable this session (results-page fetch 404'd). Structurally expected, per the homepage's own directory section and general marketplace convention: filter by language/subject, price range, tutor nationality/native-speaker toggle, availability, specialties, rating. **Needs direct observation** before TutorFlow's `Search` module's filter contract is finalized — flagged in §27.

## 13. Tutor profile patterns

See §4 above. Key trust signals surfaced near the CTA: review count/rating, lessons given in the last 48 hours (recency/popularity signal), response time, intro video. Subject-specific sub-descriptions (one per subject taught) rather than one generic bio. Reviews list below the fold.

## 14. Student dashboard patterns

**Not reliably observable** (requires authenticated session). Known from help-center cross-references: a "My Lessons" view with "Upcoming" and presumably "Past"/"Completed" tabs, a messaging inbox, a subscription/balance view. Exact layout, navigation chrome, and widget set are **Needs product decision** for TutorFlow rather than copied — flagged in §27.

## 15. Tutor dashboard patterns

**Not reliably observable**. Known from help-center cross-references: calendar/schedule view, students list, earnings view, profile editor (same sections as onboarding, editable post-approval). Exact layout **Needs product decision**.

## 16. Authentication patterns

Registration offers email+password or OAuth (Google/Facebook) for both students and tutors; tutor registration explicitly noted as desktop-only in Preply's own help center (a real platform constraint, not universal best practice — TutorFlow should not assume this limitation without its own product reason). No passkey/WebAuthn mention found in sourced material. Login is a simple top-nav "Log In" entry point on every public page.

## 17. Checkout patterns

Checkout is reached only via the trial-booking flow (§11), not as a standalone cart. Payment-method selection happens before the final confirmation step; an existing lesson-credit "balance" with a given tutor can substitute for a new charge. No evidence of guest checkout — booking requires an account. This matches TutorFlow's locked `ADR-0019` model: the student is always authenticated (JWT) before checkout; TutorFlow's single checkout call (`POST /api/bookings/{id}/checkout`) is structurally equivalent to Preply's "Schedule lesson" confirmation step.

## 18. Classroom patterns

**Not reliably observable** — Preply Classroom (the in-browser video+whiteboard product) requires an active lesson session to access. Publicly described only at a marketing level (virtual classroom, shared tools). TutorFlow has no `Lessons`-module classroom UI defined yet; this is a `P2`/`P3` gap, not a `P0` blocker (see roadmap).

## 19. Messaging patterns

**Not reliably observable** in UI detail. Known structurally: a message-tutor action exists from the tutor profile and triggers an auth gate if the visitor isn't logged in. TutorFlow's `Messaging` module is currently an empty stub; no further detail invented here.

## 20. Responsive behavior

Preply ships a dedicated mobile app in addition to responsive web (help-center documents a *separate* mobile booking flow with a different step order — payment method is asked before duration on mobile, after duration on web, per §11). This is a genuine, confirmed responsive/platform divergence, not a guess. Beyond that specific case, exact breakpoint behavior (nav collapse, filter drawer vs. sidebar, card grid column counts) is **not reliably observable** this session — flagged in §27 as needing direct device testing once TutorFlow has a frontend to test against.

## 21. Loading/error/empty states

**Not reliably observable.** No sourced material described a loading skeleton, empty-search-result state, or payment-failure error screen in enough detail to document faithfully. Mark all as **Needs product decision** — TutorFlow should design these states to its own `error-handling` skill conventions (ProblemDetails, Result pattern) rather than guess at Preply's specifics.

## 22. Accessibility observations

**Not reliably observable** via WebFetch (no DOM/ARIA inspection, no contrast measurement, no keyboard-nav trace possible in this session). No accessibility claim is made either way about Preply. TutorFlow should set its own accessibility bar (WCAG 2.1 AA as a reasonable default) independent of what Preply does or doesn't do — this is a recommendation, not an observation.

## 23. Navigation / information architecture

Public IA (confirmed): Home → {Find tutors, For business, Become a tutor, Proven Progress} + language/currency selector + Help + Log in. Footer IA: About Us / For Students / For Tutors / For Companies / Learn / location-and-subject SEO link farms. This SEO-link-farm footer pattern is a marketing/acquisition tactic, not a UX pattern TutorFlow needs to replicate for an MVP.

## 24. Complete page inventory

| # | Page | Access | Observed this session | Source |
|---|---|---|---|---|
| 1 | Home | Public | Yes — fetched | WebFetch |
| 2 | Find Tutors / search results | Public | **No — fetch 404'd** | attempted WebFetch |
| 3 | Tutor profile | Public | No — summarized from reviewer sources only | WebSearch |
| 4 | Subjects (SEO landing pages) | Public | No | inferred from footer links only |
| 5 | How It Works | Public | Yes — fetched | WebFetch |
| 6 | Become a Tutor (landing) | Public | No — only the registration help article was fetched | WebFetch (help center) |
| 7 | Sign up / Login (student) | Public | No — described only at a procedural level | WebSearch |
| 8 | Tutor application wizard (all internal steps) | Auth'd | No — described only via help-center article | WebFetch (help center) |
| 9 | Trial booking flow (duration/date/time/checkout) | Auth'd (mostly) | No — described only via help-center article | WebFetch (help center) |
| 10 | Booking confirmation | Auth'd | No | inferred |
| 11 | My Lessons / student dashboard | Auth'd | No | inferred |
| 12 | Tutor dashboard (calendar/students/earnings) | Auth'd | No | inferred |
| 13 | Messaging | Auth'd | No | inferred |
| 14 | Classroom | Auth'd | No | marketing description only |
| 15 | Subscription management | Auth'd | No | help-center article summary only |
| 16 | Settings (student/tutor) | Auth'd | No | inferred |

**Total pages/flows in scope: 16. Directly fetched and documented from live structure: 2 (Home, How It Works). Documented from help-center/secondary sources: 6. Not reliably observable at all: 8.**

## 25. Complete user-flow inventory

| # | Flow | Steps documented | Confidence |
|---|---|---|---|
| 1 | Student discovers → browses tutors | Home → Find Tutors (filters unknown) → Tutor profile | Medium (endpoints exist; filter detail missing) |
| 2 | Student books a trial | Profile → duration → date/time → payment/balance → checkout → confirm | High (help-center confirmed, both web and app variants) |
| 3 | Student subscribes after trial | Trial complete → choose package (6/12/20 lessons per 28 days) → recurring charge | Medium (pricing/cadence confirmed; exact UI not observed) |
| 4 | Student messages a tutor pre-booking | Profile → Message → auth gate if logged out → conversation | Low (existence confirmed, UI not observed) |
| 5 | Tutor registers and applies | Become a Tutor → email/password or OAuth → 8-section application → submit → ~5 business day review → approve/reject email | High (help-center confirmed) |
| 6 | Tutor manages ongoing business | Dashboard → calendar/students/earnings/messages | Low (existence inferred, UI not observed) |
| 7 | Student cancels/reschedules a trial | My Lessons → cancel/reschedule → 12h-notice credit-return rule | High (help-center confirmed) |

## 26. TutorFlow mapping

| Preply pattern | TutorFlow equivalent | TutorFlow route (proposed) | Frontend component | Backend dependency | Module |
|---|---|---|---|---|---|
| Home / tutor discovery entry | Marketing home + CTA | `/` | `HomePage` | none (static) | — |
| Find tutors + filters | Tutor search | `/tutors` | `TutorSearchPage`, `FilterBar`, `TutorCard` | search/filter query endpoint | `Search`, `Tutors`, `Subjects` |
| Tutor profile | Tutor profile | `/tutors/{id}` | `TutorProfilePage`, `ReviewList`, `BookingPanel` | profile read endpoint, `ITutorPricingQuery`, `ITutorAvailabilityQuery` | `Tutors`, `Availability`, `Reviews` |
| Message tutor | Start conversation | `/messages/{threadId}` | `MessageComposer`, `ConversationView` | messaging endpoints (not yet built) | `Messaging` |
| Duration → date/time → checkout | Trial booking wizard | `/book/{tutorId}` | `BookingWizard` (`DurationStep`, `SlotPickerStep`, `CheckoutStep`) | `POST /api/bookings/trial-holds`, `POST /api/bookings/{id}/checkout` | `Bookings`, `Payments`, `Availability` |
| Booking confirmation | Confirmation screen | `/bookings/{id}/confirmed` | `BookingConfirmation` | booking read endpoint | `Bookings` |
| My Lessons | Student lesson list | `/my-lessons` | `LessonListPage` | bookings-list endpoint | `Bookings`, `Lessons` |
| Subscription purchase | Package selection | `/subscribe/{tutorId}` | `SubscriptionPicker` | subscription endpoints (not yet built) | `Subscriptions`, `Payments` |
| Tutor application wizard | Tutor onboarding wizard | `/become-a-tutor/apply` | see `TUTORFLOW_WIZARD_ARCHITECTURE.md` | profile/credential/availability/pricing write endpoints (not yet built) | `Identity`, `Tutors`, `Subjects`, `Availability` |
| Tutor dashboard | Tutor dashboard | `/dashboard` | `TutorDashboardPage` | various read endpoints | `Tutors`, `Bookings`, `Lessons`, `Payments` |
| Classroom | Live lesson room | `/lessons/{id}/room` | `ClassroomPage` | not yet designed | `Lessons` |
| Reviews | Review display/submission | embedded in profile + `/my-lessons/{id}/review` | `ReviewForm`, `ReviewList` | review endpoints (not yet built) | `Reviews` |

No TutorFlow domain rule is changed to imitate a UI pattern — e.g., the 24h refund-eligible cancellation window and 12h reschedule window stay exactly as already locked in `ADR-0015`/`ADR-0017`, even though Preply's own cancellation-for-credit window is 12h.

## 27. Open questions / uncertain observations

All of the following are **"Not reliably observable"** or **"Needs product decision"** and must not be treated as confirmed Preply behavior:

1. Exact search/filter control types and the full filter list on the tutor-search results page (fetch 404'd this session).
2. Whether the tutor-application "8 sections" are true wizard steps (separate URLs/progress bar) or a single long form with anchored sections.
3. Exact visual tokens — colors (hex), typography (family/sizes/weights), spacing scale, border radii, shadows — none measurable via WebFetch.
4. Student/tutor dashboard layouts, navigation chrome, and widget sets (fully auth-gated, not reachable).
5. Classroom (live lesson room) UI — only marketing-level description available.
6. Messaging UI — existence confirmed, no layout/state detail available.
7. Loading, empty, and error-state visual treatment anywhere in the product.
8. Accessibility posture (contrast, keyboard nav, ARIA) — no tooling available to assess.
9. Exact mobile breakpoint behavior for search/filters/booking beyond the one confirmed app-specific step-order difference (§20).
10. Settings pages (student/tutor) — not sourced at all this session.

**Recommendation:** before the `P0`/`P1` frontend work in `FRONTEND_IMPLEMENTATION_ROADMAP.md` locks exact component behavior for search/filter and dashboards, a follow-up pass with real browser/screenshot access (or a team member's logged-in walkthrough) should resolve items 1, 2, 4, and 9 above — these materially affect component contracts, not just visual polish.

## 28. Wizard matrix

Full detail in `PREPLY_WIZARD_COMPONENT_INVENTORY.md` and `TUTORFLOW_WIZARD_ARCHITECTURE.md`. Only values supportable by observation are filled; everything else is marked.

| Wizard | Steps | Forms | Key components | Complexity | Priority |
|---|---:|---:|---|---|---|
| Tutor Onboarding | 8 (sectioned; true step-vs-anchor structure **not reliably observable**) | 8 (photo, certifications, education, description, video, calendar/availability, rate, + 1 unconfirmed section) | `WizardShell`, `FormField`, `FileUploader`/`ImageUploader`, `VideoUploader`, `AvailabilityGrid`, `ReviewStep` | High | P0 |
| Trial Booking | 3–4 (duration → date/time → payment/balance → confirm; order differs web vs. app, confirmed) | 1–2 (slot selection, payment) | `WizardShell` (lightweight), `DatePicker`/`TimeSlotPicker`, payment-method selector | Medium | P0 |
| Student Onboarding | **Not reliably observable** — no evidence of a distinct multi-step student wizard beyond simple signup | **Not reliably observable** | n/a | **Needs product decision** | P1 |
| Profile Editing (tutor) | **Not reliably observable** as a wizard — help-center implies the same 8 sections are independently editable post-approval, not a forced linear re-walk | shares fields with onboarding | reuses onboarding `FormField`/`FormSection` components | Medium | P1 |

## 29. Final report (per request, see also the summary relayed in chat)

- **Pages/flows audited:** 16 identified in scope (§24); 2 directly fetched live, 6 documented from secondary sources, 8 not reliably observable.
- **Forms audited:** see `PREPLY_FORM_DATA_ENTRY_SPEC.md` — 2 forms with field-level detail (registration, tutor-application sections as a group), several more flagged as existing but not detailed (search filters, messaging composer, review submission, settings).
- **Reusable components identified:** 20+ catalogued in `PREPLY_COMPONENT_INVENTORY.md`.
- **Screenshots/evidence items captured:** **0 screenshots** — no browser/screenshot tool was available this session (confirmed with the user in advance). All evidence is fetched HTML/text content and search-result summaries; each claim above is tagged with its source or marked not reliably observable. **This was accurate for this session.** A later, separate session received a 13-screenshot evidence pack of a real tutor-signup wizard session — see `docs/product/preply-reverse-engineering/18-screenshot-evidence.md`. This historical statement is left as-written to preserve an accurate record of what this specific session had available.
- **Major UX patterns discovered:** paid-trial-as-conversion-gate; subscription-after-trial retention model; sectioned/wizard tutor application with a human-review trust gate; trust-signal stacking (reviews, recency, video) directly adjacent to commitment CTAs; platform-specific step reordering (mobile app asks payment before duration; web asks after).
- **Major TutorFlow gaps:** no `Search` filter UI/contract defined; no `Messaging`, `Reviews`, `Subscriptions`, or `Learning` UI of any kind; no classroom/video product; no tutor-onboarding wizard; dashboards for either role undefined.
- **Highest-priority implementation areas:** tutor search + profile + trial booking + checkout (`P0`/`P1` — these are TutorFlow's core conversion path and already have backing domain modules per `ADR-0013`/`ADR-0015`/`ADR-0019`), tutor onboarding wizard (`P0` — nothing can be listed for booking without it).
- **Needs Product Owner / Architect decision:** search/filter exact contract (§27.1), whether tutor onboarding is step-based or section-based UI (§27.2), student/tutor dashboard scope for V1, classroom/video build-vs-buy, messaging UI scope, accessibility target level, and the full visual design system (colors/type/spacing) — none of these should be derived from Preply by assumption.
