# Tutor Onboarding — Product Decision Brief

**Date:** 2026-10-18. **Status:** Decision brief only — no implementation. Supersedes nothing;
consolidates and sharpens `16-product-decisions-and-open-questions.md` item 1 and
`20-tutorflow-wizard-strict-parity-matrix-2026-10-18.md` into a single decision-ready document.
**Author note:** I am the implementation agent, not the product/architecture decision-maker. Every
"Recommended" label below is a recommendation only.

---

## 1. The exact 8-stage Preply sequence — evidence-backed

Source: `18-screenshot-evidence.md`, Part A (`evidence/tutor.zip`, SS01–SS13), a 13-image real
screenshot pack. The full, legible breadcrumb appears for the first time in **SS06** and is
corroborated incrementally through **SS13**:

| # | Stage (on-screen label) | Direct evidence |
|---|---|---|
| 1 | **About** | SS01 (fields), confirmed as step 1 by SS06's breadcrumb |
| 2 | **Photo** | SS02 (H1 "Profile photo"), SS06 breadcrumb |
| 3 | **Certification** | SS03 (H1 "Teaching certification"), SS06 breadcrumb |
| 4 | **Education** | SS04, SS05 (H1 "Education"), SS06 breadcrumb |
| 5 | **Description** | SS06 (H1 "Profile description", full breadcrumb: `☑ About › ☑ Photo › ☑ Certification › ☑ Education › 5 Description › 6 Video › 7 Availability › 8 Pricing`) |
| 6 | **Video** | SS07, SS08, SS09 (breadcrumb visible in SS07/SS08) |
| 7 | **Availability** | SS10, SS11, SS12 (breadcrumb visible in SS10) |
| 8 | **Pricing** | SS13 (H1 "Set your 50 minute lesson price", full breadcrumb: `☑ About › ☑ Photo › ☑ Certification › ☑ Education › ☑ Description › ☑ Video › ☑ Availability › 8 Pricing`) |

This is **OBSERVED**, the strongest evidence tier this project uses — not inferred, not documented
secondhand. The sequence is final and not in dispute.

## 2. Which stages are genuinely separate vs. which TutorFlow combines

| Preply (separate, OBSERVED) | TutorFlow (current) | Combined? |
|---|---|---|
| About (name, email, country, subject, languages) | **Split**: `BasicsStep` (name/headline/about-text) + `TeachingStep` (subjects/languages/country) — and email is collected at **account registration**, never in the wizard | Preply's one stage maps to parts of two different TutorFlow stages, plus a pre-wizard step |
| Certification | **Merged into** `CredentialsStep` | Yes |
| Education | **Merged into** `CredentialsStep` (same component as Certification) | Yes |
| Description | Folded into `BasicsStep`'s "About you" field | Yes (merged with About) |
| Video | `VideoStep` | 1:1, no merge |
| Availability | `AvailabilityStep` | 1:1, no merge |
| Pricing | `RateStep` | 1:1, no merge, **but** TutorFlow adds an 8th stage (`ReviewStep`) Preply's real flow does not have at all |

Net effect: TutorFlow's wizard is also 8 stages, but by coincidence of count, not structure — three
of Preply's 8 stages (About, Certification, Education) are each split or merged differently, and
TutorFlow has an entire extra stage (Review) in place of what would be freed-up capacity from
un-merging Certification/Education.

## 3. Evidence for Certification and Education being separate

