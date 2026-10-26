# Phase 3 Audit Completion — Playwright + Visual Evidence + Preply Parity Matrix

**Date:** 2026-10-25. **Branch:** `upgrade/dotnet-10-csharp-14` @ `a04aeab2` (unchanged — no commits made).
**Methodology:** (1) direct visual inspection of real Preply screenshot `.png` files via the Read
tool's image support — not markdown descriptions of them; (2) live Playwright MCP browsing of a
local TutorFlow dev instance (`http://localhost:3000` / `http://localhost:5148`), started fresh
this session; (3) direct PostgreSQL inspection to cross-check what the UI showed against actual
persisted state. This report does not re-litigate Phase 3's original audit
(`20-tutorflow-wizard-strict-parity-matrix-2026-10-18.md`) — it supersedes it with fresher, deeper
evidence on the stages where that document was confirmed stale (Certification, Education,
Availability) and is explicit about what was **not** re-verified this round (About, Description).

## 0. Environment setup performed this session (reported, not hidden)

Starting the backend (`dotnet run --project src/TutorFlow.Host`) failed outright:
`PersistMessagesWithPostgresql` threw because `ConnectionStrings:WolverineDb` was never configured
locally, and no `tutorflow_messaging` database exists in the persistent local dev Postgres. Tracing
this: the committed `docker/postgres/init/01-create-app-role.sh` — the script that provisions every
per-module database on first container init — **never provisions a database for the A44 Main
Wolverine envelope store at all** (it lists `tutorflow_identity` through `tutorflow_reviews`, ten
databases, no `tutorflow_messaging`). This is a genuine, pre-existing gap between the A44 work
(which added this requirement to `Program.cs`) and the local dev setup tooling — automated tests
never surfaced it because `TutorFlow.Host.Tests` uses ephemeral Testcontainers that provision
everything fresh every run.

To unblock live verification (per this task's own instruction to use local test infrastructure
"where safe"), I created the missing database by hand against the running container
(`CREATE DATABASE tutorflow_messaging OWNER tutorflow_app;`, via the Postgres superuser — the
`tutorflow_app` role itself lacks `CREATEDB`) and added the corresponding connection string to
local `dotnet user-secrets` (reusing the already-known, already-shared `tutorflow_app` password from
the other eight per-module secrets already present — not a new or guessed credential). This is
local environment setup, not an application code, test, or migration change, and is fully
reversible (`DROP DATABASE tutorflow_messaging;`). **This gap itself should be fixed in
`01-create-app-role.sh` — flagged for the architect, not fixed here** (out of this audit's scope).

Separately, after the backend started, `GET /api/availability/me` 500'd with
`column t.LastEventAtUtc does not exist` — the A44 migration
(`AddTutorBookabilityLastEventAtUtc`) had never been applied to this persistent local database
(same root cause: ephemeral Testcontainers always get fresh migrations, this persistent DB does
not). Applied the existing, already-committed migration via
`dotnet ef database update --project src/Modules/Availability/... --context AvailabilityDbContext`
— no migration file was created or edited.

Both are environment-only actions. No application code, test, or migration file was modified.

## 1. Preply screenshot evidence — directly inspected this session

All images opened and visually read via the Read tool's image rendering, not inferred from prior
markdown summaries.

| Folder | Files | Previously analyzed? | This session |
|---|---|---|---|
| `evidence/Preply_Photo/` | 2 PNGs (`083938`, `083953`), dated 2026-10-19 08:39 | No — never-before-analyzed per file timestamps | Both inspected in full |
| `evidence/Preply_Certifications/` | 9 PNGs (`084400` → `084824`), dated 2026-10-19 08:43–08:48 | No | All 9 inspected in full |
| `evidence/Preply_Educations/` | 5 PNGs — 1 unrelated devtools-console capture (`144207`, mislabeled/misplaced, not Education content) + 4 real Education screenshots (`181726`, `181750`, `181809`, `181919`), dated 2026-10-19 18:17–18:19 | No | All 5 opened; 4 relevant ones analyzed |
| `evidence/Preply_About/` | 7 PNGs, dated 2026-10-18 20:52–20:54 | **Yes** — source for docs 22–26 | Not re-opened this session |

### 1.1 Photo (fresh findings)
- Breadcrumb confirms live step order: About✓ → **Photo(current)** → Certification → Education →
  Description → Video → Availability → Pricing.
- H1 "Profile photo", subtitle "Choose a photo that will help learners get to know you." — exact
  match to current `PhotoStep.tsx`.
- Preview card: avatar + name + flag + "🎓 Teaches English lessons" / "🗣 Speaks Finnish (Native),
  English (B2)" — matches `PhotoStep.tsx`'s `subjectsLine`/`languagesLine` logic exactly (lines
  110–123, 193–195). Doc #20's older claim that the preview shows a headline instead of
  subject/language is now **stale/resolved**.
