# First-Form Review — Preply "About" Stage vs. TutorFlow Basics + Teaching

**Date:** 2026-10-18. **Scope:** read-only investigation and comparison, per the current phase's
instructions. No application code, API, schema, or test was modified. This is the first of eight
planned per-stage reviews under the approved Option B wizard-realignment direction
(`21-tutor-onboarding-product-decision-brief.md`) — only the first stage is covered here.

## 1. Evidence inventory

| Source | Date | What it covers | Tier |
|---|---|---|---|
| `18-screenshot-evidence.md`, SS01 | 2026-10-06 (historical) | Preply's real "About" stage, mid-scroll | OBSERVED (content) / PARTIALLY OBSERVED (step-label, inferred from SS06) |
| `18-screenshot-evidence.md`, SS06 | 2026-10-06 (historical) | Full 8-step breadcrumb, confirms "About" = step 1 | OBSERVED |
| This session, live TutorFlow | 2026-10-18 (fresh) | `BasicsStep.tsx` and `TeachingStep.tsx`, rendered at `/become-a-tutor` | Freshly observed (Playwright, both desktop 1440×900 and mobile 390×844 for Basics; desktop for Teaching) |
| `BasicsStep.tsx`, `TeachingStep.tsx`, `UpdateDraft.cs`, `TutorApplication.cs` | current | Source code | Source-code observation |

**Playwright MCP: confirmed working**, used for all fresh TutorFlow observations this pass.
**Preply's live authenticated "About" stage was NOT re-accessed this session** — same standing
blocker as every prior pass (`/en/tutor-signup` redirects anonymous visitors to `/en/teach`; no
account was created). SS01/SS06 (historical) are the only evidence for Preply's actual field set.

## 2. Preply "About" stage — exact evidence (SS01)

Visible fields, in the order they appear in the screenshot:
1. **First name** (text, populated in the source capture — PII redacted here)
2. **Last name** (text, populated)
3. **Email** (text, populated)
4. **Country of birth** (dropdown, single-select, populated)
5. **Subject you teach** (dropdown, single-select, populated)
6. **Languages you speak** (repeating row: language dropdown + proficiency-level dropdown + delete icon, 2 rows populated in the capture)
7. **Add another language** (text link — repeating-group affordance)

Intro copy (quoted exactly): *"...automatically saved as you complete each section. You can return
at any time to finish your registration."*

**What SS01 does NOT show** (PARTIALLY OBSERVED crop, no breadcrumb/H1 in frame): the exact
on-screen heading/step title for this stage (inferred as "About" from SS06's breadcrumb, not
directly read off this specific image); whether "Subject you teach" supports more than one subject
elsewhere on this same step (only one dropdown visible in this crop); any client or server
validation behavior (no error state captured); character limits on any field (none visible/implied
by the screenshot).

## 3. Fresh TutorFlow observations — exact

### Basics (step 1 of 8), desktop 1440×900 — `evidence/first-form-about-2026-10-18/first-form-basics-desktop.png`

- H2 "The basics"; subtext "How students will see your name and a short introduction."
- **Display name** — text input, `required`, `maxLength=200`, no placeholder
- **Headline** — text input, `required`, `maxLength=200`, helper text "A short, engaging line students see first."
- "Writing tips" box — 4-item ordered list: "Introduce yourself", "Describe your teaching experience", "Explain how you can help students reach their goals", "Give your profile a catchy headline"
- **About you** — textarea, `maxLength=4000`, optional (no asterisk), helper text "Describe your teaching style and experience.", live character counter "`{n}` / 4000" (`aria-live="polite"`)
- "Save & continue" — **disabled** until Display name AND Headline are both non-empty (confirmed via `continueDisabled={!displayName.trim() || !headline.trim()}` in source, and observed live: button was enabled once both fields held values from an earlier session)

### Basics, mobile 390×844 — `first-form-basics-mobile.png`
Same fields, single column, no layout defects observed. Progress breadcrumb wraps to 2 rows (✓ then
2–8), consistent with the already-investigated-and-cleared wrapping behavior from the prior parity
pass.

