# About Stage — Product Decision Brief, 2026-10-18

**Author's role**: implementation engineer, not the product/architecture decision-maker. Every
option below is a recommendation, never an applied decision. Nothing in this brief has been
implemented. **Decision authority: ChatGPT.**

## 0. Verification performed before writing this brief

- `git branch --show-current` → `upgrade/dotnet-10-csharp-14`; `git status --porcelain` → 172
  pre-existing working-tree entries, none touched by this task.
- **Report path verification**: the task referenced `docs/product/reverse-engineering/24-...md` —
  that path does not exist. The actual report is
  `docs/product/preply-reverse-engineering/24-about-stage-live-evidence-2026-10-18.md`, read in
  full. No duplicate report was created at the guessed path.
- **Evidence directory verification**: `docs/product/preply-reverse-engineering/evidence/Preply_About/`
  confirmed to exist with exactly the 7 files `24-...md` describes, at unchanged sizes/timestamps
  since that report was written (re-verified via `ls -la --time-style=full-iso`, not re-assumed).
- Historical docs cross-referenced: `18-screenshot-evidence.md`, `20-tutorflow-wizard-strict-parity-matrix-2026-10-18.md`,
  `21-tutor-onboarding-product-decision-brief.md`, `22-first-form-about-parity-review-2026-10-18.md`,
  `23-about-stage-evidence-and-implementation-spec-2026-10-18.md`, and
  `docs/decisions/ADR-0026-tutor-onboarding-wizard.md` (the already-**Accepted** ADR that
  established TutorFlow's current wizard step/validation design — several questions below would
  revise it, not just restyle a component).
- Code freshly re-read for this brief (not assumed from memory): `BasicsStep.tsx`, `TeachingStep.tsx`,
  `BasicsStep.test.tsx`, `TeachingStep.test.tsx`, `TutorApplication.cs`, `TutorSpokenLanguage.cs`,
  `UpdateDraft.cs`, `TutorApplicationConfiguration.cs`, `ApplicationUser.cs`.
