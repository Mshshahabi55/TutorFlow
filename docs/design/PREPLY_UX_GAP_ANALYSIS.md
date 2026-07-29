# Preply UX Gap Analysis

**Purpose:** UX-inspiration research only. This document compares TutorFlow's current, real UX (verified by reading the actual frontend source) against Preply's real UX (verified by studying screenshots the owner captured and placed in `references/preply/`). It is an **input to planning future redesign phases** — it contains no code, no visual mockups, and recommends no immediate implementation. Every "Recommended redesign" below is a direction to evaluate in a future phase, not an instruction being executed now.

**What was explicitly NOT copied:** Preply's brand name, logo, illustrations, exact colors/hex values, icon set, marketing copy, or any asset/CSS/source code. Every comparison below is about *structure* — information architecture, flow order, hierarchy, density — never Preply's literal visual expression of it. Where a "Recommended redesign" suggests a pattern (e.g., "group time slots by Morning/Afternoon/Evening"), it means the *pattern*, expressed in TutorFlow's own already-established design system (`frontend/src/app/theme.ts`, Deep Emerald palette, 24px card radius, etc. — see the Preply Redesign Phase 1 work already completed), never Preply's pink/black/white expression of it.

## Screenshots studied

All 10 images in `references/preply/`, viewed in full:

| File | What it shows |
|---|---|
| `Home.png` | Tutor search results page (`preply.com/en/online/english-tutors`) — filter bar + tutor result cards + video-preview side panel |
| `Search.png` | Cropped detail of the same search filter bar |
| `tutor-profile-top.png` | Tutor profile: video hero + sticky right-hand booking/price card |
| `tutor-profile-middle.png` | Tutor profile: highlight badges (Adaptable/Goal-Focused/Encouraging), "Professional Tutor"/"Super Tutor" trust badges, About me |
| `tutor-profile-bottom.png` | Tutor profile: languages, 4-metric lesson-rating breakdown, individual student reviews |
| `booking-step1.png` | "Book a trial lesson" modal — duration toggle, week-strip calendar, time-of-day grouped slots |
| `booking-confirm.png` | Checkout/payment page — tutor summary, trial lesson details + cancellation policy, payment method grid, card form |
| `messages.png` | Messages/Inbox — All/Unread/Archived tabs, conversation list, empty "select a conversation" state |
| `my-lessons.png` | My Lessons — empty state |
| `Setting.png` | Account Settings — left vertical tab nav + Account Settings form |

**Coverage gap, stated up front:** no screenshot exists for Preply's Tutor-side dashboard, Parent/family view, any admin/ops screen, mobile viewports, an in-progress lesson/video-call screen, a populated message thread, an open notification panel, or any error/loading state. Sections below covering those areas are marked **"No direct screenshot evidence"** and are based on what the 10 images *do* establish (nav shell, component conventions, copy tone) rather than invented specifics. Where TutorFlow has no equivalent screen at all (Admin, Parent), that is also called out — Preply's own consumer product has no Parent/Guardian role and no visible Admin surface, so those two sections compare against general marketplace-ops conventions, not a Preply screenshot.

