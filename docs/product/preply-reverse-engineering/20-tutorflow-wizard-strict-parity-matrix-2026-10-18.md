# Strict Preply-vs-TutorFlow Parity Matrix — Tutor Onboarding Wizard

**Date:** 2026-10-18. **Supersedes-in-part:** `19-tutorflow-wizard-live-verification.md`'s conclusion
of "strong existing parity" — that conclusion was based on 4 of 8 steps and is not repeated here
as settled. This document re-verifies against all 8 steps and the existing `18-screenshot-evidence.md`
field-level record, which itself contained several **TutorFlow-side annotations that were stale**
relative to current code (corrected below, explicitly).

## 0. Tooling and access — verified, not assumed

- **Playwright MCP: confirmed working.** Used throughout this session (live navigation, snapshots,
  screenshots, form interaction) against both `preply.com` and the local TutorFlow instance.
- **Preply pages genuinely accessible this session (fresh, live):** `https://preply.com/en/teach`,
  `https://preply.com/en/teach/signup`, `https://preply.com/en/tutor-signup` (redirects anonymous
  visitors to `/en/teach` — re-confirmed again this session).
- **Preply pages NOT accessible (same reason every prior session):** the authenticated 8-step
  wizard itself. No account was created — explicit, standing project rule
  (`00-scope-and-methodology.md`: real-reviewer-time/ToS concern). Its content below is drawn
  **entirely from the existing `18-screenshot-evidence.md` pack** (13 real images, human-captured,
  SS01–SS13) — marked as such throughout, never presented as freshly observed.
- **TutorFlow pages: all 8 wizard steps freshly inspected live this session** (Basics, Photo,
  Teaching, Credentials, Video, Availability, Rate, Review), local dev instance, real test account
  (`wizard-parity-test@example.local`), desktop 1440×900 + one mobile spot-check (390×844, Photo
  step). Screenshots: `evidence/tutorflow-wizard-live-2026-10-18/`.

## 1. Strict parity matrix