- **SS03**: standalone screen, explicit H1 "Teaching certification", its own opt-out checkbox ("I
  don't have a teaching certificate"), its own controlled-certificate-list dropdown.
- **SS04/SS05**: standalone screen, explicit H1 "Education", its own opt-out checkbox ("I don't
  have a higher education degree"), entirely different field set (University/Degree/Degree
  type/Specialization/Years).
- **SS06's breadcrumb** shows both as independently checked (`☑ Certification › ☑ Education`) —
  two distinct entries, not one label covering both.
- This evidence **upgrades** TutorFlow's own pre-existing code comment in `CredentialsStep.tsx`
  ("Preply sections 2+3") from an inference to a directly observed fact — the merge was already a
  self-aware TutorFlow choice, not an oversight, but it was never confirmed at this evidence
  strength until this screenshot pack existed.

## 4. Preply's Review/final-submission experience — three separate facts, not one

Your instruction not to conflate "no review step" with "no review/confirmation" is correct — here
is the actual, separated evidence:

| Fact | Evidence | Tier |
|---|---|---|
| No distinct review/summary **step** in the wizard itself | SS13: Pricing is the literal last step; its own footer button is "Complete registration," not "Review" | **OBSERVED** |
| Review **process** takes "3 business days" (marketing page) or "5 business days" (help article) — conflicting, both official, never reconciled | `preply.com/en/teach` vs. help.preply.com registration article | **DOCUMENTED** (both sources) |
| Tutor is notified of the outcome; rejection reasons are recorded and tutor-visible | help.preply.com (rejection-reasons article) | **DOCUMENTED** |
| What the **post-submit confirmation screen** actually displays (a "thank you" page? an inline status badge? nothing but an email later?) | — | **NOT VERIFIED** — SS13 explicitly shows "Complete registration" never clicked; no screenshot evidence exists past that point, and no help article describes the screen itself (only the asynchronous review *outcome*) |

**Conclusion: Preply almost certainly has *some* post-submit confirmation experience** (every
production signup flow does, at minimum a "thanks, we'll be in touch" state) — its absence from the
evidence pack is a gap in what was captured, not evidence that nothing exists. This is explicitly
**unknown**, not negative evidence.

**TutorFlow's own current behavior, for comparison (not Preply evidence — TutorFlow's own existing
implementation):** `ApplicationStatusPanel.tsx` already renders an inline message immediately after
a successful submit — *"Thanks — we're reviewing your application. Most applicants hear back within
a few business days."* — deliberately vague on the 3-vs-5 conflict rather than picking one
unverified number. This already exists and already reasonably approximates "some post-submit
confirmation," independent of whether Preply's own screen looks anything like it.

## 5. Structural options — impact analysis

### Option A: Keep TutorFlow's current grouping (Basics+Description merged, Credentials merged, Review kept)

- **Frontend**: no changes.
- **API/application layer**: no changes.
- **Persistence**: no changes.
- **Validation**: no changes.
- **Tests**: no changes.
- **Risk**: none. **Cost**: zero.
- **Trade-off**: TutorFlow's wizard remains structurally non-identical to Preply's, a real,
  user-visible difference for anyone comparing the two side-by-side (e.g. the strict parity matrix
  continues to list this as an open moderate-severity item indefinitely).

### Option B: Realign TutorFlow's grouping to Preply's exact 8-stage boundaries

Concretely: split `CredentialsStep` into two steps (Certification, Education); fold `BasicsStep`'s
"About you" textarea into its own Description-equivalent step or keep it merged (a sub-decision);
decide whether to drop `ReviewStep` or keep it as an intentional TutorFlow addition.

- **Frontend impact**:
  - `apps/web/src/features/tutor-onboarding/types.ts` — `WIZARD_STEPS` array and `WizardStep` union
    type change (adding/removing step identifiers is a breaking change to every place that
    switches over `WizardStep`).
  - `OnboardingWizard.tsx` — the step-rendering switch, `completedSteps` derivation logic (currently
    one boolean per step, keyed by data presence — e.g. `credentials` currently checks
    `educationEntries.length > 0 || certificateEntries.length > 0`; splitting into two steps needs
    two independent completion checks).
  - `WizardProgress.tsx` — `STEP_LABELS` record, one more/fewer breadcrumb entries.
  - New component: `CertificationStep.tsx` (extracted from `CredentialsStep.tsx`'s existing
    certificate-entry UI, which already exists and works — this is a refactor/split, not new UI
    logic).
  - `EducationStep.tsx` similarly extracted.
  - Every test file for the above (`CredentialsStep.test.tsx` splits into two; `OnboardingWizard`'s
    step-sequencing tests, if any exist, need updating).
- **API/application layer impact**: **none required** — `UpdateDraft.Command` already accepts
  `EducationEntries`/`CertificateEntries` as independent optional fields; splitting the *frontend*
  step that submits them doesn't require a new endpoint or a new command shape. Each new frontend
  step would just call the existing `PATCH /api/tutors/applications/me` with only its own slice of
  the command, exactly as every other step already does.
- **Persistence/schema impact**: **none** — `EducationEntries`/`CertificateEntries` are `OwnsMany(...).ToJson(...)` JSON columns (confirmed by direct inspection, `TutorApplicationConfiguration.cs`), not separate relational tables. No migration is needed to split the frontend step that writes to them.
- **Validation impact**: none — `GetSubmissionReadinessErrors()` doesn't reference step boundaries at all, only field presence.
- **Risk**: low-to-moderate, concentrated entirely in frontend routing/state logic, not data.
- **Cost**: moderate (new components, new tests, `WizardProgress`/`OnboardingWizard` rewiring) but
  bounded and well-understood — no unknowns.