### Teaching (step 3 of 8), desktop 1440×900 — `first-form-teaching-desktop.png`

- H2 "What you teach"; subtext "Subjects, specialties, and languages students use to find you."
- **Subjects** — multi-select, placeholder "Select at least one subject" (implies required)
- **Specialties** — multi-select, placeholder "Optional" (explicitly labeled optional)
- **Languages you speak** — multi-select; selecting a language adds it to an inline list, each row showing a per-language **Proficiency** select (Basic/Conversational/Fluent/Native, defaults to "Fluent"); placeholder "Select at least one language" (implies required)
- **Country of birth** — single select, helper text "Shown to students as a trust signal — optional."
- **Years of experience** — number input, no required marker, no max/min shown beyond `min={0}`
- **Experience summary** — textarea, no required marker, no character counter
- "Save & continue" — **disabled** until Subjects AND Languages both have ≥1 entry (`continueDisabled={subjectIds.length === 0 || languages.length === 0}`)

## 4. Field mapping — Preply "About" vs. TutorFlow Basics+Teaching

| Preply field (SS01) | TutorFlow equivalent | Step | Notes |
|---|---|---|---|
| First name | — | — | **Not collected in the wizard at all.** TutorFlow collects no first/last name anywhere in this flow; only a single freeform "Display name" |
| Last name | — | — | Same as above |
| Email | — | — | **Collected at account registration** (`/signup`), before the wizard starts, not re-asked in the wizard |
| (none — Preply has no equivalent) | Display name | Basics | TutorFlow-specific field, no Preply "About" equivalent (Preply uses first+last name instead) |
| (none) | Headline | Basics | No equivalent in SS01 at all — this maps conceptually closer to Preply's *Description* stage's "catchy headline" sub-prompt (SS06), not About |
| Country of birth | Country of birth | **Teaching**, not Basics | Same field, different step; TutorFlow marks it optional, SS01 does not indicate required/optional either way |
| Subject you teach (single-select) | Subjects (**multi**-select) | Teaching | TutorFlow allows multiple subjects; SS01 shows only one dropdown — whether Preply's real About stage supports more than one subject elsewhere on the same step is **NOT VERIFIED** (SS01 is a partial crop) |
| Languages you speak (repeating, language+proficiency) | Languages you speak (multi-select + per-language proficiency) | Teaching | Equivalent shape; TutorFlow defaults new entries to "Fluent," Preply's default (if any) is **unknown** (SS01's rows are already-filled, not a fresh empty state) |
| Add another language | (implicit — multi-select itself is the repeating mechanism) | Teaching | Different UI pattern, same capability |
| (none) | Specialties | Teaching | No equivalent found anywhere in SS01; Preply does have a "Specialties" concept elsewhere in this project's broader research (original 17-step brief), but **not evidenced as part of the About stage specifically** |
| (none) | Years of experience | Teaching | No equivalent in SS01 |
| (none) | Experience summary | Teaching | No equivalent in SS01; conceptually closer to Preply's separate *Description* stage |
| (none) | About you | Basics | Same — closer to Preply's *Description* stage (SS06), not About |

## 5. Confirmed gaps, grouped

**Structure:**
- Preply's single "About" stage maps across **two** TutorFlow steps (Basics = step 1, Teaching =
  step 3), with Photo (step 2) sitting between them — a direct consequence of TutorFlow's Option-B
  target requiring this stage to become one contiguous step, not two separated by an unrelated step.
- TutorFlow's "About you"/Headline content (Basics) structurally belongs with Preply's
  *Description* stage (step 5, SS06), not About (step 1) — realigning About will likely need to
  pull Headline out of Basics too, not just merge Basics+Teaching.

**Content:**
- Preply collects First name + Last name; TutorFlow collects neither — only a single "Display
  name" free-text field.