**Governance facts that shape every recommendation below** (checked directly against the repo's own governing documents before writing this):

- **Ratings/reviews are out of scope for v1 by an Accepted, Must-priority decision** — `PRODUCT_REQUIREMENTS.md` DISC-3 / Decision C.12: *"Given a Tutor profile is displayed, when a Student/Parent views it, then no rating or review feature is presented."* Any redesign that adds review UI would contradict an Accepted product decision, not just be a UI change — it needs the owner to reopen C.12, which is a CLAUDE.md stop condition, not a phase to schedule.
- **Payments have no accepted design at all.** `docs/adr/ADR-020-payments-architecture.md` is Proposed, and its own header states "No section of this ADR was ever Accepted" — superseded by `docs/adr/ADR-021-learning-plans-enrollment-and-settlement-architecture.md`, which is *also* Proposed only, pending several "Questions Requiring Approval." TutorFlow has **zero payment/checkout code today** — not a worse version of Preply's checkout, an absent one.
- **Learning Plans are a UI-only placeholder everywhere they appear** (`LearningPlanCard.tsx`, `TutorDetailPage.tsx`, both dashboards) — no backend capability exists (same ADR-021 gate).

These three facts recur throughout the document instead of being repeated in every affected section — look for **"[Gated by DISC-3]"** or **"[Gated by ADR-021]"** markers.

---

## 1. Information architecture

**Current TutorFlow UX:** A persistent left sidebar (`NavSidebar.tsx`), permanent on desktop (≥`md`), collapsible to a 72px icon rail, overlay drawer on mobile/tablet. One flat list per role — no grouping, no sections (`navSections.tsx`): Student gets Home/Find Tutors/My Lessons/Messages/Profile; Tutor gets Dashboard/My Students/My Lessons/Availability/Messages/Profile; Parent gets Home/My Children/Book a Lesson/Messages/Profile; Admin gets Dashboard/Operations/Tutor Approvals/All Sessions/Support Messages/Reset Password. A top `AppHeader` sits above it with breadcrumbs, search placeholder, notifications, theme toggle, and the user menu.

**Preply UX:** No left sidebar anywhere in any of the 10 screenshots. Navigation is two-tier and entirely horizontal: (1) a top marketing bar — logo, "Find tutors," "Corporate training," "Refer a friend," language/currency selector, message/help/wishlist/notification icons, avatar; (2) once inside the account area, a second slim tab row directly below it — Home | Messages | My lessons | Settings, with "For business" pinned right. No icons on this second row, just text labels with an underline for the active tab.

**Problems in TutorFlow:** None inherent — a sidebar is a legitimate, common pattern (it's also what Linear/Notion use, the reference class the Phase 1 redesign already targeted). The real gap is that TutorFlow's IA has no equivalent of Preply's *first tier* — a public, pre-login marketing/discovery surface is entirely merged into the authenticated app shell today.

**Why Preply is easier:** Separating "browsing tutors" (public, no commitment) from "managing my account" (private, tabbed) lets a visitor explore the entire discovery experience before ever creating an account, and keeps the authenticated area to exactly 4 destinations — very little to learn.

**Recommended redesign:** Not a sidebar-to-topbar rewrite (that would be a large, high-risk change with no clear user-facing win once Phase 1's sidebar redesign already reads as a "clean, spacious, marketplace" surface). Instead: evaluate whether Student/Parent's nav (already the shortest lists) could be flattened further, and confirm the public/pre-auth discovery pages are reachable without authentication at all (`TutorSearchPage`/`TutorDirectoryPage` — verify this in a future phase; not confirmed either way in this pass).

**Priority:** Low. **Effort:** N/A (research spike only, ~1 day, before any code). **Backend dependency:** No.

---

## 2. User journey

**Current TutorFlow UX:** Discovery → Tutor profile → 5-step booking wizard (Choose Tutor → Choose Date → Choose Time → Review → Confirm) → session created directly, no payment → session appears in "My Lessons." Everything happens on full page routes; no modal-based booking anywhere.

**Preply UX (from the screenshot sequence):** Discovery → Tutor profile (with a persistent, always-visible booking card) → **"Book trial lesson" opens a modal**, not a page navigation → modal collects duration + slot in place → **"Continue" leaves the modal for a dedicated checkout page** → payment → (implied) lesson appears in My Lessons.

**Problems in TutorFlow:** The 5-step full-page wizard means every booking, however simple, is a 5-navigation journey with a visible stepper — heavier than the actual decision being made (pick a tutor, a time, confirm). Users lose their scroll position/context on the tutor profile each time they advance a step.

**Why Preply is easier:** The modal keeps the tutor's profile in view (dimmed) behind the booking modal, so the user never feels like they "left" the profile — reinforces the sense that this is a lightweight, low-commitment action (a trial lesson), not a multi-page checkout.

**Recommended redesign:** Collapse TutorFlow's "Choose Date" + "Choose Time" steps into a single modal/dialog step reachable directly from the tutor profile's booking card (already sticky per §14), leaving "Review"/"Confirm" as the only full-page step (since it already branches by booking mode — self/parent/manual — and that's real complexity worth a dedicated screen). This shortens the common path from 5 steps to effectively 2.

**Priority:** Medium. **Effort:** L (touches `BookSessionPage.tsx`'s stepper logic, `AvailabilityCard` reuse, routing for the deep-link-to-review case that already exists). **Backend dependency:** No — this is entirely a frontend flow restructuring; `POST /sessions` stays the same.

---

## 3. Visual hierarchy

**Current TutorFlow UX:** Already substantially addressed by the Preply Redesign Phase 1 (Visual Foundation) work: Page title 44px, Section title 28–32px, Card title 20–22px, generous line-height on body copy, Deep Emerald primary color reserved for primary actions. See `frontend/src/app/theme.ts` and `docs/design/DESIGN-SYSTEM.md` §8.

**Preply UX:** Very large (est. 36–48px), heavy-weight black display headlines against pure white ("English tutors that help you develop professionally," "Speak English for your career in 1–3 months"), with exactly one saturated accent color (pink/magenta) reserved for the single highest-priority action per screen, and everything else — including secondary buttons — in black/white/grey.

**Problems in TutorFlow:** Since Phase 1 already introduced the type scale and a single primary brand color (Deep Emerald), the structural gap here is smaller than it looks from the screenshots alone. The one remaining difference: Preply's hierarchy is reinforced by *strict* color discipline — the accent color appears in almost nothing except the one primary CTA per screen — whereas TutorFlow's theme still uses `primary` for links, selected-nav state, focus rings, and buttons alike, which is conventional but less visually "loud" for the one action that matters most.

**Why Preply is easier:** A user's eye has exactly one place to land on any given screen.

**Recommended redesign:** Audit real pages (not just the theme) for whether more than one `contained`/primary-colored element competes for attention per screen (e.g., a page with both a primary "Save" button and a primary-colored info chip) and demote non-primary-action elements to `outlined`/`text` variants. This is a page-by-page polish pass, not a token change.

**Priority:** Low. **Effort:** M (audit + spot fixes across many pages). **Backend dependency:** No.

---

## 4. CTA placement

**Current TutorFlow UX:** Each `TutorCard` stacks two full-width buttons vertically: contained "Book Lesson," then outlined "View profile," then a text link "View Learning Plans." On `TutorDetailPage`, the same "Book Lesson" CTA appears in the hero *and* again at the bottom of the page in its own "Ready to get started?" card — not sticky.

**Preply UX:** Exactly one primary CTA per card ("Book trial lesson," filled pink) plus one secondary ("Send message," outlined) — no third/tertiary link competing on the card. On the profile page, the booking card (price + CTA) is **sticky in the right rail**, so the CTA is never more than one glance away regardless of scroll position, and there's no need to repeat it at the page's bottom.

**Problems in TutorFlow:** Three stacked CTAs per search card is more decision load than Preply's two; the profile page compensates for its *non-sticky* CTA by literally duplicating the button at the bottom, rather than making it stay in view.

**Why Preply is easier:** Fewer choices per card, and a CTA that's always reachable removes the need for a duplicate "j
ust in case you scrolled" button.

**Recommended redesign:** On `TutorCard`, consider demoting "View Learning Plans" to appear only after "View profile" is clicked (or remove until Learning Plans is a real feature — see §22/§8 governance note), leaving two buttons. On `TutorDetailPage`, make the right-column booking rail sticky **on TutorFlow too** — worth checking, since the code review (§14) found it's actually already implemented (`sx={{ position: { md: "sticky" } }}`) — if so, the bottom-of-page duplicate "Ready to get started?" card may be redundant now and could be removed in a later phase.

**Priority:** Medium. **Effort:** S (both changes are narrow, single-file). **Backend dependency:** No.

---

## 5. Booking flow

**Current TutorFlow UX:** 5-step `Stepper` wizard (Choose Tutor → Choose Date → Choose Time → Review → Confirm), full pages, no modal. Date selection is a flat wrapping row of date `Chip`s (no calendar grid, no week navigation arrows). Time selection is a flat wrapping row of slot cards for the selected date only — no Morning/Afternoon/Evening grouping. No duration selector (duration is fixed per declared slot). No computed total price — only the tutor's hourly rate is shown at every step. No payment step at all; "Confirm" directly calls `POST /sessions`.

**Preply UX:** A single modal, reachable from the profile's sticky CTA. Two controls up top: a duration toggle (25 min / 50 min — changes price live). Below: a 7-day week strip with left/right arrow navigation to move to other weeks, and slots grouped under "Morning"/"Afternoon"/"Evening" headers within the selected day. "Continue" is disabled until a slot is picked, then proceeds to checkout (§21).

**Problems in TutorFlow:** No duration choice (a real Preply differentiator — trial-lesson length affects price and commitment), no way to browse future weeks from the date-picker (TutorFlow's date chips are generated only from whatever slots the tutor already declared, so there's no "browse ahead" concept at all), no time-of-day grouping (a flat row of times is harder to scan once a tutor has 10+ slots in a day).

**Why Preply is easier:** Grouping by time-of-day lets a user jump straight to "evenings only" without reading every slot; week navigation makes "later this month" reachable without leaving the modal.

**Recommended redesign:** Group `AvailabilityCard`s within Step 2 ("Choose Time") into Morning (<12:00)/Afternoon (12:00–17:00)/Evening (>17:00) sub-headers — pure frontend grouping over data already fetched, no API change. Week-navigation arrows are lower value here since TutorFlow's date chips already only show dates that *have* real availability (there's nothing to browse ahead to) — skip unless the underlying availability query changes. A duration toggle is not applicable — TutorFlow's tutors declare one fixed-duration slot at a time (an `AvailabilitySlot`'s duration is a domain fact, not a per-booking choice) — introducing a duration toggle would be a Domain-level change (a slot's duration is fixed by the Tutor when declared), not a frontend one.

**Priority:** Medium (time grouping) / Not recommended (duration toggle — conflicts with the domain model as designed). **Effort:** S (time-of-day grouping only). **Backend dependency:** No for grouping; Yes (Domain change, needs a new ADR) if duration-per-booking were ever pursued — flagging, not recommending.

---

## 6. Search flow

**Current TutorFlow UX:** `TutorSearchPage` — one large free-text "Subject" field + Search button in the main hero, plus a "Filters" button that opens a right-side `Drawer` with Language/Location/"Available from (Tehran)" fields — all free-text, no fixed taxonomy, no price/rating filter. Real page-numbered pagination (`TablePagination`, 10/20/50 per page). Active filters shown as removable chips. `TutorDirectoryPage` (a second, simpler browse view) has no filtering UI at all and no pagination — every discoverable tutor renders unpaginated.

**Preply UX:** All filters visible inline in one row — no drawer, no hidden panel: "I want to learn" (subject), "Price per lesson" (range), "Country of birth," "I'm available" (time), plus a second row: Specialties, Also speaks, Native speaker, Tutor categories, sort dropdown, and a name/keyword search — all as dropdown/multi-select controls, not free text. No visible pagination in the captured screenshot (Preply uses infinite scroll on this page in the real product, not evidenced in this single screenshot but well-established from general product knowledge — flagged as such, not verified here).

**Problems in TutorFlow:** Filters hidden behind a "Filters" drawer add a click for something Preply treats as primary, always-visible information. Free-text Subject/Language/Location fields require the user to guess exact spelling/values rather than pick from what actually exists — a real risk of zero-result searches for typos or synonyms ("Math" vs "Mathematics"). No price filter exists at all despite hourly rate being a core `TutorDto` field already returned by the search endpoint.

**Why Preply is easier:** Structured filters (dropdowns/ranges) can never return "0 results because of a typo," and seeing every filter at a glance (not behind a drawer) makes the breadth of narrowing options obvious immediately.

**Recommended redesign:** Move Language/Location out of the drawer and into the main filter row (frontend-only change — `TutorFilterPanel` reorganization). Add a price-range filter over the already-available `hourlyRate` field (frontend filtering logic + one new query param if server-side filtering is wanted, or purely client-side if result sets stay small). Converting Subject/Language/Location from free-text to a fixed, autocomplete-backed list is a larger change — it requires either a canonical subject/language taxonomy in the Domain (new) or a "distinct values already in use" endpoint; flag as Medium/Large effort with a real backend touch, not a pure frontend fix.

**Priority:** High (filter visibility, price filter) / Medium (taxonomy). **Effort:** S (move filters out of drawer) / S (client-side price filter) / L (real taxonomy, backend-dependent). **Backend dependency:** No for the first two; Yes for a real taxonomy.

---

## 7. Tutor profile layout

**Current TutorFlow UX:** Hero (generic person-silhouette avatar — no real photo field exists in `TutorDto` at all; subject-as-name convention; Book Lesson + Send Message actions) → sticky in-page section nav (About/Learning Plans/Availability/Reviews/FAQ, anchor-scroll) → two-column body: left column (Subjects & Languages, Teaching Information, Learning Plans placeholder, Availability preview limited to 3 slots, static "no reviews yet" placeholder, static FAQ placeholder) and a **sticky right column** (Manage listing — role-gated, Book Lesson CTA card).

**Preply UX:** Hero is a real, playable **video** of the tutor (not a photo, not an icon) with an "Unmute" control. Right rail: price, rating, review/lesson/student counts, primary CTA, secondary icon actions (message/save/share), and a reassurance banner ("Not a match? You still have 2 free tutor trials."). Below: highlight badges specific to that tutor's teaching style (Adaptable/Goal-Focused/Encouraging), verified-credential badges (Professional Tutor, Super Tutor) each with a "Learn more" link, free-text About Me, spoken languages with proficiency chips, then the 4-metric lesson-rating breakdown and individual reviews.

**Problems in TutorFlow:** No photo or video exists anywhere for a tutor — every profile and every card shows the same generic silhouette icon, which is a significant trust/differentiation gap for a marketplace where the product *is* the individual tutor. No bio/"about me" free text exists in the domain at all. No highlight/trust badges beyond the Admin-facing Approved/Suspended/Discoverable pills (which aren't shown to Students at all).

**Why Preply is easier:** A video (or even just a real photo) plus a genuine bio is the single biggest driver of "do I want to book this specific human" in a tutoring marketplace — TutorFlow's current profile answers "what do they teach and for how much" but never "who are they."

**Recommended redesign:** This is a Domain-level gap, not a component-styling one — `TutorDto`/the `Tutor` aggregate has no photo, video, or bio field today. Recommend as a real future phase: add an optional `PhotoUrl` (simplest — links to an externally-hosted image, no file-upload infrastructure needed yet) and a `Biography` free-text field to the Tutor aggregate, surfaced on `TutorCard`/`TutorProfileHero`/`TutorOfferingPage`'s edit form. A real video-upload pipeline is a much larger, infrastructure-heavy ask (storage, transcoding, moderation) — recommend starting with photo + bio only.

**Priority:** Critical (this is the single highest-leverage gap found in this whole analysis — a marketplace with no tutor photo/bio is missing the core trust signal). **Effort:** L (new Domain field + migration + Application command/validator + Web endpoint update + `AUTHORIZATION_MATRIX.md` row + frontend form + card/hero rendering — touches all three layers per CLAUDE.md's Definition of Done). **Backend dependency:** Yes.

---

## 8. Student dashboard

**Current TutorFlow UX (`StudentDashboard.tsx`):** "Welcome back" header → Upcoming Sessions → Continue Learning (always-empty Learning Plans placeholder [Gated by ADR-021]) → Messages widget → Recommended Tutors (literally the first page of unfiltered search results, labeled "Recommended" — not a real recommendation engine, per the code's own comment) → Recent Activity → Quick actions (Find Tutors/Book Your Lesson/My Lessons).

**Preply UX:** **No direct screenshot evidence of a populated "Home" dashboard** — `my-lessons.png` shows the My Lessons tab (empty state only), `messages.png` shows Messages (empty state only). The "Home" tab itself was never captured in an authenticated, populated state; `Home.png` is actually the public search/marketing page, not the account home. Cannot honestly compare layout here beyond the shared account-shell nav (§1).

**Problems in TutorFlow:** N/A for direct comparison — see note above. The one comparable, real gap: "Recommended Tutors" openly admits (in its own code comment) to being unfiltered search results relabeled, which risks recommending tutors in a subject the student has never expressed interest in.

**Why Preply is easier:** Not assessable from available evidence.

**Recommended redesign:** Not a Preply-comparison item — a TutorFlow-only observation: "Recommended Tutors" could reasonably filter by the subject(s) of the student's own past sessions once at least one exists, rather than showing the global unfiltered list. Small, self-contained, no new backend capability (reuses existing session history + existing search endpoint with a subject param already supported).

**Priority:** Low. **Effort:** S. **Backend dependency:** No.

---

## 9. Tutor dashboard

**Current TutorFlow UX (`TutorDashboard.tsx`):** Quick actions → Teaching Summary (4 stats: lessons today, upcoming lessons, students, open time slots) → Messages widget → My Learning Plans (always-empty stats [Gated by ADR-021]) → Next Lesson hero card → Today's teaching → Today's Availability → Upcoming Lessons → Recent Activity → **Students Requiring Attention** (a genuinely useful, non-obvious feature: surfaces students whose last session was Cancelled/No-Show with nothing rebooked) → Availability Overview → Profile completion card → static "Teaching tips coming soon" placeholder.

**Preply UX:** **No direct screenshot evidence** — no tutor-side account view was captured.

**Problems in TutorFlow:** Not assessable via direct comparison. Internally, this page is dense (11 sections) — likely the single longest-scrolling dashboard in the app.

**Why Preply is easier:** Not assessable from available evidence.

**Recommended redesign:** No Preply-driven redesign recommended without reference material. If pursued as a TutorFlow-only initiative in a later phase: consider whether "Students Requiring Attention" and "Availability Overview" (both action-oriented) should move above the purely-informational stat widgets, since they're the sections most likely to make a returning Tutor actually *do* something.

**Priority:** Low (no evidence basis for urgency). **Effort:** M if reordered. **Backend dependency:** No.

---

## 10. Parent dashboard

**Current TutorFlow UX (`ParentDashboard.tsx`):** Quick actions → Family Summary (children/lessons-today/upcoming) → Next Family Lesson hero → Today's Lessons → Children Overview (early-returns to a standalone empty state if zero relationships exist) → Upcoming Lessons → Recent Activity → Messages widget → Recommended Tutors → static "Learning tips"/"Support" placeholders.

**Preply UX:** Preply's actual consumer product has **no distinct Parent/Guardian role or family-management view** in the same sense TutorFlow's domain models it (a Parent/Guardian booking on behalf of a Student, with a `Relationship` confirmation flow). No screenshot exists because there is likely no equivalent screen to capture.

**Problems in TutorFlow:** N/A — this is a TutorFlow-specific domain concept with no Preply analog to gap-check against.

**Why Preply is easier:** Not applicable.

**Recommended redesign:** None from this analysis. This dashboard should continue to evolve from TutorFlow's own product requirements (`PRODUCT_REQUIREMENTS.md`), not from Preply parity.

**Priority:** N/A. **Effort:** N/A. **Backend dependency:** N/A.

---

## 11. Admin dashboard

**Current TutorFlow UX (`AdminDashboardPage.tsx`):** Quick actions → Pending Tutor Approvals (count + preview list, limit 5) → Recent Sessions (preview list, limit 5) → Messages widget (relabeled "Support Messages" in nav) → Marketplace Overview (2 stats: total sessions, discoverable tutors) → Platform Health (`GET /health` status pill). No charts, no graphs, no time-series visualizations anywhere — every widget is a plain number or a short list. Own doc comment: *"Administrative conflict resolution is not yet available."*

**Preply UX:** **No screenshot evidence** — an admin/ops backoffice is never part of a marketplace's public-facing product and could not have been captured from the customer-facing site regardless.

**Problems in TutorFlow:** Not assessable via Preply comparison. Internally: an Admin operations dashboard with zero charts/trend lines is a reasonable MVP choice, not a deficiency — flagging analytics as a *possible* future want, not a gap versus any evidenced reference.

**Why Preply is easier:** Not applicable — no comparable surface exists to evaluate.

**Recommended redesign:** None from this analysis.

**Priority:** N/A. **Effort:** N/A. **Backend dependency:** N/A.

---

## 12. Sidebar behavior

**Current TutorFlow UX:** See §1. Permanent sidebar (desktop) collapsible to a 72px icon rail (state persisted to `localStorage`); temporary overlay drawer (mobile/tablet, hamburger-triggered). Flat, ungrouped list per role.

**Preply UX:** **No sidebar exists in any captured screen.** All navigation is top-bar based (two tiers, per §1). This is a genuine, confirmed structural difference, not an evidence gap.

**Problems in TutorFlow:** None inherent to having a sidebar — it's a valid, common pattern for an app with this many distinct destinations per role (Tutor has 6 nav items; a two-tier top bar would get crowded at that count). Preply's flat 4-item account nav (Home/Messages/My lessons/Settings) works specifically *because* it's so short — a direct copy would force TutorFlow to hide items Preply doesn't have (Availability, My Students, Operations, Tutor Approvals, etc.) somewhere else.

**Why Preply is easier:** For a 4-destination account area, yes; not necessarily generalizable to TutorFlow's per-role item counts.

**Recommended redesign:** Keep the sidebar. No redesign recommended — already addressed in Phase 1 (Preply-style selected-state pill, larger touch targets, generous spacing).

**Priority:** Not recommended (keep current pattern). **Effort:** N/A. **Backend dependency:** No.

---

## 13. Mobile behavior

**Current TutorFlow UX:** `AppLayout.tsx` switches to a temporary overlay drawer + hamburger button below the `md` breakpoint; `PageContainer` applies responsive padding and a `safe-area-inset-bottom` allowance; Phase 1 already raised every interactive control (buttons, icon buttons, nav rows) to a 44px minimum touch target app-wide.

**Preply UX:** **No mobile screenshots were provided** — all 10 images are desktop browser captures (visible browser chrome, ~1920px-wide layouts). No mobile-specific claim can be made from this evidence.

**Problems in TutorFlow:** Not assessable via Preply comparison from the available material.

**Why Preply is easier:** Not assessable.

**Recommended redesign:** None from this analysis. If mobile parity is wanted, the next step is gathering actual Preply mobile-web or app screenshots before writing a mobile-specific comparison — asserting anything else here would be guessing, not analysis.

**Priority:** N/A (insufficient evidence). **Effort:** N/A. **Backend dependency:** N/A.

---

## 14. Sticky elements

**Current TutorFlow UX:** `TutorDetailPage`'s right-column booking rail already uses `position: sticky` (`top: 88` on `md`+) — confirmed directly in the code, not assumed. The in-page section nav (About/Learning Plans/Availability/Reviews/FAQ) is also `position: sticky` at the top.

**Preply UX:** The right-hand price/booking card visibly stays in the exact same screen position across all three tutor-profile screenshots (top/middle/bottom) despite the left column scrolling through highlights, About, languages, and reviews — direct visual confirmation of sticky behavior.

**Problems in TutorFlow:** None — this pattern is **already implemented identically** to Preply's. This section exists to confirm parity, not to flag a gap.

**Why Preply is easier:** N/A — no difference found.

**Recommended redesign:** None. Worth verifying in a future phase that the bottom-of-page duplicate "Ready to get started?" CTA card (§4) is still needed now that the sticky rail is confirmed to work — it may be safe to remove as redundant.

**Priority:** Low (verification only). **Effort:** S. **Backend dependency:** No.

---

## 15. Card hierarchy

**Current TutorFlow UX (`TutorCard.tsx`):** Header (silhouette avatar, subject-as-name, verified checkmark, location) → meta block (subject/language chips, session-length text, hourly rate, "Learning Plans coming soon" badge) → three stacked full-width actions (Book Lesson, View profile, View Learning Plans link).

**Preply UX:** Photo/video thumbnail is the dominant visual element (not a small avatar — a real, large preview image/video), name + verified flag + native-language flag icon, credential badges (Super Tutor/Professional), price prominently right-aligned at the top (not buried in a meta list), rating + review/student/lesson counts as a single scannable line, a 2-line bio excerpt with "Learn more," then exactly two actions (Book trial lesson / Send message).

**Problems in TutorFlow:** Price is visually equal-weighted with every other meta fact (just another line of text) rather than being a distinct, prominent element the way Preply right-aligns it at card-top. Three-deep vertical actions vs. Preply's two. No rating/review line is possible today [Gated by DISC-3].

**Why Preply is easier:** Price is often the second-biggest driver of a marketplace decision after "who is this person" (§7) — visually demoting it to a plain text line among five others makes comparison-shopping across cards slower.

**Recommended redesign:** Give the hourly rate its own prominent placement (e.g., top-right of the card header, larger type weight) rather than folding it into the meta-chip block. Reduce actions to two by deferring "View Learning Plans" (see §22 gating note).

**Priority:** Medium. **Effort:** S (both are layout-only changes within `TutorCard.tsx`). **Backend dependency:** No.

---

## 16. Typography scale

**Current TutorFlow UX:** Already redesigned in Phase 1 to Preply's own stated hierarchy (Page title 44px, Section title 28–32px, Card title 20–22px, Body 16px, Caption 14px, Button 16px/600) — see `docs/design/DESIGN-SYSTEM.md` §8.3. This was implemented as a direct response to a prior explicit brief, not newly discovered in this pass.

**Preply UX:** Confirms the scale already adopted — large, heavy black display headlines, clear step-down to body copy, no more than 2–3 distinct sizes visible per screen.

**Problems in TutorFlow:** None remaining that this analysis can newly identify — already closed.

**Why Preply is easier:** N/A — parity already reached.

**Recommended redesign:** None.

**Priority:** Done — no action. **Effort:** N/A. **Backend dependency:** No.

---

## 17. Spacing system

**Current TutorFlow UX:** Card padding, card-to-card gap, and section-to-section gap conventions were documented (not universally retrofitted) in Phase 1 — `docs/design/DESIGN-SYSTEM.md` §8.7 explicitly notes a per-page `Stack spacing` sweep was judged disproportionate for that phase and left as a "going-forward convention" rather than applied everywhere.

**Preply UX:** Consistently generous — large gaps between the filter bar and the first result card, between each profile section, and inside every card's own padding.

**Problems in TutorFlow:** Some older pages (built before Phase 1) likely still use the pre-Phase-1 `spacing={2}`/tighter conventions inconsistently — not verified page-by-page in this pass.

**Why Preply is easier:** Consistency reduces cognitive load — once a user learns "sections are always this far apart," new pages feel instantly familiar.

**Recommended redesign:** The mechanical sweep Phase 1 deliberately deferred — audit every page's `Stack`/`Box` `spacing`/`gap`/`py` props against the documented 24px (cards)/48px (sections) convention and normalize. This is exactly the kind of broad, low-risk, high-consistency cleanup suited to a dedicated "Phase 2: Spacing Consistency" pass.

**Priority:** Medium. **Effort:** M (touches many files, but each change is mechanical/low-risk). **Backend dependency:** No.

---

## 18. Empty states

**Current TutorFlow UX:** Already redesigned in Phase 1 — `EmptyState.tsx` gained an 88px soft-tinted illustration circle, a real `<h5>` heading, and a single-action-slot API. Copy is already friendly and specific across the app (e.g., "No lessons yet" / "You haven't booked your first lesson. Find a tutor to get started.").

**Preply UX:** `my-lessons.png` — "No lessons yet" heading, one explanatory line, one pink CTA ("Find a private tutor"). `messages.png` — plain centered text "Select a tutor to start a conversation" (no illustration, no CTA at all — a much lighter-touch empty state for a secondary/detail pane).

**Problems in TutorFlow:** None structurally — TutorFlow's empty-state component is already at (or arguably beyond, with its illustration circle) Preply's own level of polish. One real difference: Preply varies empty-state *weight* by context (a full page gets an illustration + CTA; a "nothing selected yet" detail pane gets one plain sentence) — TutorFlow's `EmptyState` is used fairly uniformly everywhere.

**Why Preply is easier:** Not overdesigning a low-stakes "nothing selected" moment avoids visual noise in a two-pane layout.

**Recommended redesign:** No component change needed. Where `EmptyState` is used for a lightweight "nothing selected" moment (if TutorFlow ever adopts a two-pane messages view, see §25), prefer a plain text treatment over the full illustration+CTA pattern.

**Priority:** Low. **Effort:** S. **Backend dependency:** No.

---

## 19. Error states

**Current TutorFlow UX:** Already redesigned in Phase 1 — `ErrorState.tsx`/`ErrorBoundary.tsx` default copy changed to "We couldn't load this page" / "We couldn't load this right now. Please try again," replacing the earlier generic "Something went wrong."

**Preply UX:** **No error state was captured in any screenshot** — cannot compare directly.

**Problems in TutorFlow:** Not assessable via direct comparison.

**Why Preply is easier:** Not assessable.

**Recommended redesign:** None from this analysis — already addressed in Phase 1 independent of Preply reference material.

**Priority:** Done — no action. **Effort:** N/A. **Backend dependency:** No.

---

## 20. Loading states

**Current TutorFlow UX:** Already redesigned in Phase 1 — `MuiSkeleton` theme override gives every skeleton rounded corners matching the new card radius. Numerous dedicated, layout-matching skeleton components already exist (`TutorCardSkeleton`, `SessionCardSkeleton`, `ConversationListItemSkeleton`, `SessionDetailSkeleton`, `BookingPageSkeleton`, etc.) — confirmed by direct code reading, not assumed.

**Preply UX:** **No loading state was captured in any screenshot** (all screenshots show fully-loaded content) — cannot compare directly.

**Problems in TutorFlow:** Not assessable via direct comparison. One thing noted in research (not Preply-derived): `DeclareAvailabilityPage`'s schedule section uses a plain "Loading your schedule…" text line rather than a skeleton, inconsistent with the rest of the app's skeleton-everywhere convention.

**Why Preply is easier:** Not assessable.

**Recommended redesign:** Replace `DeclareAvailabilityPage`'s text-based loading state with a skeleton matching `WeeklyAvailabilityCalendar`'s layout, for internal consistency (not a Preply-driven change).

**Priority:** Low. **Effort:** S. **Backend dependency:** No.

---

## 21. Payment flow

**Current TutorFlow UX:** **Does not exist.** `BookSessionPage`'s final step calls `POST /sessions` directly — no payment method selection, no card entry, no total-price computation, no checkout page of any kind, anywhere in the app. `docs/adr/ADR-020-payments-architecture.md` (Proposed, never Accepted, superseded) and `ADR-021` (Proposed, pending several "Questions Requiring Approval") are the only design work that exists, and neither authorizes any implementation yet.

**Preply UX (`booking-confirm.png`):** A dedicated checkout page: tutor summary card (name, rating, review count, students/lessons/years-teaching stats), trial-lesson details card (exact date/time, a **"Cancel or reschedule for free until [deadline]"** policy line), a payment-method grid (Card/Apple Pay/Google Pay/PayPal), a card-entry form with a "save this card" checkbox, a single pay button labeled with the exact charge amount ("Book lesson and pay · €15.05"), and trust copy ("It's safe to pay on Preply. All transactions are protected by SSL encryption.") plus a policy link.

**Problems in TutorFlow:** This is not a "TutorFlow does it worse" gap — it is a **product/architecture decision that has never been made**, per CLAUDE.md's own stop-condition rules (new business rule, new ADR required). No amount of frontend redesign can close this gap; it is blocked on the owner resolving `ADR-021`'s open questions (Learning Plan/Enrollment/Settlement design) before any Domain/Application/Web work can start, per the ADR's own Status line.

**Why Preply is easier:** Preply has a payment flow at all; TutorFlow currently has none.

**Recommended redesign:** Not a redesign recommendation — a **flag for the owner**: this is the largest single gap in the entire document, but implementing it is gated behind `ADR-021`'s pending decisions (which pre-date this analysis and were already surfaced to the owner in that ADR). If/when the owner accepts a design, Preply's checkout page (single-screen summary + policy + payment method grid + one clearly-labeled total-inclusive pay button) is a reasonable structural reference for that future phase's frontend.

**Priority:** Critical (from a product-completeness standpoint) — but **explicitly gated on an owner decision, not schedulable as a normal engineering phase.** **Effort:** XL (new Domain aggregate, new Infrastructure payment-gateway adapter, new Web endpoints, full checkout UI, plus real PSP integration per `ADR-018`'s Iranian-PSP-only constraint). **Backend dependency:** Yes — the entire feature is backend-first by nature.

---

## 22. Review section

**Current TutorFlow UX:** A fully static placeholder — `ReviewsSection` on `TutorDetailPage` makes no API call at all (no review endpoint exists) and always renders "No reviews yet" / "This tutor hasn't received any reviews yet. Book a lesson to be one of the first to share your experience." `TutorCard`'s own code comment confirms: "no rating, review count, or bio is shown because none exists in the API."

**Preply UX (`tutor-profile-bottom.png`):** A 4-metric breakdown (Reassurance/Clarity/Progress/Preparation, each out of 5, based on anonymous student ratings) plus an aggregate star score with count ("4.8 ★, based on 9 student reviews"), then individual reviews with reviewer photo, first name, date, star rating, and free-text comment.

**Problems in TutorFlow:** **This is not an implementation gap — it is a deliberate, Accepted, Must-priority product decision to exclude reviews from v1** (`PRODUCT_REQUIREMENTS.md` DISC-3 / Decision C.12, quoted in the preamble). The current static placeholder is the *correct* implementation of that decision, not a shortfall.

**Why Preply is easier:** Reviews are one of the strongest trust signals in any marketplace — but TutorFlow's product owner has explicitly chosen not to build this for v1, likely for reasons not visible in this codebase (e.g., needing a critical mass of completed lessons first, or moderation/abuse concerns for a new platform).

**Recommended redesign:** **None — do not implement.** Per CLAUDE.md's stop conditions, this would require the owner to explicitly reopen Decision C.12/DISC-3 before any design or code work begins; this document is not the mechanism for that decision and does not recommend it. Recorded here only so a future reader of this gap analysis doesn't mistake the static placeholder for an oversight.

**Priority:** **Not applicable — blocked by an Accepted decision, not a priority-rankable backlog item.** **Effort:** N/A. **Backend dependency:** Yes, if the decision is ever reversed (new aggregate, new endpoints, new authorization rows, new audit coverage per CLAUDE.md's Domain Event rules).

---

## 23. Availability calendar

**Current TutorFlow UX:** Two different, non-calendar widgets: (1) `WeeklyAvailabilityCalendar` (Tutor-facing, "Manage Your Schedule") — a fixed "today + next 6 days" horizontally-scrolling strip, no navigation to other weeks, slots shown as small colored chips (Available/Booked/Past legend), no delete/edit capability for a declared slot (confirmed by an explicit code comment: "there is no cancel/delete-availability capability in this API"). (2) The booking wizard's own flat date-chip row (Student-facing) — no calendar grid at all, just clickable date pills for dates that already have open slots.

**Preply UX (`booking-step1.png`):** A week-strip with **left/right navigation arrows** ("Jul 28 – Aug 3, 2026" ⟷), 7 day columns, and — once a day is picked — slots grouped by Morning/Afternoon/Evening headers below the strip.

**Problems in TutorFlow:** The Tutor's own calendar cannot be navigated to a future week at all (always exactly "today + 6"), and has no way to remove/edit a mistakenly-declared slot from the UI. The Student-facing wizard has no time-of-day grouping (§5).

**Why Preply is easier:** Being able to look ahead more than a week, and to fix a mistake without contacting support, are basic calendar-management expectations.

**Recommended redesign:** Add week-navigation (prev/next) to `WeeklyAvailabilityCalendar` — this is mostly a frontend date-window change, but "delete availability" is a **new Domain capability** (currently, per the code comment, structurally impossible — `AvailabilitySlot` has no retraction behavior). Time-of-day grouping for the Student wizard is the same frontend-only change already flagged in §5.

**Priority:** High (delete/edit availability is a real, frequently-needed capability, not a nice-to-have) / Medium (week navigation). **Effort:** M (week nav, frontend-only) / L (delete-availability — new Domain method + Application command + validation for "not already booked" + new endpoint + authorization row + tests across all three layers). **Backend dependency:** No (week nav) / Yes (delete-availability).

---

## 24. Lesson flow

**Current TutorFlow UX:** `MeetingCard` on `SessionDetailPage` — role-aware states: Tutor sees "Start Lesson" (creates the meeting via the configured provider — Google Meet/Teams/Zoom/Mock per ADR-023); Student sees "Join Lesson"; Parent/Guardian sees "View Meeting"; Admin sees no join action (read-only). A live countdown/timing badge (`deriveMeetingTiming`) shows whether the meeting is currently live. `SessionDetailPage` also has "Notes"/"Homework"/"History" sections that are all static "coming soon" placeholders.

**Preply UX:** **No direct screenshot evidence of an in-progress/join-lesson screen** — the closest evidence is `Home.png`'s tutor video-preview thumbnail (a pre-recorded intro clip with a play button, not a live lesson).

**Problems in TutorFlow:** Not assessable via direct comparison for the join/live-lesson experience itself. The three "coming soon" placeholders (Notes/Homework/History) are honestly-labeled non-features, not a Preply gap.

**Why Preply is easier:** Not assessable.

**Recommended redesign:** None from Preply comparison. TutorFlow's own multi-provider meeting architecture (ADR-023) is already a more flexible design than what any single screenshot could evidence.

**Priority:** N/A (insufficient evidence for a Preply-driven recommendation). **Effort:** N/A. **Backend dependency:** N/A.

---

## 25. Conversation flow

**Current TutorFlow UX:** Two separate full-page routes (Inbox list page, Conversation detail page) — not a two-pane split view. Inbox has a single free-text search box (no All/Unread/Archived tabs), filtering only by last-message preview text (no participant name field exists to search by). Each row shows the other participant's **raw id in monospace** — no name, no avatar — plus an unread-count pill and a timestamp. Detail page shows a message thread with a back-arrow to return to the inbox list.

**Preply UX (`messages.png`):** All/Unread/Archived tab row above the conversation list; each row shows the participant's real name and photo; a **single-page, two-pane layout** — list on the left, detail/thread on the right, both visible simultaneously, no full navigation required to open a conversation.

**Problems in TutorFlow:** Raw ids instead of names/avatars in every conversation row is a significant readability/trust gap (a Student can't tell which "conversation" is with which tutor without opening it) — and this traces back to the same root cause as §7: no tutor/user-facing display name exists in the domain at all. The two-page (not two-pane) navigation is a heavier interaction for what's usually a quick back-and-forth check.

**Why Preply is easier:** Seeing who you're talking to, and reading the latest exchange, without a full page navigation, is materially faster for a "check messages" habit loop.

**Recommended redesign:** Once a display-name field exists (§7's recommended `Tutor` bio/photo work — a Student-facing display name is a related, possibly prerequisite change), swap the raw-id text in `ConversationListItem` for it. Restructuring Inbox+Detail into a single two-pane responsive layout (list always visible on desktop, detail slides over on mobile) is a real frontend restructuring, independent of any backend change, and would also let `EmptyState`'s "select a conversation" moment go from a full empty page to the lightweight in-pane treatment noted in §18.

**Priority:** High (both — name/avatar is high-value and low-effort once §7's identity fields exist; two-pane layout is a meaningful, if larger, UX upgrade). **Effort:** S (raw id → name swap, contingent on §7) / L (two-pane restructuring). **Backend dependency:** Yes for names (shares §7's Domain change) / No for the two-pane layout itself.

---

## 26. Notification UX

**Current TutorFlow UX:** A bell `IconButton` in `AppHeader` (`NotificationsButton.tsx`) opens a `Popover` listing the 10 most recent notifications with unread-bold styling, a "Mark all read" action, and per-item click-to-navigate (routes by `NotificationType` to the relevant Session/Conversation/Relationship). Already reviewed and polished in Phase 1 (dropped its `size="small"` override for a proper 44px touch target).

**Preply UX:** A bell icon is visible in the top bar in every screenshot, but **it was never clicked/opened** in any of the 10 captures — no dropdown/panel content evidence exists.

**Problems in TutorFlow:** Not assessable via direct comparison — cannot compare content/layout of a panel that was never shown.

**Why Preply is easier:** Not assessable.

**Recommended redesign:** None from this analysis.

**Priority:** N/A (insufficient evidence). **Effort:** N/A. **Backend dependency:** N/A.

---

## 27. Color psychology

**Current TutorFlow UX:** Already redesigned in Phase 1 to a single Deep Emerald primary color (`#0F766E`), reserved for primary actions/links/selected-nav/focus rings, against a neutral grey/white base — see `docs/design/DESIGN-SYSTEM.md` §8. (Emerald was the owner's own explicit color choice for this phase, not derived from or matching Preply's pink — correctly not copied.)

**Preply UX:** A warm pink/magenta accent reserved almost exclusively for the single primary CTA per screen ("Book trial lesson," "Continue," "Book lesson and pay"), against a stark black/white/grey base — a deliberately "warm and human" choice for a tutoring-relationship product, contrasted with black text for authority/trust.

**Problems in TutorFlow:** None to copy (color choice itself is explicitly out of scope per this task's own instructions) — the transferable *principle*, not the hue, is: reserve the accent color exclusively for the single most important action per screen, never for decorative or secondary elements. §3 already flagged where TutorFlow's current primary-color usage is broader than that principle.

**Why Preply is easier:** See §3 — strict color discipline, not the specific hue, is what does the work.

**Recommended redesign:** Same as §3 — a color-discipline audit, not a re-palette.

**Priority:** Low. **Effort:** M. **Backend dependency:** No.

---

## 28. Visual density

**Current TutorFlow UX:** `TutorCard` packs avatar, name, verified badge, location, two chips, session-length text, rate, a Learning-Plans badge, and three buttons into one 280–360px-wide card — a good deal of information per card. `TutorDetailPage` is comparatively spacious (two-column layout, generous card padding from Phase 1's 32px `CardContent` default).

**Preply UX:** Search-result cards are moderately dense (photo, name badges, price, rating line, 2-line bio excerpt, two buttons) but visually organized into clear horizontal bands via the photo's own bounding box; profile pages are notably airy — large video, generous whitespace between every section.

**Problems in TutorFlow:** `TutorCard`'s three-stacked-button footer (§15) is the main density complaint — everything above it is a reasonable amount of scannable metadata for a comparison-shopping card.

**Why Preply is easier:** Same root cause as §4/§15 — fewer, better-differentiated actions.

**Recommended redesign:** Same fix as §15 (reduce to two actions). No separate work needed here.

**Priority:** Low (folded into §15). **Effort:** N/A (see §15). **Backend dependency:** No.

---

## 29. Form simplification

**Current TutorFlow UX:** `TutorOfferingPage`'s edit form: five plain stacked text fields (Hourly rate, Subject, Language, Location, Offered durations as a comma-separated string), one "Save changes" button that only submits changed fields. `AddTeachingTimeDialog`: three fields (native `datetime-local` picker, free-text duration-in-minutes, delivery-mode select).

**Preply UX (`booking-confirm.png`):** Checkout form is minimal — one payment-method choice (visually a large tappable card, not a dropdown), then exactly 3 fields for a new card (number, expiry, CVC — laid out compactly in one row split two ways) plus one checkbox, one button. `booking-step1.png`'s "form" has *zero* text-entry fields at all — every choice is tap-only (duration toggle, day, time slot).

**Problems in TutorFlow:** The "Offered durations (minutes, comma-separated)" field (`TutorOfferingPage`) asks a Tutor to type `"30, 60, 90"` as raw text — a real usability rough edge (no validation feedback shown until submit, easy to mistype a comma or unit). `AddTeachingTimeDialog`'s duration is also free-text-numeric rather than a preset chip/button set (30/45/60/90).

**Why Preply is easier:** Tap-to-choose from a small fixed set (a chip, a toggle) is faster and error-proof compared to typing a value the system then has to parse/validate.

**Recommended redesign:** Replace the comma-separated durations text field with a multi-select chip group of common durations (e.g., 30/45/60/90 min, plus an "Other" free-text fallback for an unusual length) in `TutorOfferingPage`. Replace `AddTeachingTimeDialog`'s free-text duration field with the same chip set. Both are frontend-only (the underlying API already accepts a list of minute-values / a single integer respectively — no contract change).

**Priority:** Medium. **Effort:** S (both are self-contained form-field swaps). **Backend dependency:** No.

---

## 30. Progressive disclosure

**Current TutorFlow UX:** `TutorDetailPage` renders every section fully at once (About, Teaching Information, Learning Plans, Availability, Reviews, FAQ) — nothing is collapsed or truncated by default; the sticky in-page nav exists to *jump* to a section, not to hide it. `TutorOfferingPage`/`AddTeachingTimeDialog` show every field at once, no step-by-step reveal.

**Preply UX:** Bio text is truncated with a "Show more" link; badge explanations ("Professional Tutor," "Super Tutor") are one-line summaries with a "Learn more" link rather than the full explanation inline; the booking modal only reveals time slots after a day is picked, and only reveals the payment form after a slot is confirmed (via the separate checkout page) — each step deliberately shows only what's relevant to the current decision.

**Problems in TutorFlow:** None of TutorFlow's static/placeholder sections (Learning Plans, Reviews, FAQ) are long enough to need truncation today — this gap is more relevant to real content (Tutor bio, once §7 adds one) than to anything currently on the page. The booking wizard's own 5-step structure (§5) is arguably already stricter progressive disclosure than Preply's 2-screen flow, just spread across more steps.

**Why Preply is easier:** Showing only what's decision-relevant *right now* reduces a long profile page to a skimmable summary, with detail available on demand.

**Recommended redesign:** Once §7 adds a real Tutor bio field, truncate it with a "Show more" expander (a frontend-only pattern, `Collapse`/local state — no backend change) rather than rendering the full text inline by default.

**Priority:** Low (contingent on §7 shipping first). **Effort:** S. **Backend dependency:** No (contingent on §7's field existing).

---

## 31. Trust-building patterns

**Current TutorFlow UX:** A "Verified" checkmark chip shown when `tutor.isApproved` (the only trust signal that exists today, and it's really an Admin-approval fact, not a credential/reputation signal). No review count, no rating, no "years teaching," no student/lesson counts, no cancellation-policy copy anywhere in the booking flow, no payment-security copy (no payment flow exists at all, §21).

**Preply UX:** Layered trust signals throughout: "Professional Tutor"/"Super Tutor" credential badges with "Learn more" links, aggregate rating + review count, 4-metric rating breakdown, student/lesson/years-teaching counts, a "Not a match? You still have 2 free tutor trials" reassurance banner directly in the booking rail, an explicit "Cancel or reschedule for free until [time]" policy line at checkout, and "It's safe to pay on Preply. All transactions are protected by SSL encryption" at the payment step.

**Problems in TutorFlow:** Most of Preply's trust signals are downstream of gaps already flagged elsewhere in this document (reviews — gated by DISC-3, §22; bio/photo — §7) or don't exist yet (payments — §21). The one trust-building pattern that's genuinely independent of those gates and cheaply addable today: **cancellation-policy copy**. TutorFlow's `SessionActions` cancel-confirmation dialog says only "This cannot be undone" — it never states what the actual cancellation policy is (how much notice is required, what happens to a paid session — moot until §21, but relevant for the Tutor's own declared-availability side today).

**Why Preply is easier:** Explicit, specific reassurance ("free until this exact time") reduces booking anxiety more than a generic warning does.

**Recommended redesign:** Check `PRODUCT_REQUIREMENTS.md`/`DOMAIN_MODEL.md` for whether a cancellation-notice-period policy has actually been decided (the summary of this session's own prior work flags "cancellation notice periods" as a known **unresolved Open Question** in those documents) — if so, surface its actual text in the cancel dialog; if not, this is itself a CLAUDE.md stop condition (an unresolved Open Question) and needs the owner's decision before any copy can be written, not a UI-only fix.

**Priority:** Medium — but gated on confirming/resolving the Open Question first, same governance shape as §21/§22. **Effort:** S once the policy exists. **Backend dependency:** No (copy-only) — unless the policy itself needs to be enforced server-side (e.g., blocking cancellation inside the notice window), which would be a real Application-layer change.

---

## Roadmap summary (prioritized)

For planning the next redesign phase(s). Sorted by priority, then by whether it's schedulable now vs. gated on an owner decision.

| # | Area | Priority | Effort | Backend? | Schedulable now? |
|---|---|---|---|---|---|
| 7 | Tutor photo + bio | **Critical** | L | Yes | Yes |
| 21 | Payment flow | **Critical** | XL | Yes | **No — gated on ADR-021 decision** |
| 22 | Review section | N/A | N/A | Yes (if reversed) | **No — gated on reopening DISC-3** |
| 6 | Search filters visible + price filter | High | S | No (mostly) | Yes |
| 23 | Delete/edit declared availability | High | L | Yes | Yes |
| 25 | Conversation names/avatars + two-pane layout | High | S/L | Partial (names depend on §7) | Yes (layout) / After §7 (names) |
| 2 | Shorten booking flow (modal for date/time) | Medium | L | No | Yes |
| 4 | CTA reduction + verify sticky rail | Medium | S | No | Yes |
| 5 | Time-of-day slot grouping | Medium | S | No | Yes |
| 15 | Card price prominence + fewer actions | Medium | S | No | Yes |
| 17 | Spacing consistency sweep | Medium | M | No | Yes |
| 29 | Duration chips instead of free-text | Medium | S | No | Yes |
| 31 | Cancellation-policy copy | Medium | S | No | **Check Open Question status first** |
| 1 | IA research spike (public discovery reachability) | Low | Spike | No | Yes |
| 3, 27 | Color/primary-usage discipline audit | Low | M | No | Yes |
| 8 | Subject-filtered Recommended Tutors | Low | S | No | Yes |
| 18, 20 | Empty/loading state consistency touch-ups | Low | S | No | Yes |
| 30 | Bio "Show more" truncation | Low | S | No (after §7) | After §7 |
| 9–14, 16, 19, 24, 26 | No action (already addressed, no evidence, or not applicable) | — | — | — | — |

**Two items are not on this backlog at all because they are not TutorFlow's decision to schedule as engineering work:** the payment flow (§21) and the review section (§22). Both require the owner to make (or reopen) a product decision first — `ADR-021`'s pending questions and `PRODUCT_REQUIREMENTS.md` Decision C.12, respectively — per CLAUDE.md's stop-condition rules. Recommend raising both explicitly with the owner before any future phase attempts to "close the Preply gap" on either.