| # | Stage | Preply expected behavior | Preply evidence | TutorFlow actual behavior | TutorFlow ref | Difference | Severity | Confidence | Recommended correction | Test needed |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | Entry boundary | Public `/en/teach`+`/teach/signup` = account creation only; full wizard authenticated-only at `/en/tutor-signup` | OBSERVED live (redirect confirmed) | Public `/become-a-tutor` = intro card only; full wizard authenticated-only, same route | `BecomeATutorView.tsx` | None | — | Verified (both sides, live) | None | Existing (`BecomeATutorView.test.tsx`) |
| 2 | Step count/order | 8 steps: About, Photo, Certification, Education, Description, Video, Availability, Pricing | OBSERVED (SS06, full breadcrumb) | 8 steps: Basics, Photo, Teaching, Credentials, Video, Availability, Rate, Review | `types.ts WIZARD_STEPS` | **Content grouping differs** (see #3, #4, #8) and TutorFlow has an 8th step (Review) Preply doesn't | Moderate | Verified (both sides) | **Product decision required** — not implemented here (same open item as `16-...md` #1, now doubly evidenced) | N/A until decided |
| 3 | Step 1 content | Name, email, country of birth, one subject, repeatable languages+proficiency — all on one step | OBSERVED (SS01) | Split: Basics (name/headline/about) is step 1; email collected at **registration**, not in wizard; Teaching (subjects/languages/country) is step **3** | `BasicsStep.tsx`, `TeachingStep.tsx` | Confirmed split, already known | Moderate | Verified | Product decision (#2 above) | N/A |
| 4 | Photo step preview card | Live card: avatar + name + country flag + "Teaches [subject]" + "Speaks [languages]" | OBSERVED (SS02) | Live card: avatar (initials fallback) + name + **headline** (no subject/language/flag — those aren't collected yet at step 2) | `PhotoStep.tsx` | Card exists (✅ **corrects stale "NOT VERIFIED" in `18-...md`**) but shows different fields, a direct consequence of #3's step-order difference | Minor | Verified (fresh live) | None standalone — resolves itself if #2 is ever decided | None needed now |
| 5 | Certification step | Separate step; controlled certificate dropdown; "not on the list" fallback; years-of-study range; file upload, "manually reviewed" copy | OBSERVED (SS03) | Merged into "Credentials"; **Certificate name + Issuing organization only** — no years field, no controlled list, no file upload | `CredentialsStep.tsx` (confirmed live, "Add certificate" form) | Real, current gap (not stale) | Major | Verified (fresh live) | Requires backend work (file storage, admin review workflow) — **not implemented, needs approval** | New backend+frontend tests once approved |
| 6 | Education step | Separate step; University/Degree/Degree type/Specialization/Years; "Diploma verified" badge (JPG/PNG, 20MB, reviewed-then-deleted) | OBSERVED (SS04, SS05) | Merged into "Credentials"; **Institution + Degree only** (confirmed live) — no degree-type, specialization, years, or badge | `CredentialsStep.tsx` | Real, current gap | Major | Verified (fresh live) | Same as #5 — backend-scale, needs approval | Same as #5 |
| 7 | Description/About | 4 numbered sub-prompts, soft non-blocking counter at 400 chars | OBSERVED (SS06) | "Writing tips" box, **4 items**, matches structurally; counter present, "0 / 4000" (10× Preply's limit) | `BasicsStep.tsx` (confirmed live) | **Corrects stale "missing" in `18-...md`** — sub-prompts + counter both already exist; only the numeric limit differs | Minor | Verified (fresh live) | 4000 is a **backend-enforced** limit (`UpdateDraft.Validator`) — changing it is a business-rule change, **flagging only, not changing without approval** | N/A until decided |
| 8 | Video — capture method | In-browser webcam recording (primary), OR paste YouTube/Vimeo link, OR (implied) file upload; AI "Create script" assist | OBSERVED (SS07–SS09) | **URL paste only** — no recording, no AI assist. Explicitly, honestly labeled in the UI itself: "TutorFlow doesn't have its own video upload yet" | `VideoStep.tsx` | Confirmed, already self-documented gap | Major | Verified (fresh live) | Requires real video infra/AI integration — **not implemented, needs approval**; honest labeling is correct behavior, keep it | N/A until decided |
| 9 | Video — Do/Don't list | 6 Do items (incl. stable surface, face/eyes visible), 4 Don't items (incl. logos/links) | OBSERVED (SS08, SS09) | **Before this task**: 4 Do / 3 Don't, missing "stable surface," "face visible," "logos/links" (**corrects stale "MISSING" in `18-...md`** — a Do/Don't panel already existed, just incomplete). **After this task**: 6 Do / 4 Don't, content-complete | `VideoStep.tsx` | **Fixed this session** | Minor | Verified (fresh live, before+after) | **Implemented** | `VideoStep.test.tsx` (+1 test, 9/9 pass) |
| 10 | Availability — timezone | Auto-detect browser timezone, Yes/No confirm | OBSERVED (SS10) | Plain required `<select>` dropdown, no auto-detect | `AvailabilityStep.tsx` (confirmed live) | Real, current UX-pattern gap | Moderate | Verified (fresh live) | Frontend-only, feasible (`Intl.DateTimeFormat().resolvedOptions().timeZone`) — **not implemented this pass, flagged as a good next candidate**, see §4 | New test once implemented |
| 11 | Availability — weekly default | All 7 days pre-enabled, 09:00–17:00, multi-timeslot via "Add another timeslot" | OBSERVED (SS11, SS12) | All 7 days pre-enabled, 09:00–17:00, "Add a time block" per day | `AvailabilityStep.tsx` (confirmed live, `DEFAULT_RULES`) | **Corrects stale "NOT VERIFIED" in `18-...md`** — matches | None | Verified (fresh live) | None | Existing (`AvailabilityStep.test.tsx`) |
| 12 | Pricing step | Single USD price per 50-min lesson; collapsible commission disclosure; "Corporate Tutor Program" opt-out notice; **no review step** — leads straight to "Complete registration" | OBSERVED (SS13) | Single EUR hourly rate (no commission UI, no corporate-program concept); **separate Review step exists** (Preply has none) | `RateStep.tsx`, `ReviewStep.tsx` (confirmed live) | EUR-vs-USD and hourly-vs-per-lesson are **already-decided** (`ADR-0004`, trial-pricing formula) business-model choices, not bugs. Commission/corporate-program UI is MISSING (blocked on Stripe Connect decision, `ADR-0018`/`ADR-0027`, pre-existing). Review step is the same open #2 item. | Moderate (review-step question) / none (currency) | Verified | Currency: none needed. Commission/corporate: blocked, needs product+Stripe decision. Review step: same #2. | N/A |
| 13 | Review/submit validation | N/A (no review step; "Complete registration" is the only action) | OBSERVED (SS13, no distinct review UI) | Live checklist of missing required fields, deep-linked to each step; **real `/submit` call** correctly rejected an incomplete application (verified via actual click, not inferred) | `ReviewStep.tsx`, `TutorApplication.GetSubmissionReadinessErrors()` | **Found and fixed this session**: checklist omitted the intro-video requirement that an earlier task added to the backend | — | Verified (fresh live, before+after) | **Implemented** (prior task today) | `ReviewStep.test.tsx` (4/4 pass) |
| 14 | Submit error detail | N/A (not observable — registration never completed on Preply) | NOT VERIFIED (Preply) | Generic `"One or more errors occurred."` shown — backend's specific per-field `errors` array (e.g. "An intro video is required…") is **not surfaced** to the user; `errorMessage()` reads `problem.detail`, which the backend never sets | `OnboardingWizard.tsx errorMessage()`, `ResultExtensions.ToProblemDetails()` | Pre-existing TutorFlow-internal gap, not a Preply-parity item (Preply's own behavior here is unknown) | Moderate | Verified (fresh live reproduction) | Shared utility used by 3 call sites — **cross-cutting, needs its own scoped approval**, not bundled here | New tests once scoped |
| 15 | Mobile — progress indicator | NOT VERIFIED (Preply evidence is desktop-only, `18-...md` explicitly states no mobile screenshots exist) | NOT VERIFIED | Wraps to 2 rows at 390px; fully correct at scroll-top (✓/current-circle + chevrons); a mid-transition screenshot briefly showed only row 2 — **investigated and confirmed to be a scroll-position artifact, not a bug** | `WizardProgress.tsx`/`.module.css` | No real defect found | — | Verified (fresh live, investigated) | None | None needed |
| 16 | Resume behavior | NOT VERIFIED (Preply) | NOT VERIFIED | Re-navigating to `/become-a-tutor` mid-application lands on the **first incomplete step**, not step 1 | `OnboardingWizard.tsx firstIncompleteStep()` | — | — | Verified (fresh live) | None | Existing coverage presumed, not re-audited line-by-line this pass |

## 2. Most significant differences, ranked

1. **Step content grouping** (#2, #3, #8, #12 review-step) — the single largest structural
   difference, already flagged in `16-product-decisions-and-open-questions.md` item 1 since before
   this session, now evidenced twice over. **Explicitly not resolved here** — restructuring the
   wizard's step boundaries is a product/architecture decision this task's own instructions require
   approval for before implementing, and doing so would touch routing, the `WIZARD_STEPS` type,
   multiple components, and the step-completion-derivation logic — not a "focused frontend" change.
2. **Credentials file upload / verified-badge mechanics** (#5, #6) — a real, current, major gap.
   Backend-scale (storage, admin review state), not implementable as a frontend-only change.
3. **Video capture method** (#8) — real, current, already self-documented honestly in the UI copy.
   Backend/infra-scale.
4. **Timezone auto-detect** (#10) — real, moderate, and genuinely frontend-only feasible, flagged
   as the best next candidate if you want one more small win without a product decision.
5. Several items the *existing* `18-screenshot-evidence.md` had marked "MISSING" or "NOT VERIFIED"
   for TutorFlow turned out to **already be implemented** once checked live (#4 preview card, #7
   sub-prompts/counter, #9 Do/Don't panel's existence, #11 default schedule) — the historical
   document's TutorFlow-side annotations were stale, not wrong about Preply.

## 3. Fixed this session

| File | Change | Why |
|---|---|---|
| `apps/web/src/features/tutor-onboarding/components/ReviewStep.tsx` | Added missing intro-video item to the client-side readiness checklist (prior task today) | Checklist was out of sync with a backend requirement added earlier this session |
| `apps/web/src/features/tutor-onboarding/components/ReviewStep.test.tsx` | Updated fixture + new assertion | Cover the fix |
| `apps/web/src/features/tutor-onboarding/components/VideoStep.tsx` | Added 2 Do items ("stable surface," "face visible") + 1 Don't item ("logos or links") | Preply's SS08/SS09 Do/Don't list has these; TutorFlow's existing panel didn't |
| `apps/web/src/features/tutor-onboarding/components/VideoStep.test.tsx` | New test asserting all 3 added items render | Cover the fix |

Both changes are copy/content-only — no backend contract, validation rule, or business logic
touched.

## 4. Left open, explicitly not implemented this session

| Item | Why not implemented now |
|---|---|
| Step regrouping to match Preply's exact 8-section boundaries, or dropping the Review step | Product/architecture decision required (task's own instruction) |
| Credentials file upload + manual-review + "verified" badge | Backend-scale (storage, admin workflow, new domain fields) |
| Video in-browser recording + AI script assist | Backend/infra-scale (storage, AI integration) |
| Timezone auto-detect-and-confirm | Frontend-only and feasible, but not implemented this pass — flagged as the best next small win |
| Commission disclosure / Corporate Tutor Program equivalent | Blocked on pre-existing Stripe Connect/commission decision (`ADR-0018`/`ADR-0027`) |
| Generic submit-error message (not surfacing per-field detail) | Cross-cutting shared-utility change (3 call sites), needs its own scoped approval |
| Profile-description 4000 vs. Preply's 400 char limit | Backend-enforced; changing it is a business-rule change |

## 5. Test and build results

- `npx vitest run src/features/tutor-onboarding` — **88/88 passed**, 10 files (87 pre-existing/from
  earlier today + 1 new this pass).
- `npx vitest run src/features/tutor-onboarding/components/VideoStep.test.tsx` — **9/9 passed**
  (isolated, confirming the new test specifically).
- `npx eslint src/features/tutor-onboarding` — **0 errors**, 1 pre-existing warning (`PhotoStep.tsx`
  `<img>` LCP advisory, unrelated to this change).
- No backend code was touched this session — no backend test run needed/performed.
- Both fixes re-verified live in the running UI (screenshots: `tutorflow-wizard-review-fixed.png`,
  `tutorflow-wizard-video-fixed.png`), not just via unit tests.

## 6. Evidence inventory summary

| Source | Steps covered | Fresh or historical |
|---|---|---|
| `18-screenshot-evidence.md` (SS01–SS13) | All 8 Preply steps | **Historical** (2026-10-06 capture) — relied on, not re-browsed (inaccessible) |
| This session, live Preply | `/en/teach`, `/en/teach/signup`, `/en/tutor-signup` (redirect only) | **Fresh** (2026-10-18) |
| This session, live TutorFlow | All 8 steps: Basics, Photo, Teaching, Credentials, Video, Availability, Rate, Review | **Fresh** (2026-10-18), desktop; 1 mobile spot-check |
| `19-tutorflow-wizard-live-verification.md` | Basics, Photo, Availability, Review (4 of 8) | Fresh, earlier today — superseded in coverage by this document |

## 7. Recommended next steps, in order

1. **Product decision**: resolve the step-grouping/review-step question (`16-...md` item 1) —
   blocks any further structural work.
2. If timezone auto-detect is wanted: small, frontend-only, no decision blocker — implementable
   next.
3. Scope and approve the Credentials file-upload/verified-badge feature as its own backend-involving
   task.
4. Scope and approve the Video recording/AI-assist feature as its own backend-involving task.
5. Scope the submit-error-detail surfacing fix (shared `errorMessage()` utility) as its own small
   cross-cutting task.
6. Commission/Corporate-program UI remains correctly blocked on the pre-existing Stripe Connect
   decision — no new action.

**Do not conclude "parity achieved."** Two small, real gaps were closed this session (video
guidelines, submit-checklist consistency); the two largest structural/content gaps (Credentials
depth, Video capture method) remain open by design — they require resources (storage, AI, admin
tooling) and approvals beyond this task's authorized scope, not an oversight.