- **Trade-off**: closer Preply parity on structure; but TutorFlow's grouping was a **deliberate**
  choice (the existing `CredentialsStep.tsx` comment explicitly frames it as intentional, not an
  oversight) — undoing it has no technical driver, only a parity-aesthetics one.

### Option C: Hybrid — split Certification/Education (cheap, low-risk per Option B's analysis), keep Review step, leave Basics/Description merge as-is

- Same frontend impact as Option B, scoped to only the Certification/Education split.
- Explicitly keeps the Review step as an acknowledged, intentional TutorFlow improvement (per
  `18-screenshot-evidence.md`'s own framing of this as a "deviation candidate," not a defect) —
  avoids relitigating whether a review step is good UX, which has no Preply-parity argument against
  it (Preply's absence of one is not evidence it's a bad idea, only that Preply doesn't have it).
- **Recommended** (see §9) as the best cost/benefit ratio of the three — closes the one piece of
  Option B with a clean, non-controversial technical case (Certification/Education truly are
  evidenced as separate Preply sections) while not touching the two pieces (Basics/Description
  merge, Review step) that are more defensible as intentional TutorFlow choices than as gaps.

**This is a recommendation, not an approved decision — your call.**

## 6. What TutorFlow's current data model already supports

Verified by direct inspection this session (not assumed):

| Area | Current support |
|---|---|
| Education | `EducationEntry(Institution, Degree, FieldOfStudy, StartYear, EndYear)` — no `DegreeType`, `Specialization`-as-distinct-field (folds into `FieldOfStudy`), no file reference, no verified flag. JSON column. |
| Certifications | `CertificateEntry(Name, IssuingOrganization, IssueYear, FileReference)` — **`FileReference` field already exists in the domain model**, but no UI control renders it (confirmed live — `CredentialsStep.tsx`'s certificate form only shows Name + Issuing organization inputs). No verified flag, no years-of-study range. JSON column. |
| File uploads generally | **Real, working infrastructure already exists**: `TutorPhotoStorage`/`UploadProfilePhoto` — local-disk storage under `wwwroot/uploads/tutor-photos`, magic-byte format detection (JPEG/PNG/WebP, not trusting client content-type), 5MB limit, one-file-per-user overwrite. Explicitly documented as "smallest viable choice until a real deployment target" — i.e., a known, accepted placeholder, not a dead end. |
| Video | `IntroVideoReference: string?` — URL only, no file upload, no recording infra, no AI-assist infra. |
| Availability | `AvailabilitySettingsConfigured: bool` (a flag, not the actual rule count — see the already-known reliability caveat from an earlier audit this session) + real `WeeklyAvailabilityRule` data in the separate `Availability` module. |
| Hourly rate | `Money` value object (amount + currency), EUR-only in practice (`ADR-0004`), no commission/fee concept anywhere. |
| Application status | `TutorApplicationStatus` enum: `Draft → Submitted → UnderReview → Approved/Rejected → Published`, plus `Suspended`. Full admin pipeline already built (`ADR-0028`). |

## 7. What needs backend/schema/storage/external-provider work, and how much

| Requirement | Needs | Estimated shape |
|---|---|---|
| Certificate/diploma **file upload UI** | **No new storage infra** — `TutorPhotoStorage` pattern is directly reusable (same magic-byte validation approach, different subfolder). New: `UploadCertificateDocument`/`UploadDiplomaDocument` feature (handler + endpoint), wiring `FileReference` (already exists on `CertificateEntry`) and a new equivalent field on `EducationEntry`. **Medium**, not "backend-scale" as previously characterized — the hard part (real file storage) is solved; this is mostly plumbing + a new field. |
| "Verified" badge / manual admin review of documents | A new boolean (or small enum) field per entry — **no migration** (JSON column) — plus a real admin review surface (reuses the existing `/api/tutors/admin/*` pattern and `AdminOnly` policy already built for application approval) to flip it. **Medium** — the admin-pipeline pattern already exists once (ADR-0028); this extends it, doesn't invent it. |
| Video in-browser recording | Real browser `MediaRecorder` API work + the **same file-storage question as photos/documents** (where do recorded video blobs land?) — video files are categorically larger than photos/documents; local disk may not be the right choice at that size without revisiting the "no object storage yet" constraint. **Larger** — a real feature, not a quick extension. |
| AI "Create script" assist | Requires a new external AI provider integration (not present anywhere in this codebase) — **out of scope for a frontend-only or even a modest backend task**; a new dependency and a new product decision (which provider, cost, prompt design). |
| Timezone auto-detect-and-confirm | **Frontend only** — `Intl.DateTimeFormat().resolvedOptions().timeZone` is a browser API, no backend change; the same timezone string already flows to the same existing endpoint. **Small.** |
| Commission/"Corporate Tutor Program" disclosure UI | Blocked on the **pre-existing, unrelated** Stripe Connect/commission-model decision (`ADR-0018`/`ADR-0027`) — not something this brief should fold in; already tracked elsewhere. |

## 8. What can be closed safely as frontend-only work, right now

- **Timezone auto-detect-and-confirm** (§7) — zero backend/schema touch, already flagged as the
  next good small win in `20-...md`.
- **Video Do/Don't list completeness** — already done this session.
- **Submit-checklist consistency** (intro video) — already done this session.
- Nothing else on the open-gaps list qualifies as frontend-only without at least a small backend
  addition (even the "reuse existing photo-storage pattern" item needs a new backend endpoint, even
  if the hard infrastructure problem is already solved).

## 9. Recommendation summary (recommendation only, not a decision)

1. **Structural grouping**: Option C (split Certification/Education, keep Review, keep
   Basics/Description merge) — best evidence-to-cost ratio; Options A and B remain fully viable
   alternatives depending on how much you value exact structural parity vs. implementation cost.
2. **Credentials file upload**: lower-cost than previously characterized, thanks to the existing
   photo-storage pattern — worth scoping as a real near-term task, not indefinitely deferred.
3. **Video recording / AI assist**: genuinely larger, keep deferred.
4. **Timezone auto-detect**: approve as a small, immediate, no-risk frontend task if desired.

## 10. Explicit questions requiring your approval

1. Do you approve Option C (or A, or B) for the Certification/Education/Review structural question?
2. If Option B or C: should the frontend split happen before or alongside the file-upload feature
   (they touch the same component)?
3. Do you want the Certificate/Education file-upload feature scoped as a near-term task now that
   its real cost is lower than previously stated?
4. Should "verified" document status be a simple flag (cheap) or a richer admin review record
   (more work, more auditability) — relevant if/when the file-upload feature is approved?
5. Do you approve the timezone auto-detect-and-confirm as an immediate, isolated frontend task?
6. Is Preply's post-submit confirmation experience worth further research (e.g., does any help
   article or third-party account describe it) before deciding whether `ApplicationStatusPanel`'s
   current copy is good enough, or should this be treated as permanently unknowable and TutorFlow's
   own reasonable approximation accepted as-is?

## 11. Acceptance criteria and test strategy (for whichever structural option is approved)

- **Option A**: no criteria — no change.
- **Option B/C** (Certification/Education split): acceptance = both new steps render independently,
  each has its own "Save & continue" gating its own data, `WizardProgress` shows the correct
  expanded/contracted step count, resuming mid-application lands on the correct first-incomplete
  step under the new boundaries, and `GetSubmissionReadinessErrors()` behavior is unchanged (no
  new requirement is silently introduced by the split). Tests: split `CredentialsStep.test.tsx`
  into `CertificationStep.test.tsx`/`EducationStep.test.tsx` covering the same cases each currently
  covers; update `OnboardingWizard`-level tests for the new step count; no backend test changes
  needed (confirmed no API/schema change).
- **File upload** (if approved): acceptance = a tutor can upload a certificate/diploma file, it
  persists via `FileReference`, is retrievable on reload, rejects non-matching file types by
  content (not extension), rejects oversized files, and (if verification is scoped in) an admin
  can mark it verified via the existing admin-pipeline pattern. Tests: handler-level tests mirroring
  `UploadProfilePhotoTests.cs`'s existing pattern; a new admin-action test mirroring
  `TutorsAdminEndpointsTests.cs`'s existing self-action-guard/authorization pattern if verification
  is included.
- **Timezone auto-detect** (if approved): acceptance = the field pre-fills with the browser's
  detected timezone on first visit to the step, a visible "is this correct?" affordance lets the
  tutor confirm or pick a different one from the existing dropdown, and the same validated string
  still flows to the same existing endpoint. Tests: extend `AvailabilityStep.test.tsx` with a
  mocked `Intl.DateTimeFormat` to assert the pre-fill and the confirm/correct interaction.