- Preply collects Email in-wizard; TutorFlow collects it at registration, never in the wizard.
- TutorFlow collects Specialties, Years of experience, and Experience summary — **none evidenced**
  anywhere in Preply's About stage (SS01). This is not necessarily wrong (TutorFlow may have valid
  reasons to ask for more than Preply's About stage does), but it is not Preply-parity-justified by
  this evidence.

**Behavior:**
- Preply's languages are added via "Add another language" (repeating rows); TutorFlow uses a
  multi-select dropdown. Functionally equivalent, structurally different interaction.
- TutorFlow's default new-language proficiency is "Fluent" — Preply's default (if any) is unknown
  from a pre-filled screenshot.

**Validation:**
- TutorFlow: Display name + Headline required (client-side `continueDisabled`, confirmed also
  backend-enforced via `GetSubmissionReadinessErrors()` — DisplayName and Headline both checked
  there). Subjects + Languages required (Teaching step, same dual client+backend enforcement
  pattern — `SubjectIds.Count == 0`/`SpokenLanguages.Count == 0` checks exist in
  `GetSubmissionReadinessErrors()`).
- Preply: **no required/optional field is directly evidenced** in SS01 — no asterisks, no error
  state captured (the screenshot shows an already-filled form). **Do not infer Preply's required
  fields from this evidence; they are UNKNOWN.**

**Responsive design:**
- TutorFlow's Basics step: no defects found on mobile (390×844), spot-checked this session.
  Teaching step was not spot-checked at mobile width this pass.
- Preply: **no mobile evidence exists at all** (`18-screenshot-evidence.md` is explicitly
  desktop-only, 1920×1080).

**Backend contract:**
- `UpdateDraft.Command`/`.Validator` (confirmed by source read): `DisplayName`/`Headline`
  `MaxLength(200)`; `About` `MaxLength(4000)`; `YearsOfExperience >= 0`; `SubjectIds`/`SpecialtyIds`
  each `NotEqual(Guid.Empty)`; `SpokenLanguages` validated against ISO 639-1 codes;
  `CountryOfBirthCode` `Length(2)`. No first-name/last-name/email fields exist in this command at
  all — adding them (if Option B's "About" realignment requires it) is a **new field**, not a
  renamed one.

## 6. Explicit unknowns — not guessed

- Preply's exact on-screen heading/title for the About stage (inferred as "About" from the SS06
  breadcrumb label only, never read directly off an H1 in SS01 itself).
- Whether Preply's About stage supports multiple subjects (SS01 shows one dropdown; could be a
  single-subject-only design, or multi-subject support could exist off-screen in this crop).
- Preply's required/optional field markers — none observed.
- Preply's validation error messages and timing — none observed (no error state in the evidence
  pack).
