# Preply Tutor Profile Parity Spec

Status: **Implemented (2026-10-04).** §13's open questions were resolved by explicit Product
Owner decisions and are now reflected in `ADR-0022-tutor-profile-public-surface.md` and the
feature's completion report. §12's Bucket A is complete; Bucket B items (Guest availability
visibility, Favorites, Messaging CTA boundary, trial eligibility, structured data scope, media
hosting) were resolved as part of this same implementation pass — see ADR-0022 for the final
decisions and the completion report for file-level detail. Bucket C remains explicitly deferred.

<details>
<summary>Original Phase 0 status (superseded)</summary>

Status: **Proposed — Phase 0 research/planning only. No code, migrations, or endpoints exist yet
for this feature.** Nothing in this document authorizes implementation by itself (same convention
as every other doc under `docs/decisions` and `docs/design`).

</details>

Researched via `WebFetch`/`WebSearch` against official Preply sources only (preply.com,
help.preply.com) — no proprietary text, images, or branding copied; structure/behavior described
in our own words, per the permanent Preply-benchmark rule. Every Preply finding is tagged
**Confirmed** (fetched/sourced directly), **Partially confirmed** (plausible but not literally
observed), or **Not confirmed**. Supersedes/extends row 26 of `PREPLY_SEARCH_PARITY_MATRIX.md`
("Book trial lesson" CTA — "real CTA lands with Tutor Profile"), which should be updated to point
here once this spec is approved.

---

## 1. Current Preply findings

### 1.1 URL structure — Confirmed

`preply.com/en/tutor/[numeric ID]` (e.g. `/en/tutor/3739073`). **Preply itself does not use a
slug** — the tutor's name never appears in the URL path, only as page content. Locale is a path
prefix (`/en/`). Source: direct fetch of multiple profile URLs found via search.

### 1.2 Hero section — Confirmed

Large photo, name + first initial of last name (e.g. "John M."), role line ("English tutor"),
country of origin with flag ("From Lesotho"), lesson rating (e.g. "4.2") with a breakdown into
sub-dimensions ("Reassurance," "Clarity," "Progress," "Preparation"). Source: direct fetch,
`preply.com/en/tutor/3739073`.

### 1.3 Trust badges — Confirmed

Displayed directly beneath the name: "Top X% of [subject] tutors," total lessons taught count,
years of experience (platform vs. total, e.g. "4 years experience, 3 on platform"), "Professional
Tutor" badge, "Super Tutor" badge. Both badges can co-occur.

**Professional Tutor — Confirmed.** Source:
`help.preply.com/en/articles/9795272-what-is-a-professional-tutor`. Requires ONE of: (1) a passing
grade in Preply's own "Online Language Teaching Course," or (2) a government-accredited teaching
certificate from a ≥120-hour course. Assessed once — not a rolling window.

**Super Tutor — Confirmed.** Sources: `help.preply.com/en/articles/6062191-what-is-a-super-tutor`,
`help.preply.com/en/articles/14880665-how-super-tutor-metrics-and-criteria-work` (already compiled
in `PREPLY_SEARCH_PARITY_SPEC.md`, reused here for consistency — not re-derived). All 9 criteria
measured over a trailing 90 days, reassessed monthly: ≥40 lessons taught, ≥4.8 average rating,
≥50% trial→subscription conversion, ≥80% pre-trial message response within 24h, 0 no-shows, <5%
trial cancellation/reschedule rate, <20% regular-lesson cancellation/reschedule rate, ≥80%
classroom usage for trials, ≥80% post-trial message response within 24h. Revoked immediately on
any no-show. Reported +15% trial-booking rate for badge holders.

### 1.4 Price — Confirmed

Shown per-lesson, not per-hour (e.g. "€45 / 50-min lesson"), in a persistent sidebar/panel that
also carries the primary CTA. No discount/promo copy observed near price on any fetched profile.

### 1.5 CTA set and hierarchy — Confirmed

- **Primary**: "Book trial lesson" — appears in the sticky/sidebar panel and is repeated inline.
- **Secondary, same panel**: "Not a match? You still have N free tutor trials" (reassurance
  copy reducing booking-commitment anxiety).
- **Favorite (heart icon)** — Confirmed via Preply's own help content (not directly observed in
  fetched markup): present on both the search card and the profile itself; saves the tutor to a
  "Favourites"/"Save to my list" page. Tutors who reply to a student who favorited them are
  reported "50x more likely" to convert that student. Source: `help.preply.com` search results on
  favoriting/saved-tutors articles.
- **Message** — Confirmed present as a card-level CTA on search results ("Send message" button,
  separate from "Book trial lesson"); **not confirmed** as a visually distinct button on the
  profile page itself in the fetched markup (the profile's own messaging entry point may be
  folded into a more general contact flow) — treat the search-card finding as authoritative for
  *existence* of the capability, profile-page *exact placement* as partially confirmed.
- **CTA swap on unavailable tutors — Confirmed by direct observation**: fetching a real profile
  (`preply.com/en/tutor/3812700`) that is not currently accepting students shows **no 404** —
  the full profile (bio, badges, credentials, reviews) renders normally, but the booking CTA is
  replaced with "Tutor isn't accepting new students" plus a secondary link back to the subject's
  tutor list. This is a real, observed state, not an inference.