- **New, concrete technical finding this task produced**: a direct, read-only SQL query against the
  local dev database (`docker compose exec postgres psql ... -c "SELECT spoken_languages FROM
  tutors.\"TutorApplications\" ..."`, no data modified) found the `LanguageProficiency` enum is
  **stored inconsistently** in existing rows — one record has `"Proficiency": "Native"` (string),
  another has `"Proficiency": 2` (raw integer, i.e. `Fluent`). This is because
  `TutorApplicationConfiguration.cs` applies `HasConversion<string>()` to `Status` but **not** to
  `SpokenLanguages.Proficiency** inside its `OwnsMany(...).ToJson(...)` block — the enum falls back
  to whatever System.Text.Json's default (unconfigured) enum handling produced at write time,
  which has evidently differed across different code paths/sessions. This is directly relevant to
  §5 below and was not known before this task.
- Reference sources consulted: `dotnet-claude-kit`'s `.claude/rules/testing.md` (Testcontainers/
  real-Postgres convention — already how `BasicsStep.test.tsx`/`TeachingStep.test.tsx` and the
  backend test suites operate; nothing to change) and `security.md` (not directly applicable to
  this brief's content, which is product/UX, not auth). The codewithmukesh course has no module
  specific to wizard UX/field consolidation; not applicable here beyond what was already consulted
  in `23-...md`.

---

## 1. Whether and how to consolidate Basics and Teaching into the About stage

**Question**: Should TutorFlow merge `BasicsStep.tsx` and `TeachingStep.tsx` into a single `AboutStep`
matching Preply's one-step About stage, and if so, which fields move?

**Observed Preply behavior**: one stage, labeled "About" (H1 confirmed), containing — in this exact
visible order — First name, Last name, Email, Country of birth, Subject you teach, Languages+Level
(repeating), Phone number (optional), age-confirmation checkbox, then Continue.
Evidence: `24-...md` §2, SCREENSHOT-CONFIRMED for every field's presence/order; `about-main-form.png`
+ `about-country-dropdown.png` together show the full stage top-to-bottom.

**Current TutorFlow behavior**: same underlying data split across two separate steps —
`BasicsStep.tsx` (Display name, Headline, About you) and `TeachingStep.tsx` (Subjects, Specialties,
Languages+Proficiency, Country of birth, Years of experience, Experience summary) — with `PhotoStep`
in between (ADR-0026 §3's approved 7-step order). Source-confirmed, both files re-read this task.

**User impact**: a merge reduces TutorFlow's step count from 8 to 7 and changes where in the flow a
tutor encounters Country of birth/Subjects/Languages (currently step 3, would become step 1).
Nothing about *what data is collected* changes for the fields that already exist on both sides.

**Implications**:
- Frontend: new `AboutStep.tsx`; `types.ts`'s `WIZARD_STEPS`/`WizardStep` union changes;
  `WizardProgress.tsx`'s `STEP_LABELS` changes; `OnboardingWizard.tsx`'s step switch and
  resume-to-first-incomplete-step logic changes. `BasicsStep.tsx`/`TeachingStep.tsx` either shrink
  (lose only the moved fields) or are retired entirely, depending on where Headline/About you land
  (see open question in `22-...md` §5 — Headline conceptually fits a future Description-stage
  realignment, not About; this brief does not re-litigate that, consistent with "do not redesign
  the entire onboarding wizard").
- Backend: **none**, for the fields that already exist (Country of birth, Subjects, Languages) — no
  new migration, confirmed by `TutorApplicationConfiguration.cs` (`CountryOfBirthCode` is a plain
  `HasMaxLength(2)` scalar column, `SubjectIds` is a `PrimitiveCollection`, `SpokenLanguages` is
  JSON-owned). `GetSubmissionReadinessErrors()` is unaffected since it checks fields, not steps.
- Tests: `BasicsStep.test.tsx`/`TeachingStep.test.tsx` would need splitting into whatever shape
  `AboutStep.test.tsx` takes; no backend test changes for a field-move-only scope.

**Options**:
- **A — Full merge now** (Country of birth + Subjects + Languages move into a new About step
  alongside whatever of Basics stays). Pro: closest structural match to evidence. Con: couples this
  decision to the still-open Headline/About-you placement question from `22-...md`.
- **B — Partial merge, defer Headline/About-you** (move only Country of birth/Subjects/Languages;
  leave Basics as a smaller "Display name + About you" step, or fold it elsewhere later). Pro:
  smallest, most reversible change; matches `22-...md` §10's own prior recommendation. Con: doesn't
  fully match Preply's one-step shape yet.
- **C — No structural change** (keep 8 steps as-is; treat this evidence as informational only).
  Pro: zero risk. Con: leaves the already-identified structural mismatch unresolved.

**Recommendation (not a decision)**: **B** — it is the only option that touches zero
still-undecided questions from prior reports, is fully reversible, and requires no migration.

**Decision required from ChatGPT**: approve A, B, or C — and if A, resolve where Headline/About-you
goes as part of the same decision (this brief does not propose an answer).

---

## 2. First name, last name, and email — placement and ownership

**Question**: Should TutorFlow collect First name/Last name/Email inside the tutor wizard, and if
so, where should that data live — on `ApplicationUser` (Identity, applies to every account) or on
`TutorApplication` (Tutors, tutor-only)?

**Observed Preply behavior**: all three fields appear inside the About stage, pre-filled with the
account's real data in the evidence (the Email shown matches the account's actual registered
email). SCREENSHOT-CONFIRMED presence/label/type. Whether they're editable here or merely
redisplayed read-only is UNKNOWN — no edit attempt was captured.

**Current TutorFlow behavior**: `ApplicationUser : IdentityUser<Guid>` (source-confirmed, fresh
read) adds only `CreatedAtUtc` — no `FirstName`/`LastName` property exists anywhere in Identity.
`Email` already exists (inherited from `IdentityUser`). `TutorApplication` has a single freeform
`DisplayName`, no structured name. Neither Register nor any OAuth path collects a real name
(`Register.cs`, `CompleteExternalLogin.cs`, both re-confirmed this session in prior tasks).

**User impact**: if added, every existing Student account (not just Tutors) would be missing this
data retroactively if it's placed on `ApplicationUser`; if placed on `TutorApplication`, only
tutors ever have it, and the "Email, redisplayed" requirement becomes a cross-module read (Tutors
would need the account's email, which it doesn't currently store — it only holds `UserId`).

**Implications**:
- **This is the most architecturally consequential open question in this brief**, because it's a
  module-boundary decision, not just a field decision, under CLAUDE.md's modular-monolith rule
  ("Modules communicate through integration events, never... shared database tables"). Options:
  - **Identity-owned** (`ApplicationUser.FirstName`/`LastName`): matches "a real name is an
    identity-level concept, not tutor-specific" — would also benefit a future Student profile, but
    is new scope for Identity, a module with no product requirement for this today, and needs a new
    EF Core migration against `tutorflow_identity`'s `AspNetUsers` table.
  - **Tutors-owned** (new scalar columns on `TutorApplication`): matches Preply's own observed
    scope (first-party-tutor wizard only) and keeps the change inside the module that already owns
    this UI, but duplicates name data if a dual-role Student+Tutor account exists, and still needs
    its own new EF Core migration (confirmed: these would be plain scalar properties like
    `DisplayName`, not JSON-owned — `22-...md` §8 already flagged this exact migration need).
  - **Don't add them**: keeps `Register`'s "no first-name/last-name/email fields exist in this
    command at all" boundary (re-confirmed, `UpdateDraft.cs`/`Register.cs` fresh read) fully intact.
- Email redisplay specifically: even if read-only, the wizard frontend would need a new read path
  to the account's email — today `TeachingStep`/`BasicsStep` never receive it; `GetCurrentUser.cs`
  (Identity) already returns it to `/api/identity/me`, so the frontend already *has* it available
  app-wide (confirmed via `useAuth()`), meaning a **read-only redisplay needs no backend change at
  all** — only backend change is needed if it becomes editable in-wizard.
- Tests: new `UpdateDraft` validator cases and `TutorApplicationTests.cs`/`IdentityEndpointsTests.cs`
  additions either way, scoped to wherever the decision lands.

**Options**: (1) Identity-owned, editable; (2) Tutors-owned, editable; (3) Email read-only
redisplay only (no name fields at all); (4) add none of the three.

**Recommendation (not a decision)**: **(3) for Email + defer name entirely** — Email redisplay is
genuinely free (no migration, no new write path, already-available data) and directly closes one
piece of the gap; First/Last name carries the real architectural cost above and shouldn't be
decided as a side effect of this brief.

**Decision required from ChatGPT**: pick an option above; if (1) or (2), explicitly resolve the
module-ownership question, since it cannot be inferred from evidence alone — Preply's own
architecture is not visible to this project.

---

## 3. Add the optional phone number + country-calling-code picker?

**Question**: Should TutorFlow add a Phone number field, and if so, what are its validation and
storage requirements?

**Observed Preply behavior**: a text input with a leading country-calling-code dropdown (flag +
dial code, alphabetically sorted, e.g. Eritrea +291/Estonia +372/Ethiopia +251), label explicitly
marked **"(optional)"** — the only field on this entire stage with an explicit optional marker.
SCREENSHOT-CONFIRMED (`24-...md` §2 item 7). Format validation, and whether an invalid-but-present
number blocks submission, are UNKNOWN.

**Current TutorFlow behavior**: no phone field exists anywhere — not in `ApplicationUser`, not in
`TutorApplication`, not in any DTO. Confirmed absent by source search this task and the prior one.

**User impact**: a genuinely new capability for TutorFlow, not a reorganization. Low UX risk if
optional (matches Preply's own marking), but international phone validation and a country-code
picker are new, non-trivial frontend surface (no existing TutorFlow component does this — the
closest precedent is `COUNTRY_OPTIONS`/`Select` used for Country of birth, which has no dial-code
concept).

**Implications**:
- Frontend: a new input type (combined dial-code select + number text, with its own validation
  library choice — e.g. a libphonenumber-style package would be a **new dependency**, explicitly
  flagged since CLAUDE.md's `Directory.Packages.props` convention requires `dotnet add package`
  resolution for backend packages and the frontend has no equivalent constraint documented, but
  adding any new frontend package is still a real, reviewable decision, not a free action).
- Backend: new nullable string column on `TutorApplication` (not JSON-owned — a new scalar
  property, same migration shape as First/Last name above) + a new `UpdateDraft` validator rule
  (format TBD — E.164 is the common choice but is a product decision, not inferred from evidence).
- No existing TutorFlow data/behavior is displaced — this is purely additive.

**Options**: (1) add as optional free-text (no format validation, matching the one thing evidence
*does* confirm — the optional marker — and nothing it doesn't); (2) add with E.164/libphonenumber
validation (adds a dependency, matches a common SaaS pattern but is not evidenced by Preply); (3)
don't add.

**Recommendation (not a decision)**: **(1)** if added at all — it's the only option that doesn't
invent validation Preply's own evidence never showed, consistent with this project's standing rule
against inventing unobserved rules.

**Decision required from ChatGPT**: add or don't; if added, approve the validation approach and the
new dependency (if any).

---

## 4. Add the over-18 confirmation — validation and persistence

**Question**: Should TutorFlow add an age-confirmation checkbox, and how should its value be
validated and persisted?

**Observed Preply behavior**: a checkbox labeled "I confirm I'm over 18", unchecked in every frame
it appears in. SCREENSHOT-CONFIRMED presence. Whether it blocks submission is UNKNOWN — per the
user's own note, Continue is always enabled regardless, so **this project has never observed
Preply actually enforce this checkbox at all**, only that the control exists.

**Current TutorFlow behavior**: no age/date-of-birth/majority concept exists anywhere in the
codebase. Confirmed absent by source search.

**User impact**: this is the one field in this entire brief with a real legal/compliance dimension
(minors teaching paid lessons) rather than a pure UX question — it should not be treated as "just
another checkbox" the way Phone can be.

**Implications**:
- Backend: simplest persistence is a `bool AgeConfirmed` (or similar) on `TutorApplication` —
  cheap, scalar, no migration complexity beyond the column itself. A date-of-birth field would be
  more rigorous (allows re-verification, supports a real age calculation) but is **not evidenced**
  — Preply's own UI, per the evidence, asks for a confirmation checkbox, not a birthdate.
  Inventing a stricter mechanism than what's observed would itself violate this project's
  evidence-only rule.
- Whether this should gate `GetSubmissionReadinessErrors()` (i.e., actually block submission) is
  explicitly **not answerable from evidence** — the only data point (Continue always enabled) cuts
  the other way, but that could simply mean Preply validates at a later, unobserved step rather
  than never at all.
- This is the one item in this brief with a plausible case for **legal review outside this
  project's own evidence-based process** — flagged, not resolved here.

**Options**: (1) add as a non-blocking checkbox, stored but not enforced (matches the one thing
evidence shows — unenforced at this stage); (2) add and enforce via `GetSubmissionReadinessErrors`
(invents an enforcement rule evidence doesn't show); (3) don't add, pending explicit legal/product
input given the compliance dimension.

**Recommendation (not a decision)**: **(3)** — this is the one question in this brief where "wait
for more than Preply screenshots" is itself the responsible recommendation, given the stakes differ
qualitatively from a cosmetic field.

**Decision required from ChatGPT**: whether to proceed on evidence alone here or require separate
compliance input before any implementation.

---

## 5. Replace or migrate the proficiency scale to CEFR (A1–C2 + Native)?

**Question**: Should `LanguageProficiency` change from `{Basic, Conversational, Fluent, Native}` to
Preply's evidenced `{A1, A2, B1, B2, C1, C2, Native}`?

**Observed Preply behavior**: the full 7-value CEFR scale, SCREENSHOT-CONFIRMED for the first time
this session (`24-...md` §2 item 6; prior reports could only see 2 of 7 values in a partial crop).

**Current TutorFlow behavior**: `TutorSpokenLanguage.Proficiency : LanguageProficiency` enum with 4
values (`Basic, Conversational, Fluent, Native`), source-confirmed fresh this task. New entries in
`TeachingStep.tsx` default to `Fluent`.

**New finding governing this section's risk assessment**: a direct, read-only query against the
local dev `tutorflow_tutors` database found `SpokenLanguages.Proficiency` is **already
inconsistently serialized** across existing rows — `"Native"` (string) in one record, `2` (raw int,
= `Fluent`) in another — because `TutorApplicationConfiguration.cs` never applies
`HasConversion<string>()` to this enum inside its `OwnsMany(...).ToJson(...)` block, unlike
`Status`, which does. This means:
- This is **not a schema migration** in the traditional EF Core sense (the column is `jsonb`,
  already flexible) — but it **is** a real **data-correctness migration**: existing stored values
  (in whichever of the two formats they happen to be in) encode meaning under the *old* 4-value
  enum's ordinal/name space. Simply changing the C# enum's members would silently reinterpret
  every existing integer-stored value under new, wrong semantics (e.g. a stored `2`, meant as the
  old `Fluent`, would deserialize as whatever occupies ordinal 2 in a new 7-value enum — not
  `Fluent` or anything resembling it).
- Any migration plan **must** handle both string-keyed and integer-keyed existing JSON values, not
  just one — demonstrated by direct evidence, not a theoretical edge case.
- This is local dev data (plausibly test accounts from this project's own sessions), not
  production — but the *mechanism* that produced the inconsistency (an unconfigured enum
  converter) is a code-level fact that would affect production identically once any real data
  exists.

**User impact**: tutors with an already-set proficiency would see it silently change or corrupt on
read unless explicitly migrated; this is the highest-technical-risk item in this entire brief.

**Implications**:
- Backend: (a) fix the missing `HasConversion<string>()` on `Proficiency` first, independent of
  whether CEFR is adopted, to stop the inconsistency from growing — this is arguably a latent bug
  regardless of this brief's outcome; (b) a data-backfill script/migration to normalize existing
  rows to a known format before any enum-shape change; (c) an explicit mapping decision for old→new
  values if CEFR is adopted (e.g. `Basic→A2`? `Conversational→B1`? — **not evidenced by anything
  supplied, invented by nobody in this brief on purpose**).
- Frontend: `PROFICIENCY_OPTIONS` in `TeachingStep.tsx` (or its `AboutStep` successor) changes
  shape; the default-for-new-row value (currently `Fluent`) has no CEFR equivalent evidenced either
  (`24-...md` explicitly flags this as UNKNOWN).
- Tests: `TeachingStep.test.tsx` and any backend `TutorApplicationTests.cs` cases referencing the
  old enum values need updating.

**Options**: (1) adopt CEFR fully, with an explicit backfill mapping approved separately; (2) fix
the serialization bug only (add `HasConversion<string>()`), keep the 4-value scale; (3) do neither,
leave as-is and flag as a known latent risk.

**Recommendation (not a decision)**: **(2) now, (1) as a separate, explicitly-scoped follow-up** —
fixing the unconfigured converter is low-risk and closes a real bug regardless of the CEFR
question; changing the scale itself needs its own backfill-mapping decision this brief deliberately
does not invent.

**Decision required from ChatGPT**: approve the converter fix as an independent, non-product
technical correction (if so, this is the one item in this brief with a plausible case for being
*mechanical enough* not to need full product sign-off — but is still listed here rather than
assumed); separately decide on CEFR adoption and, if approved, the exact old→new value mapping.

---

## 6. Does the subject selector remain single-select?

**Question**: Should `TeachingStep`'s multi-select Subjects field be downgraded to single-select to
match every piece of Preply evidence ever gathered?

**Observed Preply behavior**: single-value dropdown in **every** supplied image, collapsed and
expanded, historical and fresh (`18-...md` SS01 and `24-...md` §2 item 5, both single-select, no
"+Add another subject" affordance anywhere). Still not proven to be a **hard platform limit** — no
screenshot shows an attempt to add a second subject and its outcome.

**Current TutorFlow behavior**: `TeachingStep.tsx` uses `MultiSelect` over `subjectIds`, required
(`continueDisabled` + backend `SubjectIds.Count == 0` check). Source-confirmed.

**User impact**: downgrading removes an existing, working, tested capability on still-incomplete
evidence (absence of a feature in screenshots is not proof of its absence in the product).

**Implications**: backend `SubjectIds` is already a `PrimitiveCollection` column supporting any
count ≥0 — no schema change either way. A downgrade is a pure frontend/validation change
(`MultiSelect`→`Select`, `continueDisabled` logic adjusted) plus a data question: what happens to
any already-stored application with >1 subject if the UI no longer supports editing that state?

**Options**: (1) keep multi-select (status quo); (2) downgrade to single-select to match evidence.

**Recommendation (not a decision)**: **(1), unchanged from `22-...md`'s own prior recommendation**
— downgrading an existing, working capability on absence-of-counter-evidence (rather than
evidence-of-a-limit) inverts this project's own evidence standard. The brief flags this is now
*more* evidenced than before, not *fully* evidenced.

**Decision required from ChatGPT**: confirm keep-as-is, or explicitly accept the downgrade despite
the gap noted above.

---

## 7. Continue button: disabled-until-valid vs. Preply's reported always-enabled behavior

**Question**: Should TutorFlow relax its client-side Continue-button gating to match Preply's
reported behavior?

**Observed Preply behavior**: always enabled — **USER-CONFIRMED**, not independently screenshot-proven
(`24-...md` §2, explicit caveat: "no screenshot shows an attempted-click-while-invalid outcome").

**Current TutorFlow behavior**: this is not incidental UI — it is **ADR-0026 §3, an already-Accepted
decision**: `BasicsStep` disables Continue until Display name + Headline are non-empty;
`TeachingStep` disables it until Subjects + Languages each have ≥1 entry. Both are asserted by
existing, passing tests (`BasicsStep.test.tsx:17,27,44,60`; `TeachingStep.test.tsx:38,58`, line
numbers confirmed this task).

**User impact**: relaxing this is a genuine UX-philosophy change (validate-before-allowing-progress
vs. validate-after-the-fact), not a cosmetic tweak, and it would revise an Accepted ADR, not just a
component.

**Implications**: frontend-only (remove the `continueDisabled` conditions), but requires deciding
*what replaces* the gating — if Continue is always enabled, something must still communicate
missing-required-field state (inline errors on submit attempt? A non-blocking banner? Evidence
doesn't show what Preply does here either, only that the button itself stays enabled). Existing
tests asserting `toBeDisabled()` would need rewriting, not just relaxing — a behavior change with
real test-authoring cost, not a one-line fix.

**Options**: (1) keep disabled-until-valid (status quo, ADR-0026-consistent); (2) adopt
always-enabled, with inline-on-submit-attempt validation as the replacement mechanism (closest
designed guess at what Preply might do, but not evidenced); (3) adopt always-enabled with no
replacement validation surface at all (closest literal match to "always enabled," but leaves users
with no feedback — not recommended, included only for completeness).

**Recommendation (not a decision)**: **(1)** — this is a single, not-fully-independently-verified
user observation asking to reverse an Accepted ADR; the bar for reversing an already-Accepted
decision should be higher than one user-reported behavior note, however credible.

**Decision required from ChatGPT**: explicitly affirm ADR-0026 §3's gating stands, or approve
revising it (and if revising, pick the replacement validation mechanism, since evidence doesn't
supply one).

---

## 8. Sticky bottom action across short/long forms, mobile, and keyboard-open states

**Question**: What should TutorFlow's Continue button's scroll/viewport behavior be, and what is
actually known about Preply's?

**Observed Preply behavior**: **USER-CONFIRMED** fixed/sticky at the bottom while scrolling, on
whatever viewport the account holder was using when the evidence was captured (full-OS-chrome
screenshots suggest a standard desktop browser window — no explicit viewport width was recorded).
Both images showing the button are *consistent with* sticky behavior (button at a stable
frame-relative position across two different scroll depths) but this is corroborating, not
independently proving, per `24-...md` §2's own caveat.

**What remains explicitly unverified** (stated here, not invented):
- **Short forms** (does a short/near-empty form's footer still stick, or sit inline if content
  doesn't fill the viewport?) — UNKNOWN, no evidence either way.
- **Long forms** (does the sticky button ever re-dock, shrink, or yield to a "scroll to see more"
  affordance on a very tall form?) — UNKNOWN.
- **Mobile viewports** — UNKNOWN. Every piece of evidence in `18-...md` and `24-...md` is desktop
  (`18-...md` is explicitly documented as 1920×1080-only; the new evidence is full-OS-chrome
  desktop screenshots too). **No mobile Preply evidence of this stage exists anywhere in this
  project.**
- **Keyboard-open states on mobile** (does a sticky footer get pushed up/obscured/collapsed by an
  on-screen keyboard over a focused text input?) — UNKNOWN, and cannot be inferred from desktop
  evidence at all.

**Current TutorFlow behavior**: not independently re-verified in this brief (out of scope to
re-screenshot the live app here); no prior report in this project has ever claimed TutorFlow's
`StepShell` wrapper uses a sticky footer.

**User impact**: implementing a sticky footer on desktop alone is a relatively safe, well-evidenced
(by user report) change; extending the same assumption to mobile/keyboard-open states would be
**pure invention** — explicitly against this project's standing rule.

**Options**: (1) implement sticky-on-all-viewports now, accepting the mobile/keyboard-open behavior
is unverified and may need revision later; (2) implement sticky-on-desktop only, leave mobile as
TutorFlow's current (non-sticky, presumably inline) behavior until mobile evidence exists; (3) defer
entirely pending fresh mobile Preply evidence.

**Recommendation (not a decision)**: **(2)** — matches exactly what's evidenced (a desktop
observation) without extrapolating to viewports/states no evidence covers.

**Decision required from ChatGPT**: approve scope (desktop-only vs. all-viewports vs. defer), and
if desktop-only, confirm that's an acceptable permanent split rather than a temporary one pending
more evidence.

---

## 9. Field order, required/optional rules, and navigation — what's actually evidenced

**Observed field order** (SCREENSHOT-CONFIRMED, `24-...md` §2): First name → Last name → Email →
Country of birth → Subject you teach → Languages+Level (repeating) → Phone number (optional) → age
checkbox → Save and continue.

**Required/optional rules — the full, honest state**: **only one field in the entire stage carries
an explicit marker anywhere in the evidence** — Phone number, labeled "(optional)". Every other
field (First/Last name, Email, Country of birth, Subject, Languages, age checkbox) shows **no**
asterisk, no "(required)" text, and no validation-error state in any supplied image. Combined with
the user's own note that Continue is always enabled, **this project has never once observed Preply
enforce a required field on this stage, for any field**. Any required/optional rule TutorFlow
adopts for fields it chooses to add is therefore a **product decision made in the absence of
evidence**, not a transcription of an observed rule — flagged explicitly so it is never later cited
as "evidence-based."

**Navigation behavior**: only "Continue" was observed; no "Back" control was visible in any crop
(About is step 1 of 8, so a Back control may simply have no destination yet — UNKNOWN whether one
exists and is just not visible, or genuinely absent on step 1 specifically).

---

## 10. Consolidated acceptance-criteria framework

Per the task's instruction to separate these four categories explicitly, for whichever subset of
§1–§8 is approved:

### Evidence-confirmed requirements (safe to treat as fact)
- Field order: First/Last name → Email → Country of birth → Subject → Languages/Level → Phone
  (optional) → age checkbox → Continue.
- Phone number is explicitly optional; CEFR proficiency scale is A1/A2/B1/B2/C1/C2/Native, 7 values.
- Country of birth and Subject are single-select dropdowns with search-by-scroll, alphabetical lists.
- Languages support repeating rows via an "Add another language" affordance, each independently
  removable.

### Existing TutorFlow behavior to preserve unless explicitly revised
- `GetSubmissionReadinessErrors()`'s current required-field set (Display name, Headline,
  ProfilePhotoReference, SubjectIds≥1, SpokenLanguages≥1, HourlyRate, IntroVideoReference) —
  untouched by any option in this brief unless §1/§6/§7 are explicitly approved to change it.
- `Register`/`CompleteExternalLogin`'s "no first-name/last-name/email field in the command" boundary
  — untouched unless §2 is approved.
- ADR-0026 §3's Continue-disabled-until-valid gating — untouched unless §7 is approved.
- Subjects as multi-select — untouched unless §6 is approved.

### Requirements that depend on a pending product decision (cannot be written as acceptance criteria yet)
- Any First/Last name/Email-editability acceptance criteria (depends on §2's ownership decision).
- Any Phone format-validation acceptance criteria (depends on §3's validation-strictness decision).
- Any age-checkbox enforcement acceptance criteria (depends on §4's compliance-scope decision).
- Any CEFR backfill-mapping acceptance criteria (depends on §5's old→new value mapping, not yet
  proposed by anyone).
- Any Continue-button replacement-validation acceptance criteria (depends on §7's decision).

### Behaviors that need further live Preply observation before any criteria can be written
- Short-form, long-form, mobile, and keyboard-open sticky-footer behavior (§8) — zero mobile
  evidence exists anywhere in this project for this stage.
- Whether First/Last/Email are editable or read-only in Preply's own wizard.
- Whether Subject truly has a hard single-select platform limit.
- Default proficiency value for a freshly-added language row.
- Whether the age checkbox has any submit-time consequence at all, at any later step.

---

## 11. Summary table — all nine decisions at a glance

| # | Question | Recommendation (not decision) | Needs ChatGPT to decide |
|---|---|---|---|
| 1 | Consolidate Basics+Teaching into About | Partial merge (Country/Subject/Languages only); defer Headline/About-you | A/B/C + Headline placement if A |
| 2 | First/Last name + Email ownership | Email read-only redisplay only; defer names | Module ownership if names are added |
| 3 | Add Phone number | Add as optional free-text, no invented validation | Add/don't; validation strictness; new dependency |
| 4 | Add over-18 confirmation | Hold for explicit compliance input | Proceed on evidence alone, or require separate review |
| 5 | CEFR proficiency scale | Fix the serialization bug now; defer the scale change | Converter fix approval; CEFR adoption + backfill mapping |
| 6 | Subject single- vs multi-select | Keep multi-select | Keep or downgrade |
| 7 | Continue button gating | Keep ADR-0026 §3 as-is | Affirm or revise the ADR |
| 8 | Sticky footer scope | Desktop-only for now | Desktop-only vs. all-viewports vs. defer |
| 9 | Field order/required rules | Treat as evidenced; no required-rule exists to adopt | N/A — informational |