- Preply's default proficiency value (if any) for a newly added language row.
- Whether Preply's About stage is reachable/resumable the same way TutorFlow's is (both claim
  auto-save/resume in their own UI copy, but Preply's actual resume behavior was never tested live).

## 7. Proposed target field order and layout (evidence-linked, not a final decision)

If Option B's "About" stage is implemented as a single merged step, the evidence supports this
field order (matching SS01's visible sequence, with TutorFlow-specific additions placed after the
evidenced fields, not interleaved):

1. First name *(new field — not currently collected anywhere)*
2. Last name *(new field)*
3. Email *(already collected at registration — **product question**: re-display read-only here for
   parity with Preply's visual structure, or continue to omit it? Not decided by this brief.)*
4. Country of birth *(move from Teaching — already exists)*
5. Subject(s) you teach *(move from Teaching — already exists; Preply evidence shows single-select,
   TutorFlow currently multi-select — **keep multi-select** unless evidence of a hard Preply
   single-subject limit emerges, since downgrading existing capability needs its own justification)*
6. Languages you speak + proficiency *(move from Teaching — already exists)*

Fields with **no Preply "About"-stage evidence** (Display name, Headline, About you, Specialties,
Years of experience, Experience summary) are **not placed in this proposed stage** — they would
need a separate product decision on where they belong (most plausibly Headline/About-you joins a
future Description-stage realignment; Specialties/Years/Experience-summary have no evidenced Preply
home at all in this wizard and may be a deliberate TutorFlow addition worth keeping regardless of
Preply parity).

## 8. Files likely affected (later implementation phase — not touched now)

**Frontend:**
- `apps/web/src/features/tutor-onboarding/components/BasicsStep.tsx` — would lose Headline/About
  you (move to a future Description step) or gain First/Last name (if folded into About)
- `apps/web/src/features/tutor-onboarding/components/TeachingStep.tsx` — would lose
  Subjects/Languages/Country-of-birth (move to About), keep Specialties/Years/Experience-summary
  (no evidenced home) or these get reassigned too
- `apps/web/src/features/tutor-onboarding/types.ts` — `WIZARD_STEPS`, `WizardStep` union, possibly
  `UpdateDraftCommand` shape if First/Last name are added
- `apps/web/src/features/tutor-onboarding/components/WizardProgress.tsx` — `STEP_LABELS`
- `apps/web/src/features/tutor-onboarding/components/OnboardingWizard.tsx` — step switch,
  `completedSteps` derivation
- New component likely needed: `AboutStep.tsx` (merged)
- All corresponding `*.test.tsx` files

**Backend (only if First/Last name are added — everything else is a frontend-only field move):**
- `src/Modules/Tutors/TutorFlow.Modules.Tutors/Features/UpdateDraft/UpdateDraft.cs` — new
  `FirstName`/`LastName` fields + validator rules
- `src/Modules/Tutors/TutorFlow.Modules.Tutors/Domain/TutorApplication.cs` — new properties,
  `ApplyDraftUpdate`
- Migration: **required** if First/Last name become real columns (not JSON-owned) — confirmed by
  inspecting `TutorApplicationConfiguration.cs`, these would be plain scalar properties like
  `DisplayName`, not inside a `ToJson()` owned collection, so a real EF Core migration would be
  needed, unlike the Credentials JSON-column fields discussed in `21-...md`.
- `tests/Modules/Tutors/.../TutorApplicationTests.cs`, `TutorsEndpointsTests.cs` — new field
  coverage

## 9. Acceptance criteria and proposed tests (for whichever option is approved later)

- **If fields move between steps only** (no new fields): acceptance = all moved fields retain
  their exact current validation rules and backend contract unchanged; `GetSubmissionReadinessErrors()`
  behavior identical; resume-to-first-incomplete-step logic correctly reflects the new step
  boundaries. Tests: split/rename the existing `BasicsStep.test.tsx`/`TeachingStep.test.tsx` into
  the new step shape; no backend test changes needed.
- **If First/Last name are added**: acceptance = new required (or optional — **product decision**)
  fields persist, round-trip on reload, are included in `GetSubmissionReadinessErrors()` if marked
  required, and the migration applies cleanly against the existing `tutor_applications` table.
  Tests: new `UpdateDraft` validator tests, new `TutorApplicationTests.cs` cases, a new EF Core
  migration reviewed before being applied (not created in this phase).
- Playwright: a fresh desktop+mobile pass of the realigned "About" step once built, following the
  same method used in this report.

## 10. Implementation recommendation (not applied)

Given the evidence, the lowest-risk, highest-evidence-fidelity path is: **move the 4 already-built,
already-evidenced fields (Country of birth, Subjects, Languages+proficiency) from Teaching into a
merged About step alongside Display name**, and treat First/Last name, Email-redisplay,
Headline-placement, and the no-Preply-evidence fields (Specialties/Years/Experience-summary) as
**separate, explicit product decisions** rather than bundling them into one large change. This
keeps the migration-requiring part (First/Last name, if approved) isolated and small, and avoids
guessing at Headline's final home before the Description stage is reviewed with the same rigor.

**This is a recommendation only — not an approved decision.**