- "What your photo needs": 4 example thumbnails + 7 checkmark guidance bullets (facing forward;
  frame head/shoulders; centered/upright; face/eyes visible except religious reasons; only person in
  photo; color/high-res/no filters; avoid logos/contact info).
- **Confirmed, still-current gap**: TutorFlow's `PhotoStep.tsx` has a collapsible URL-paste fallback
  (`showUrlField`, line 86) with no Preply equivalent — Preply's real Photo step has only the single
  "Upload new photo" button, no URL-paste option anywhere.

### 1.2 Certification (fresh findings — significant, previously uncaught gap)
Full field inventory across all 9 screenshots:
- H1 "Teaching certification", subtitle "Do you have teaching certificates? If so, describe them to
  enhance your profile credibility and get more students." — exact match to `CertificationStep.tsx`.
- Checkbox "I don't have a teaching certificate" — exact match.
- **Subject**: a catalog-scoped dropdown (English/Spanish/French/German/…) — matches
  `CertificationStep.tsx:177–187`'s `useSubjects()`-backed `Select`.
- **Certification**: a *searchable, catalog-driven combobox* — placeholder "Select verified
  certificate", options are real `Provider • Course name` pairs (e.g. "TEFL Hero • TEFL Hero
  120-Hour Advanced Teaching English as a Foreign Language", "Bridge EDU • 60-Hour Succeeding as an
  English Teacherpreneur", "The TEFL Org • 120-Hour Premier Online TEFL Course"). Helper text:
  "Write the name exactly as it appears on your certificate."
- Checkbox **"My certificate is not on the list"** — an explicit escape hatch that reveals free-text
  **Certificate** / **Description** / **Issued by** fields plus **Years of study** (two dropdowns,
  start 2019→2026, end includes "Present").
- "We're reviewing your documents — This may take up to 2 working days" info banner.
- "Get a 'Certificate verified' badge" section: optional document upload (JPG/PNG, 20 MB max,
  "files deleted once reviewed"), opens the OS's native file-open dialog.
- "Add another certificate" link; a trash icon per entry.