### 1.6 Intro video — Partially confirmed

Present on some profiles as a static photo with a play affordance (not confirmed to autoplay);
**confirmed absent** on other real profiles fetched (plain static avatar, no video section
rendered at all — i.e. a missing video is not shown as an empty-state placeholder, the whole
video element is simply omitted).

### 1.7 About / bio — Confirmed

Free-text narrative: specialization focus, years of experience, qualifications (degree, TEFL,
etc.), a value proposition sentence, and a closing invitation to book a trial. Written in first
person as marketing copy, not structured fields.

### 1.8 Subjects / specialties — Confirmed

Presented as expandable cards, one per specialty (e.g. "Conversational English," "Business
English," "English for beginners," "Job interview prep"), each with its own short descriptive
paragraph of approach/outcome — not just a label chip.

### 1.9 Spoken languages — Confirmed

Simple list with proficiency label, e.g. "English — Native." No further structure observed (no
CEFR-style levels for secondary languages on the fetched profile, though this may simply reflect
that specific tutor's data, not a platform limitation).

### 1.10 Education / experience / certificates — Confirmed

A "resume"-style combined section: Education entries with date range, institution, degree, and a
"Diploma verified" trust indicator; certification mentions (e.g. TEFL) referenced inline rather
than as a separate rigidly-structured list in all cases.

### 1.11 Reviews — Confirmed

Aggregate star rating + total count (e.g. "5★ from 36 student reviews"). Individual review cards:
student name/avatar, date, per-review star rating, review text, "Show more"/"Show original"
expansion for long or translated text, and a "Show all N reviews" link that expands beyond the
initially-rendered subset.

### 1.12 Availability / calendar — Confirmed

A "Schedule" section titled with explicit timezone disclosure copy ("The timings are displayed in
your local timezone"). Weekly grid (Sun–Sat), clickable dates, time slots rendered within.
Directly tied to the booking flow — selecting a slot is how "Book trial lesson" is completed, not
a separate browse-only widget.

### 1.13 Trial booking mechanics — Confirmed

Source: `help.preply.com/en/articles/4179487-how-to-book-a-trial-lesson`. Flow: profile → "Book
trial lesson" → choose 25 or 50 minutes → select date/time → choose payment method or use lesson
balance → confirm. **Each tutor offers exactly one trial lesson per student** — once used, that
tutor's CTA for that student becomes "schedule a (regular/subscription) lesson" instead, handled
through a different post-subscription flow ("My lessons" → "Schedule lesson"), not the profile's
trial CTA. Trial lessons are paid, not free.

### 1.14 Messaging eligibility — Partially confirmed

Source: `help.preply.com/en/articles/7958525-messaging-potential-students` (tutor-facing article,
doesn't fully specify the student-facing rule). Confirms tutors are notified when a student
favorites them or abandons a booking, and are strongly incentivized to respond quickly. Does
**not** confirm from Preply's own content whether *any* student can message *any* tutor with no
prior interaction — TutorFlow's own `MESSAGING_SPEC.md` already states this explicitly for our
product ("any authenticated Student can message any Published Tutor directly from the tutor's
profile, no prior booking required") — treated here as a TutorFlow product decision consistent
with, but not purely copied from, Preply.

### 1.15 Similar/recommended tutors — Confirmed

"You might also like" carousel beneath the main content: other tutors (same subject), each shown
in the same card shape as search results (photo, name, origin, rating, price, short descriptor).

### 1.16 Profile visibility / unpublished / hidden — Confirmed

Source: `help.preply.com/en/articles/4175297-tutor-profile-visibility-how-to-hide-or-show-your-
profile`. A profile is auto-hidden for: repeated lesson absences, unanswered new-student messages
>24h (auto-restored on reply), conduct violations, or a broken intro video. Tutors can also
manually hide their own profile. Hidden profiles drop out of **search results**; current students
can still reach their existing tutor. **Not confirmed**: exact behavior when a student has a
*direct link* to a hidden profile's URL (no Preply source observed this specific case) — the
"not accepting new students" state observed in §1.5 is the closest real evidence, and is a
*different* state (still published, just not bookable) from *hidden* (pulled from search).

### 1.17 Desktop vs. mobile layout — Partially confirmed

Desktop: sidebar booking panel alongside scrolling content. Mobile app: same CTA ("Buy trial
lesson" is the app's exact label vs. "Book trial lesson" on web) reached from the profile screen.
**Not confirmed**: whether the mobile *web* (not app) layout uses a sticky bottom action bar — no
source directly observed this; it is a common enough marketplace pattern that TutorFlow adopting
it is a reasonable independent design choice, not a verified copy.

### 1.18 Search → profile link — Confirmed

Source: direct fetch of `preply.com/en/online/english-tutors`. Each result card already carries
**both** "Book trial lesson" and "Send message" CTAs directly on the card (no profile visit
required to start either action), plus a separate "profile" link that opens the full page. Tutor
name links to `/en/tutor/[ID]`.

### 1.19 Breadcrumb / back-to-search navigation — Confirmed

`Find Tutors › [Subject] Tutors Online › [Tutor Name]`, rendered near the footer on the fetched
page (not necessarily sticky/top-of-page).

### 1.20 SEO — Not confirmed (structured data), Confirmed (indexable, crawlable)

Profile pages are plain server-rendered HTML with normal `<title>`/meta content, locale-prefixed
URLs, and crawlable internal links (breadcrumb, similar-tutors, footer). No `schema.org` JSON-LD
block was observed in the fetched markdown conversion — **this doesn't disprove its presence**
(JSON-LD can be stripped by HTML→Markdown conversion), so treat structured data as unconfirmed
either way, not assumed absent.

### 1.21 Loading / empty / error states — Not confirmed

Not observable via a static fetch (these are client-side runtime states). No claim made either
way; TutorFlow's own `UX_STATES_PRINCIPLES.md` 7-state policy governs these regardless of what
Preply specifically does.

---

## 2. Parity matrix

| Preply behavior | TutorFlow equivalent | Domain owner | API/query | Frontend component | State | Deviation |
|---|---|---|---|---|---|---|
| Numeric-ID profile URL, no slug | `GET /tutors/{tutorUserId}` (Guid) | `Identity` (UserId per ADR-0011) | n/a (route param) | `app/tutors/[tutorUserId]/page.tsx` (exists, stub) | — | None — TutorFlow already matches Preply's own "no slug" approach; see §5. |
| Hero: photo, name, origin, headline, rating | Public profile projection | `Tutors` | **NEW** `GET /api/tutors/{userId}` | `TutorProfileHero` **(new)** | loading / loaded / not-found | Rating sourced from `Reviews` once it exists, not duplicated from `Tutors` (single-source-of-truth) |
| Professional Tutor / Super Tutor badges | Same two badges | `Tutors` (`IsProfessionalTutor`, `IsSuperTutor` — both already modeled) | same `GET /api/tutors/{userId}` | `TutorTrustBadges` **(new shared atom** — extracted for reuse with `TutorResultCard`) | static | **Intentional**: v1 badges are admin/manually set flags, not Preply's dynamic 90-day/9-metric computation — see §11 |
| Price per lesson | `HourlyRate` display | `Tutors` | same endpoint | `BookingPanel` | static | None |
| "Book trial lesson" CTA | Same CTA, calls existing booking flow | `Bookings` (already implemented) | `POST /api/bookings/trial-holds` (existing) | `BookingPanel` | enabled / disabled (trial-used) / disabled (guest) | Guest pre-signup behavior depends on pre-existing `OPEN_QUESTIONS.md` #5 — not re-decided here |
| CTA swap when tutor unavailable | Show profile, replace CTA with reason | `Tutors` | profile endpoint should return a `BookingEligibilityReason` field | `BookingPanel` | `not-accepting` | **Gap**: TutorFlow's `Tutors` domain has no "temporarily not accepting students" state today (binary Published/not) — see §6 |
| "Not a match? N free trials" reassurance copy | Omit or replace with TutorFlow-native copy | — | — | `BookingPanel` | — | **Intentional**: proprietary Preply marketing mechanic, not copied; TutorFlow may add its own reassurance copy later, not required for parity |
| Favorite (heart) | Favorite toggle | `Students` (`FavoriteTutor` — ownership already decided in `PREPLY_SEARCH_PARITY_MATRIX.md` row 28, still a stub) | **NEW** `POST/DELETE /api/students/me/favorites/{tutorUserId}` | `BookingPanel` heart toggle | guest (prompt login) / idle / favorited | None — ownership already settled by precedent, just not built |
| Send message | Message CTA | `Messaging` (stub) | **NEW** (future) `POST /api/messaging/conversations` | `BookingPanel` message button | guest (prompt login) / disabled (module stub) | **Temporary**: ships disabled/"coming soon" this phase — see §6 |
| Intro video (when present) | Video player, click-to-play | `Tutors` (`IntroVideoReference`, already modeled) | same endpoint | `TutorIntroVideo` **(new, conditional)** | present / absent (no empty-state shown) | None — matches Preply's own "omit entirely if absent" behavior |
| About / bio | Free-text section | `Tutors` (`About`, already modeled) | same endpoint | `TutorAbout` **(new)** | static | None |
| Subjects / specialties as expandable cards | Same shape, resolved via catalog | `Tutors` (IDs) + `Subjects` (names, per ADR-0021 client-side join) | same endpoint + existing `/api/subjects`, `/api/specialties` | `TutorSubjectsAndSpecialties` **(new)**, reuses `useCatalog()` | loading / loaded | None |
| Spoken languages with proficiency | Same | `Tutors` (`TutorSpokenLanguage`, ADR-0020) | same endpoint | `TutorLanguages` **(new)** | static | None |
| Education / experience / certificates ("resume") | Combined credentials section | `Tutors` (`EducationEntries`, `CertificateEntries`, `YearsOfExperience`, `ExperienceSummary` — already modeled) | same endpoint | `TutorCredentials` **(new, combines 3 Preply-separate blocks into one — simplification, not loss)** | loading / loaded / empty (per sub-section) | None |
| Reviews: aggregate + list + expand | Same shape, real data later | `Reviews` (**stub — no entities**) | **future** `GET /api/reviews/tutors/{userId}` | `TutorReviews` **(new)**, empty-state now | **empty** (ships as "No reviews yet") | **Temporary** — `Reviews` module has zero data pipeline; matches `FRONTEND_IMPLEMENTATION_ROADMAP.md`'s own prior plan to ship this empty |
| Weekly availability grid tied to booking | Date/time picker feeding the trial-hold | `Availability` (slots already implemented) | existing `GET /api/availability/tutors/{userId}/slots` | `TutorAvailability` (`DatePicker` + `TimeSlotPicker`, **new — not yet built anywhere in the frontend despite being named in `PREPLY_COMPONENT_INVENTORY.md`**) | guest (preview-only, **open question**) / full picker (authenticated) | **Gap**: endpoint currently requires auth for *any* access — see §6 |
| One trial per tutor; CTA becomes "schedule lesson" after | Same business rule | `Bookings` | **NEW** lightweight eligibility read, e.g. `GET /api/bookings/me/trial-eligibility/{tutorUserId}` | `BookingPanel` CTA label | trial-available / trial-used | **Gap**: no such read exists today — see §6 |
| "You might also like" carousel | Similar tutors, same subject | `Search` (existing `/api/search/tutors`, filtered by shared subject, excluding self) | existing `GET /api/search/tutors?subjectId=...` | `SimilarTutors` **(new wrapper, reuses existing `TutorResultCard`)** | loading / loaded / empty (hide section) | None |
| Breadcrumb back to search | Same | Frontend | — | `PageContainer`/breadcrumb (reuse existing layout primitives) | static | None |
| Hidden/unpublished profile → removed from search, direct-link behavior unconfirmed on Preply | TutorFlow: not-Published → 404, consistent with not leaking internal pipeline state | `Tutors` | same `GET /api/tutors/{userId}` (404 unless `Published`) | Next.js `notFound()` page | not-found | **Intentional** — TutorFlow treats Draft/Submitted/Approved/Rejected identically (404) rather than exposing partial states publicly; stricter than Preply's observed behavior, by design |
| SEO: indexable, crawlable, meta tags | Same | Frontend | — | `generateMetadata` in the existing page file | — | None |
| Structured data (unconfirmed on Preply) | Minimal `Person` JSON-LD, `AggregateRating` only when `ReviewCount > 0` | Frontend | — | page `<script type="application/ld+json">` | — | **Intentional, conservative**: don't emit misleading empty-aggregate schema — see §9 |

---

## 3. .NET Claude Kit reuse

Per the permanent reuse rule, every decision below checks the Kit first (skills loaded/consulted
this session: `minimal-api`, `error-handling`, `caching`; others — `ef-core`, `authentication`,
`testing` — referenced from existing TutorFlow code that already follows them, not re-loaded).

| Decision | Existing pattern | Reuse | Deviation |
|---|---|---|---|
| New public profile endpoint | Kit's `IEndpointGroup` auto-discovery (`minimal-api` skill) — already how every existing module (`TutorsModule`, `AvailabilityModule`, `SearchModule`, `BookingsModule`) maps endpoints | Add the new endpoint inside the existing `TutorsModule`'s endpoint group, same file convention as its current self-service endpoints | None |
| Not-found / not-Published handling | Kit's Result pattern + `TypedResults`/`Results<Ok<T>, NotFound>` union (`error-handling`, `minimal-api` skills) — already how `GetTutorSlots`/`Bookings` endpoints are written per the audit | `GetTutorProfile` handler returns `Results<Ok<TutorPublicProfileResponse>, NotFound>`; Published-gate failure returns `TypedResults.NotFound()`, not a 403/410, to avoid leaking "exists but hidden" vs. "doesn't exist" | None |
| Response shape | Kit's "never return domain entities" rule (`minimal-api` anti-patterns) — already followed (`TutorApplication` is never returned raw anywhere in the audited code) | New `TutorPublicProfileResponse` DTO, explicitly excluding admin/internal fields (`RejectionReason`, raw `Status` enum beyond "Published," internal review metadata) | None |
| Caching the public profile read | Kit's caching skill: `HybridCache` for data, output caching for full public GET responses | **Output caching** (`AddOutputCache` + `.CacheOutput("TutorProfile")`, tagged `tutors`, varied by `userId` route value) on `GET /api/tutors/{userId}` — it's a public, read-heavy, infrequently-mutated response, exactly the output-cache use case the skill names first. Invalidate via `IOutputCacheStore.EvictByTagAsync("tutors", ct)` from `Tutors`' existing `Publish()`/profile-edit handlers. | None — this is a new cross-cutting concern (`Tutors` currently has no caching), explicitly justified by the skill's own decision table ("Full HTTP response → Output caching"), not invented |
| Trial-eligibility read | Kit's existing `ADR-0014` cross-module synchronous query-contract pattern (already used for `ITutorAvailabilityQuery`, `IActiveBookingIntervalsQuery`, etc.) | New lightweight `GET /api/bookings/me/trial-eligibility/{tutorUserId}` inside `BookingsModule`, same shape as its existing `/me/as-student` endpoints — no new contract interface needed since this is a same-module read, not cross-module | None |
| Validation | FluentValidation + `ValidationFilter<T>` (`error-handling` skill) | Route-param binding (`Guid userId`) needs no request-body validator; reuse as-is for future mutation endpoints (favorite toggle) if a body is ever added | None |
| Composition strategy (avoid N+1 vs. god endpoint) | ADR-0021's existing precedent: ID→name resolution for subjects/specialties/languages is explicitly the **frontend's** job, not the API's; `Search`'s own read model is a denormalized projection, not a live join | Profile page composes via **parallel frontend fetches** (`GET /api/tutors/{userId}`, existing `/api/subjects`+`/api/specialties`+`/api/languages` via the already-built `useCatalog()`, existing availability-slots endpoint, future reviews endpoint) rather than inventing a new backend BFF/aggregator. Matches established precedent exactly; avoids a new cross-module composition layer and a new god-endpoint simultaneously. | None — this *is* the Kit/TutorFlow-consistent answer, see §7 |
| Module boundary check | `get_project_graph` / `find_references` via `cwm-roslyn-navigator` | Run after implementation (not yet, per this task's "no code" constraint) to confirm the new `Tutors` endpoint doesn't reach into `Reviews`/`Availability`/`Bookings` DbContexts directly | None |
| Testing | Existing xUnit v3 + `WebApplicationFactory` + Testcontainers convention (already used across `Bookings`/`Availability` tests per audit) | New tests follow the identical pattern — see §10 | None |

No competing pattern (new mediator, new validation library, new caching library, new DTO-mapping
library) is proposed anywhere in this spec.

---

## 4. Current TutorFlow gaps (what exists vs. what's missing)

**Already real and reusable as-is:**
- `Tutors.TutorApplication` already models nearly every field a profile needs (headline, about,
  photo/video references, languages, subject/specialty IDs, country, badges, experience,
  education, certificates, hourly rate).
- `Subjects` catalog (subjects/specialties/languages) is real and public.
- `Availability`'s slot-computation endpoint is real, correctly excludes active holds, and fails
  closed per ADR-0014.
- `Bookings`' full trial-hold → checkout → confirm lifecycle is real (checkout currently `501`
  pending `Payments`, which is a pre-existing, independent gap, not introduced by this feature).
- `Search`'s result card → `/tutors/{tutorUserId}` link is already wired end-to-end.
- Frontend design tokens, 24 `ui/` primitives, `Skeleton`/`EmptyState`/`ErrorState`, accessibility
  test tooling (`vitest-axe`), and React Query are all already in place with zero new foundational
  work needed.

**Missing / stubbed:**
- No public, by-UserId profile *read* endpoint exists in `Tutors` (only self-service `/me*`
  endpoints exist).
- `Reviews` module is a pure stub (no entities, no endpoints) — rating/review count/list are
  unavailable product-wide, not just on this page.
- `Messaging` module is a pure stub — "Message" CTA has nothing to call yet.
- `Students.FavoriteTutor` capability doesn't exist yet (ownership decided, not built).
- No trial-eligibility read exists in `Bookings` (needed to decide the CTA's exact label/state).
- No tutor can currently reach `Published` status via any HTTP path (`OPEN_QUESTIONS.md` A35 — no
  `Admin` role in `Identity` yet) — a pre-existing sequencing blocker this feature does not need
  to solve, but cannot ignore either (see §8).
- `TimeSlotPicker`/`DatePicker`/`BookingPanel`/`IntroVideoPlayer`/`RatingStars` are named in
  `PREPLY_COMPONENT_INVENTORY.md` as target components but **none exist yet** in
  `apps/web/src/components` — this feature is the first to actually build several of them.
- No media/photo/video hosting convention or ADR exists yet — `ProfilePhotoReference`/
  `IntroVideoReference` are opaque strings today with no documented resolution-to-URL mechanism.

---

## 5. Human-friendly URL evaluation

**Recommendation: keep `/tutors/{tutorUserId}` (Guid), do not add a slug.**

Reasoning: Preply itself — the product this spec benchmarks — uses a bare numeric tutor ID in the
URL path and has never added a vanity/slug segment despite being a mature, heavily-SEO-optimized
marketplace (§1.1). The CLAUDE.md/memory "human-friendly UX" rule is about not making a user
*type or understand* an ID as part of completing a task (forms, support requests, manual
navigation) — it is not violated by an opaque ID that users only ever reach by clicking a link,
which is the actual Preply pattern being matched here. A Guid is also strictly better than
Preply's sequential integer for not leaking tutor-count/enumeration information. `ADR-0011`
already fixes `UserId` as the one cross-module tutor identity, and the frontend route param
(`[tutorUserId]`) already matches it — changing to a slug would require a brand-new identity
concept (slug generation, uniqueness, collision/rename handling, redirect-on-rename) solely for a
cosmetic URL improvement Preply's own product doesn't bother with. **Bucket C (§12)**: revisit
only if real SEO data later shows a concrete ranking/CTR reason to add one; not required for
parity and not proposed now.

---

## 6. Required domain / API / read-model / frontend changes

### Domain changes
1. **`Tutors`**: add a public read surface. No new aggregate — same `TutorApplication`, filtered
   to `Published` status, mapped to a public-safe DTO.
2. **`Tutors`** (future, Bucket B/C, not this phase): a "temporarily not accepting new students"
   toggle distinct from the full Publish/Unpublish lifecycle, to match Preply's observed
   CTA-swap behavior (§1.5). Binary Published/not is all that exists today.
3. **`Students`**: implement the already-ownership-decided `FavoriteTutor` capability (currently
   just a named stub in the search parity matrix). Should publish a `TutorFavorited` integration
   event so `Tutors`/`Notifications` can eventually react (mirrors Preply's tutor-notified-on-
   favorite mechanic, §1.5) — the event is a cheap now-or-later decision; the entity/endpoint is
   the real work.
4. **`Bookings`**: add a trial-eligibility read (has-this-student-used-their-one-trial-with-this-
   tutor) — small, additive, no new aggregate.

### API changes
1. **NEW** `GET /api/tutors/{userId}` (public, no auth required) — `Tutors` module. 404 unless
   `Published`. Returns `TutorPublicProfileResponse` (see DTO shape in §4's "missing" list —
   mirrors existing `TutorApplication` fields minus internal-only ones).
2. **MODIFY** `GET /api/availability/tutors/{tutorUserId}/slots` — currently `.RequireAuthorization()`
   unconditionally. Needs an explicit decision (open question, §8) on whether/how a Guest-visible
   preview variant is added, vs. keeping it fully gated and only showing a static "has upcoming
   availability" boolean (already present on the `Search` read model, just unwired) to Guests.
3. **NEW** `GET /api/bookings/me/trial-eligibility/{tutorUserId}` — `Bookings` module, authenticated
   students only.
4. **NEW** (Bucket B) `POST`/`DELETE /api/students/me/favorites/{tutorUserId}` — `Students` module.
5. **Deferred** (Bucket C, blocked on `Reviews` module existing at all) `GET /api/reviews/tutors/{userId}`.
6. **Deferred** (Bucket C, blocked on `Messaging` module existing at all) message-compose endpoint.

### Read-model changes
- None to `Search`'s existing denormalized read model — Tutor Profile reads `Tutors` directly
  (it's the source of truth for profile content), not through `Search`'s projection. This keeps
  `Search`'s read model scoped to *browsing/ranking* and `Tutors`' own endpoint scoped to *detail*,
  avoiding duplicated ownership of the same fields (per the single-source-of-truth principle
  already applied elsewhere in this codebase's ADRs).

### Frontend changes
See the component list in §2's matrix and the states breakdown in §7.

---

## 7. Frontend plan

**New components** (all under `apps/web/src/features/tutor-profile/` or similar, following the
existing `features/search/` folder shape):
`TutorProfileHero`, `TutorTrustBadges` (shared atom — also retrofits `TutorResultCard` to use it
instead of its current inline badge rendering, per the "don't duplicate a rendering decision"
principle — existing component stays working throughout, this is additive extraction, not a
replace-and-delete), `BookingPanel` (price + Book Trial + Message + Favorite, sticky on desktop
per `PREPLY_COMPONENT_INVENTORY.md`'s own prior note, becomes a bottom-anchored bar on mobile),
`TutorIntroVideo` (conditional — omitted entirely when no video, not an empty state),
`TutorAbout`, `TutorSubjectsAndSpecialties` (reuses `useCatalog()`), `TutorLanguages`,
`TutorCredentials` (combines experience + education + certificates — Preply shows these as one
"resume" block, so does this), `TutorReviews` (empty-state now), `TutorAvailability`
(`DatePicker` + `TimeSlotPicker` — first real build of these, named but not yet built per the
component inventory), `SimilarTutors` (wraps the *existing* `TutorResultCard`, no new card).

**Explicitly not building**: a separate `TutorTrustSignals` top-level section (folded into the
hero + badges atom instead — Preply doesn't render trust signals as their own heavy section
either, they're inline near the name).

**States required per `UX_STATES_PRINCIPLES.md`'s 7-state policy:**
- *Loading*: section-level `Skeleton`s (hero, booking panel, each content block independently —
  not one page-wide spinner, so fast-loading sections render as soon as ready).
- *Empty*: reviews ("No reviews yet"), certificates/education (hidden sub-section if empty, not a
  placeholder — matches how Preply simply omits a video when absent), similar-tutors (hide
  section entirely if the query returns nothing).
- *Error*: per-section `ErrorState` for independent fetch failures (e.g. availability fails to
  load but the rest of the profile still renders) — partial degradation, not a full-page crash.
- *Not-found*: whole-page 404 (invalid `userId`, or any non-`Published` status) using Next.js
  `notFound()`, with a link back to search — matches `UX_STATES_PRINCIPLES.md` and keeps internal
  pipeline states (Draft/Submitted/Approved/Rejected) unexposed.
- *Unavailable*: Bucket C — not buildable this phase since the domain has no such state yet (§6).
- *CTA states*: Guest (Message/Favorite/Book-Trial prompt login — exact Book-Trial guest behavior
  is pre-existing `OPEN_QUESTIONS.md` #5, not re-decided here), authenticated Student
  (favorited/not-favorited toggle, trial-available vs. trial-used label), Tutor-role viewer
  (hide booking/message/favorite CTAs entirely — a tutor viewing any public profile, including
  conceivably their own, isn't a booking actor).
- *Accessibility*: landmark/heading-order structure, focus management for the date/time picker
  (flagged in `ACCESSIBILITY_REQUIREMENTS.md` as the highest-risk custom component platform-wide),
  `prefers-reduced-motion` respected in the similar-tutors carousel, `vitest-axe` coverage matching
  the existing `features/search/accessibility.test.tsx` convention.
- *Responsive*: desktop sidebar-panel layout vs. mobile bottom-anchored action bar (§1.17 — our own
  reasonable pattern, not a verified Preply copy, documented as such).

---

## 8. API / read-model loading plan

Primary recommendation (see §3's Kit-reuse table for the full justification): **parallel frontend
composition, no new BFF/aggregator endpoint.**

```
GET /api/tutors/{userId}                              → profile core (hero, about, badges, price, IDs)
GET /api/subjects, /api/specialties, /api/languages    → already fetched/cached via useCatalog()
GET /api/availability/tutors/{userId}/slots            → existing, auth rule TBD (open question)
GET /api/bookings/me/trial-eligibility/{tutorUserId}    → new, authenticated only
GET /api/search/tutors?subjectId=...&excludeTutorUserId=...  → similar tutors (existing endpoint, new query param)
[future] GET /api/reviews/tutors/{userId}
[future] favorite state (existing session + new favorites endpoint)
```

This avoids both failure modes named in the task: no god endpoint (each module still owns and
serves its own slice), and no N+1 waterfall (`useCatalog()` is already a one-time cached fetch
shared across the whole app, not a per-profile-view fetch; the remaining calls are independent and
parallelizable, not chained). Output caching (§3) keeps the highest-traffic call (`/api/tutors/
{userId}`) cheap under load without needing a new aggregation layer.

**`excludeTutorUserId` note**: `Search`'s query currently has no "exclude this tutor" parameter;
adding one is a small, additive change to an already-existing endpoint, not a new module
capability.

---

## 9. SEO plan

Appropriate for this stage (not over-building):
- **Canonical URL**: `https://<host>/tutors/{tutorUserId}`, self-referential canonical tag (no
  slug variants to canonicalize against, per §5).
- **Metadata**: Next.js `generateMetadata` on the existing page file — dynamic `<title>`
  ("{DisplayName} — {Headline} | TutorFlow"), meta description from `About` (truncated), Open
  Graph (`og:image` = profile photo, `og:title`, `og:description`).
- **Structured data**: minimal `schema.org` `Person` JSON-LD (name, image, jobTitle). Include
  `AggregateRating` **only when `ReviewCount > 0`** (Bucket C, waits on `Reviews`) — deliberately
  not emitting a zero/empty aggregate, which would be misleading structured data and a real SEO
  anti-pattern, not just an aesthetic choice.
- **Indexability**: `Published` profiles indexable; the 404/not-found response for anything else
  naturally returns a non-200 status, which is itself correct `noindex`-equivalent behavior — no
  extra robots meta tag logic needed beyond returning the right HTTP status.
- **Crawlability**: breadcrumb and similar-tutors links are real `<a href>` navigation (already
  the plan in §7), not JS-only client-side-only links, so they're crawlable.
- **Image optimization**: Next.js `<Image>` component — blocked on the open question of where/how
  `ProfilePhotoReference` resolves to an actual servable URL (§4, §8 open questions) — can't fully
  spec this until that's answered.

---

## 10. Testing plan

**Backend** (xUnit v3 + `WebApplicationFactory` + Testcontainers, matching existing `Bookings`/
`Availability` test conventions):
- `GET /api/tutors/{userId}` — found (`Published`) → 200 with correct shape, excluding internal
  fields.
- Not found (no such `userId`) → 404.
- Found but `Draft`/`Submitted`/`Approved`/`Rejected` → 404 (same as not-found — explicit
  regression test that internal status is never leaked).
- `GET /api/bookings/me/trial-eligibility/{tutorUserId}` — trial available / trial already used /
  unauthenticated → 401.
- Module boundary: `get_project_graph`/`find_references` via `cwm-roslyn-navigator` confirms the
  new `Tutors` endpoint doesn't reference `Reviews`/`Availability`/`Bookings`/`Students` DbContexts
  directly.
- Output-cache invalidation: profile edit → cache entry for that `userId` is evicted (integration
  test hitting the real cache, not mocked, per this project's own testing-philosophy precedent of
  preferring real integration behavior over mocks where it matters).

**Frontend** (Vitest + Testing Library + `vitest-axe`, matching `features/search/` convention):
- Full-data render (all sections present).
- Loading skeletons per section.
- Empty states: reviews, absent video, absent certificates, empty similar-tutors.
- Error state: one section's fetch fails, rest of page still renders.
- Not-found page render + link back to search.
- CTA behavior: favorite optimistic toggle, message button disabled state + tooltip copy, book-
  trial button navigates into the existing booking flow, trial-used label swap.
- Guest vs. authenticated-Student vs. Tutor-role viewer CTA visibility matrix.
- Accessibility: `vitest-axe` pass on the full page and on the date/time picker in isolation
  (highest-risk component per `ACCESSIBILITY_REQUIREMENTS.md`).
- Responsive: documented as a **manual QA checklist item** for this phase — Vitest/RTL can't
  verify real viewport/sticky-positioning behavior; flagging explicitly rather than claiming
  automated coverage that doesn't exist (per this project's own "don't claim UI success without
  browser verification" standard).

---

## 11. Intentional deviations from Preply

1. **Badges are static, admin-set flags**, not Preply's dynamic trailing-90-day/9-metric
   computation — no metrics pipeline exists yet; deferred (Bucket C).
2. **No tutor self-service "temporarily hide profile" toggle** — binary Published/not only;
   deferred (Bucket B/C depending on Product Owner priority).
3. **Guid, not sequential-integer, in the URL, and no slug at all** — intentionally matches
   Preply's own "no slug" pattern while improving on its enumerable sequential ID (§5).
4. **Reviews section ships empty** — no review data pipeline exists product-wide yet, not specific
   to this page.
5. **Message CTA ships disabled/placeholder** — `Messaging` module is a stub.
6. **Not-Published states are uniformly 404'd** rather than Preply's partially-observed
   "profile renders, CTA swaps" behavior for the one "not accepting students" case — TutorFlow
   doesn't yet have that intermediate domain state, so it's stricter (hide entirely) rather than
   guessing at a visual treatment for a state that doesn't exist.
7. **"Not a match? N free trials" and similar Preply marketing microcopy is not copied** — any
   reassurance copy TutorFlow adds will be written fresh.
8. **No multi-currency/multi-locale** — Preply operates many currencies/locales; TutorFlow's
   current scope is single-currency/locale and this feature doesn't change that.
9. **Experience/education/certificates combined into one `TutorCredentials` component** rather
   than Preply's looser narrative mixing — a presentation simplification, not a data/logic loss
   (all the same fields are shown).

---

## 12. Proposed implementation plan (buckets — nothing below is authorized to start)

**Bucket A — Ready to implement once this spec is approved (no open decision blocking):**
- `GET /api/tutors/{userId}` (public, Published-gated, output-cached).
- `TutorProfileHero`, `TutorTrustBadges` (+ retrofit `TutorResultCard`), `TutorAbout`,
  `TutorSubjectsAndSpecialties`, `TutorLanguages`, `TutorCredentials`, `TutorIntroVideo`.
- `TutorReviews` as a permanent-for-now empty state.
- `SimilarTutors` via existing `Search` endpoint + existing `TutorResultCard` (needs the small
  additive `excludeTutorUserId` query param).
- Not-found / loading / per-section error states.
- SEO basics: `generateMetadata`, canonical, Open Graph, `Person` JSON-LD (without
  `AggregateRating` until Reviews exists).
- Backend + frontend tests per §10 for everything above.

**Bucket B — Waiting on a product/architecture decision (this spec's open questions, §13):**
- Final shape of `TutorAvailability`/`BookingPanel` (depends on the Guest-availability-visibility
  decision).
- `Students.FavoriteTutor` endpoint + `BookingPanel` favorite wiring.
- `Bookings` trial-eligibility endpoint + CTA label logic.
- Message CTA's exact disabled-state copy/behavior for this phase.
- Structured-data scope sign-off (already conservative by design, but worth explicit confirmation
  per the task's "only propose what's appropriate for current stage" instruction).
- Image/video hosting resolution mechanism (blocks final `<Image>`/video-player implementation
  details, not the component structure itself).

**Bucket C — Long-term / explicitly out of scope for this phase:**
- Dynamic Super Tutor/Professional Tutor eligibility computation.
- Tutor self-service temporary profile hiding / "not accepting students" domain state.
- Multi-currency/multi-locale.
- Any slug/vanity-URL work (explicitly rejected for now per §5).
- Real reviews data pipeline, real messaging compose flow (both depend on their owning modules'
  first real feature work, which is larger than this page).

---

## 13. Open product/architecture questions

1. **Guest availability visibility** — does an unauthenticated visitor see a real (if limited)
   slot picker, a coarse "has upcoming availability" signal only, or nothing until login? Current
   backend (`GetTutorSlots`) requires auth unconditionally; `STUDENT_JOURNEY.md`/
   `ROLES_AND_PERMISSIONS.md` imply Guests get *some* availability preview. Needs an explicit
   Product Owner call.
2. **Messaging CTA behavior for this phase** — fully disabled with a tooltip, or some minimal
   placeholder compose UI, while `Messaging` remains unbuilt?
3. **Structured data sign-off** — confirm the conservative `Person`-only (+conditional
   `AggregateRating`) scope in §9 is sufficient for now.
4. **Image/video hosting** — where do `ProfilePhotoReference`/`IntroVideoReference` actually
   resolve to a servable URL today? No ADR or media/blob-storage convention was found in the
   audit — this is a real, pre-existing gap this feature surfaces but doesn't need to solve alone.
5. **Sequencing**: `OPEN_QUESTIONS.md` A35 (no `Admin` role → `Tutors.Publish()` unreachable via
   HTTP) means there is currently no way to produce a real `Published` tutor through the product
   itself to test this page end-to-end outside of direct test/DB seeding. Not this feature's
   problem to fix, but worth flagging to the Product Owner as a dependency this feature will hit
   immediately.
6. **Pre-existing, not re-opened here**: `OPEN_QUESTIONS.md` #5 (can a Guest start a trial booking
   and be prompted to sign up mid-flow, or must signup precede booking) directly determines the
   Book-Trial CTA's guest-state behavior on this page — cross-referenced, not re-decided.

---

*This document does not authorize any implementation. Awaiting approval before any code,
migration, or endpoint is written.*