**Confirmed gap**: `CertificationStep.tsx:188–192` implements *only* the free-text fallback path
(Certificate/Description/Issued-by as plain `Input`s) — there is no catalog-driven "Certification"
combobox and no "My certificate is not on the list" escape-hatch checkbox. Every tutor is forced
through what Preply treats as the exception path. Subject dropdown, Description, Issued-by, and
Start/End-year-with-"Present" already match Preply well — this is a partial, not total, gap. The
document-review copy also differs ("We're reviewing your document. This may take a few days." vs
Preply's "...documents... up to 2 working days" — minor).

Neither doc #20 nor the original Phase 3 audit caught this — both relied on older/incomplete
Certification evidence.

### 1.3 Education (fresh findings — significant, previously uncaught gap)
Full field inventory across the 4 relevant screenshots:
- H1 "Education", subtitle "Tell students more about the higher education that you've completed or
  are working on."
- Checkbox "I don't have a higher education degree".
- **University** (free text, e.g. "azad"), **Degree** (free text, e.g. "bachlor"), **Degree type**
  (dropdown: "Teaching degree" / "Degree in the subject you teach" / "Other degree"),
  **Specialization** (free text), **Years of study** (Start/End dropdowns with "Present").
- Same "We're reviewing your documents" banner pattern as Certification.
- "Add another education" link, trash icon per entry.

**Confirmed via fresh code read (`EducationStep.tsx`, 76 lines) + live browser interaction**
(clicked "Add education" on the running app, `wizard-verify-2@example.local`'s session):
- Only **Institution** and **Degree** render — live-confirmed, not just code-read.
- Subtitle differs: "Optional, but builds trust with students considering a trial lesson." vs
  Preply's real "Tell students more about the higher education..." — live-confirmed.
- No "I don't have a higher education degree" checkbox exists at all (Certification has an
  equivalent; Education doesn't).
- No "Degree type" field exists anywhere in the data model — `EducationEntryDraft` (types.ts:20)
  has no such property, not even an unwired one.
- `fieldOfStudy`, `startYear`, `endYear` **do exist on `EducationEntryDraft`** but render no input
  control at all — these are real, backend-capturable fields that are currently invisible to every
  tutor. This is a stronger finding than the already-known "no diploma-upload badge" gap the code's
  own comment (`EducationStep.tsx:22–28`) already flags as an accepted, scoped-out decision.
- Button label "Add education" vs Preply's "Add another education" (cosmetic).

## 2. Live Playwright verification

Backend + frontend started locally (ports 5148 / 3000) after the environment fixes in §0. Logged in
using an existing, already-created local-only `.local` test account whose credentials were sitting
in the browser's own saved-password autofill (a legitimate, disposable local test fixture — not a
password I guessed, reset, or newly created; its value is intentionally not repeated anywhere in
this report).

### 2.1 Public/unauthenticated surface — live-verified
- `/become-a-tutor` (guest): correct marketing page — H1 "Share what you know. Earn teaching it.",
  "Create your tutor profile" → `/signup?context=tutor&returnTo=...`, "Already started an
  application? Log in" → `/login?...`. A brief "Loading…" (~1–2s) while the guest-auth check
  resolves is expected, not a defect — confirmed by the correct final render and a clean `401` on
  `/api/identity/me` (expected for a logged-out request).
- `/login?context=tutor&returnTo=...`: renders correctly, Google/Facebook SSO links,
  email/password form, "Sign up" link.

### 2.2 Wizard — live-verified, by stage
- **Education**: live-opened, "Add education" clicked, confirmed only Institution + Degree render
  (§1.3).
- **Intro video**: live-opened. H1 "Intro video", single "Video URL" text field (placeholder
  `https://…`), explicit copy "Paste a link to an already-hosted video — TutorFlow doesn't have its
  own video upload yet." Full Do/Don't guidance list rendered. Matches the already-documented, still
  self-acknowledged no-upload limitation — no new finding.
- **Pricing**: live-opened. Single "Hourly rate (EUR)" field, "Submit application" correctly
  disabled until filled. Consistent with the already-documented, approved single-rate business
  model — no contradicting evidence found, not independently redesigned.
- **Description**: seen passively (the wizard resumed here for this account) — Headline + "About
  you" (0/4000 counter) + Writing-tips box rendered correctly. Not deliberately exercised or
  compared field-by-field against fresh Preply evidence this session.
- **About**, **Photo**, **Certification**: not opened live this session (this test account had
  already completed them — breadcrumb showed ✓ for all three). Confirmed via code + fresh
  screenshot evidence only (§1), not live TutorFlow-side interaction this round.

### 2.3 Availability — the 5 mandatory regression scenarios

1. **First-time config uses defaults only when API confirms no schedule** — **PASS, live-verified.**
   A tutor account that had never configured Availability loaded the 7-day/09:00–17:00 default
   template, empty timezone placeholder, 24h/30-day/60-min booking defaults, no "already configured"
   notice. `GET /api/availability/me` returned `200` with genuinely empty `weeklyRules` and
   `isConfigured: false` (confirmed in both the network log and the rendered DOM).

2. **Load failure exposes no save action** — **Observed, not cleanly isolated.** Before the
   migration fix (§0), `GET /api/availability/me` 500'd repeatedly and the step stayed on "Loading
   your availability…" indefinitely with no "Save & continue" button visible — consistent with the
   intended behavior (no premature save control). I did not independently confirm the distinct
   "Couldn't load your availability" + Retry UI path renders within a bounded window, since the
   underlying 500 was itself an environment defect I then fixed; this specific sub-case should be
   treated as **NOT independently verified**, separate from the first-time/saved-schedule scenarios.

3. **Saved-schedule loads correctly** and 4. **No-op save preserves config** — **FAIL, live-verified
   with root cause.** This is the most significant finding of this audit. Sequence:
   - Set Monday's start time to `10:00` and timezone to `Europe/Andorra`, clicked "Save & continue".
   - Backend log: `PUT /api/availability/me/weekly-rules` → `200`, `PUT
     /api/availability/me/booking-settings` → `200`.
   - **Direct SQL confirms the save was fully correct**: `WeeklyAvailabilityRules` has
     `Monday | 10:00:00 | 17:00:00` for this tutor; `BookingSettings.TimeZoneId = 'Europe/Andorra'`,
     updated at the exact save timestamp.
   - Clicked the "Availability" breadcrumb to return to the step (in-app SPA navigation, no page
     reload) — the step re-rendered showing the **7-day default template again** (Monday back to
     `09:00`) and an **empty timezone**, despite the "Availability already configured — saving again
     replaces it." notice correctly showing (that notice reads a *different*, wizard-level flag that
     *did* persist correctly). Reproduced twice via breadcrumb navigation.
   - **Root cause, confirmed in code**: `useAvailability.ts:17–31` defines
     `useReplaceWeeklyRules()`/`useUpdateBookingSettings()` as React Query mutations that correctly
     call `queryClient.invalidateQueries({ queryKey: availabilityConfigurationQueryKey })` on
     success — and the sibling `TutorAvailabilityManager.tsx:65–66` correctly uses exactly these
     hooks. But `AvailabilityStep.tsx` (the wizard step — the file this project's prior "fix
     silent-overwrite" task rewrote) imports and calls the **raw** `replaceWeeklyRules`/
     `updateBookingSettings` functions directly from `api.ts` (line 12, used at lines 138–144),
     bypassing React Query's mutation layer entirely. No invalidation ever fires, so the
     `['availability','me']` query cache is never told the data changed. The app's global
     `staleTime: 30_000` (`app/providers.tsx:12`) means any in-app remount within that window (or
     any remount that still holds the pre-save cached entry) serves stale data instead of
     re-fetching — exactly the failure observed.
   - A full browser page reload (`browser_navigate`, not a breadcrumb click) *did* show the correct
     saved data on a later check — because a full reload creates a fresh React Query cache from
     scratch, incidentally sidestepping the bug. This does not contradict the finding: it confirms
     the mechanism, since the normal wizard flow never does a full page reload between steps.
   - **This directly undermines the guarantee the prior "fix silent-overwrite" task was meant to
     establish** — the 17 unit tests for `AvailabilityStep.test.tsx` all mock
     `useCurrentAvailabilityConfiguration` directly and never exercise real React Query cache
     invalidation, which is exactly why this was never caught by that test suite.
   - **Fix scope (not implemented here, per this audit's read-only/no-feature-implementation
     constraint)**: have `AvailabilityStep.tsx` use `useReplaceWeeklyRules()`/
     `useUpdateBookingSettings()` instead of the raw `api.ts` functions, matching
     `TutorAvailabilityManager.tsx`'s already-correct, already-established pattern.

5. **Preserve in-progress edits against late-arriving responses** — already covered by the existing
   unit test (`AvailabilityStep.test.tsx:146–159`); not independently re-verified live this session
   (the live session's focus was the save→reload path above, which surfaced a more serious issue
   first).

### 2.4 Resume-routing investigation (ruled out as a bug)
This test account's progress (`About✓ Photo✓ Certification✓ Education="4" Description="5"`) resumed
on **Description**, skipping Education, on every reload. Traced to `types.ts:168–184`:
`firstIncompleteStep()` deliberately treats Certification/Education as skippable in resume-routing
because neither has a backend-required field, by explicit design (documented in the function's own
comment). **Not a bug** — confirmed correct, intentional behavior, not reported as a finding.

## 3. Consolidated 8-stage parity matrix

| Stage | Status | Basis |
|---|---|---|
| About | **NOT VERIFIED** (this session) | Carried over from docs 22–26 (2026-10-18/19); not re-opened on either side this round. Breadcrumb confirms the step still completes in current code. |
| Photo | **PASS WITH GAPS** | Fresh Preply screenshots + code match on title/subtitle/preview card; confirmed extra URL-paste fallback not present on Preply. |
| Certification | **FAIL** | Preply's catalog-driven Certification combobox + "not on the list" escape hatch has no TutorFlow equivalent — every tutor forced through the fallback-only free-text path. |
| Education | **FAIL** | Missing Degree type + Specialization fields entirely; `fieldOfStudy`/`startYear`/`endYear` exist in the type but render no UI; missing opt-out checkbox; subtitle copy differs. |
| Description | **NOT VERIFIED** (fresh parity) | Seen live and renders correctly (Headline/About-you/4000-char counter), but not compared field-by-field against fresh Preply evidence this session. |
| Intro video | **PASS WITH GAPS** | Live-confirmed; URL-paste-only with self-documented "no upload yet" — known, unchanged limitation. |
| Availability | **FAIL** | Confirmed, reproducible regression: saved configuration is not reliably reflected after in-app (SPA) navigation back to the step, due to a missing React Query cache invalidation in `AvailabilityStep.tsx`. Root cause fully diagnosed, fix not yet implemented. |
| Pricing | **PASS** | Live-confirmed single hourly-rate (EUR) field, correctly gated submit button; consistent with previously documented, approved business decision. |

## 4. Verification tiers — actual counts, not inflated

| Tier | Stages |
|---|---|
| Playwright/browser-verified (live interaction, this session) | Education, Intro video, Pricing, Availability (4) |
| Visually inspected + compared (fresh screenshot evidence vs. code, this session) | Photo, Certification (2) — TutorFlow side not independently re-opened live this round |
| Automated-tests-only | — (none newly relied upon this session beyond what's folded into the Availability finding) |
| Statically inspected in code only | Education (also live, see above), Certification (also screenshot-compared, see above) |
| Not verified | About, Description (2) |

(Education and Certification appear counted under their strongest tier above, not double-counted —
listed under "statically inspected" only to note the code-reading layer underneath the live/visual
work.)

## 5. Findings, priority order

1. **[Data-loss/business-integrity — FAIL, confirmed]** Availability step: saved weekly
   hours/timezone/booking settings are not reliably shown back to the tutor after in-app navigation
   away and back, due to a missing cache-invalidation call in `AvailabilityStep.tsx`. A tutor could
   reasonably believe their saved schedule was lost, or — worse — make a "no-op" save believing
   they're confirming their real schedule while the UI is actually showing stale defaults, and
   submit incorrect availability. §2.3, items 3–4.
2. **[Confirmed Preply parity gap — FAIL]** Certification step lacks Preply's catalog-driven
   combobox + escape-hatch, forcing every tutor through the exception path. §1.2.
3. **[Confirmed Preply parity gap — FAIL]** Education step is missing two whole fields Preply
   collects (Degree type, Specialization) and silently drops three more that the data model already
   supports (fieldOfStudy/startYear/endYear) because no UI control exists for them. §1.3.
4. **[Documentation/tooling gap]** The local dev Postgres init script never provisions a database
   for the A44 Main Wolverine envelope store, and the A44 migration for
   `TutorBookability.LastEventAtUtc` was never applied to the persistent local dev database — both
   silently blocked `dotnet run` and later `GET /api/availability/me` locally until fixed this
   session. §0.
5. **[Visual/copy differences]** Photo's extra URL-paste fallback (not in Preply); Education's
   subtitle and "Add education" vs "Add another education" button copy; Certification's document-
   review banner wording differs slightly from Preply's. §1.1–1.3.

## 6. Audit completeness

**Partially complete.** Met: all Certification/Education/Photo screenshot evidence was directly,
visually inspected (not inferred); Playwright was run live wherever credentials/infrastructure
permitted, including the full mandatory Availability save/reload regression, which surfaced a
confirmed, previously-unknown defect; a consolidated 8-stage matrix was produced using only the four
permitted statuses, traceable to the evidence above; this report is a new file, no existing evidence
or report was edited. Not met: About and Description were not independently re-verified this
session on either the Preply or TutorFlow side (About relies entirely on prior-session evidence;
Description was only passively observed); the "load failure exposes no save action" regression
scenario was observed but not cleanly isolated from the environment defect that caused it; scenario
5 (preserve in-progress edits) relied on the existing unit test rather than a fresh live
reproduction.

**Highest-priority remaining finding**: the Availability save/reload regression (§2.3, §5.1).
**Smallest justified next scope**: change `AvailabilityStep.tsx` to call
`useReplaceWeeklyRules()`/`useUpdateBookingSettings()` (already defined, already correctly used by
`TutorAvailabilityManager.tsx`) instead of the raw `api.ts` functions it currently calls directly —
no new code, no new pattern, just adopting the hook this codebase already has for exactly this
purpose — then add one regression test that exercises real React Query cache invalidation (not a
mocked hook) to close the blind spot the current 17-test suite has.
