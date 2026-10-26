# About Stage — Evidence-Backed Specification, 2026-10-18

**Scope**: research only. No application code, test, schema, or product decision record was
modified by this task. This report extends (does not replace) `22-first-form-about-parity-review-2026-10-18.md`
with (a) official Preply documentation research, (b) a fresh live-site authentication-boundary
check, and (c) direct inspection of the historical screenshot image itself rather than its written
description alone. The approved product direction this report assumes (per this task's own framing
and `21-tutor-onboarding-product-decision-brief.md` Option B) is: consolidate TutorFlow's Basics +
Teaching content into a single About stage aligned with Preply's 8-stage boundaries.

## 1. Executive summary

- **The live, authenticated About stage was NOT reached this session** — same standing blocker as
  every prior attempt (`/en/tutor-signup` redirects anonymous visitors to `/en/teach`), re-verified
  fresh today. No Preply account exists for this project and none was created.
- **Official Preply documentation does not describe the About stage's fields at all.** The
  registration help article documents only account creation (email/password/OAuth) as "Step 1,"
  and its "Step 2: Fill your application" links to dedicated guides for Photo, Certifications,
  Education, Description, Video, Availability, and Price — but **no dedicated guide exists for
  name/country-of-birth/subject/languages**, the content this project calls "About." This is a
  genuine, newly-confirmed documentation gap, not an oversight in this project's research.
- **One new piece of official evidence was found**: Preply's "How do I edit my profile?" help
  article (fetched today) states that name, country of origin, subject taught, and approved
  language levels are **locked after approval** and can only be changed by contacting Support. This
  has implementation-relevant implications (see §6).
- The only direct evidence of the About stage's actual field layout remains the historical SS01
  screenshot (2026-10-06) — **today, SS01's source image was opened and inspected directly** (not
  just its prior written redacted description), and the written description in
  `18-screenshot-evidence.md`/`22-...md` is confirmed accurate: every field, label, and the intro
  copy match exactly. No new field or discrepancy was found by this direct re-inspection.
- **TutorFlow's current source code was re-read fresh** (not assumed from the prior report):
  `BasicsStep.tsx`, `TeachingStep.tsx`, `UpdateDraft.cs`, `TutorApplication.cs`, and
  `TutorApplicationConfiguration.cs` all match `22-...md`'s description exactly — nothing has
  drifted since that report was written earlier today.
- **Decision on implementation readiness (§9)**: evidence is sufficient to implement the
  **field-move-only** part of an About consolidation (Country of birth, Subjects, Languages+proficiency
  moving from Teaching into a merged step alongside Display name) with no migration. Evidence is
  **not sufficient** to implement First name/Last name/Email collection, since Preply's required/optional
  status, validation, and exact copy for those three fields remain entirely unobserved and
  undocumented — attempting them now would mean guessing, which this project's standing rule
  prohibits.

## 2. Official Preply sources inspected

| # | URL | Title | Accessed | What it establishes |
|---|---|---|---|---|
| 1 | `https://help.preply.com/en/articles/4175138-register-to-teach-on-preply` | Register to teach on Preply | 2026-10-18 | **Step 1** (account creation) is email/password or Facebook/Google OAuth only — no name/country/subject/language fields mentioned. Explicit note: *"Currently, it is possible to register as a tutor only in the desktop version of Preply."* **Step 2** ("Fill your application") is named only as a step; the article links out to per-section guides for Photo, Certifications, Education, Description, Video, Availability, and Price — **it does not link to, or otherwise name, a dedicated "About" section**. Step 3 is application review. |
| 2 | `https://help.preply.com/en/articles/4175240-how-do-i-edit-my-profile` | How do I edit my profile? | 2026-10-18 (article's own displayed update date: **June 22, 2026**) | States verbatim (per fetch extraction): name *"cannot be changed from your profile page... contact our Support Team"*; country of origin, subject taught, and approved language levels are likewise **locked post-approval**, requiring Support contact to change. Email is not mentioned in this article. |

Both are **official Preply Help Center articles** (`help.preply.com`), not marketing copy or
third-party sources. Both were retrieved via an automated fetch-and-summarize tool (not read
character-by-character by a human), so exact wording should be treated as **approximately
accurate, not a verified verbatim quote** — flagged explicitly per source in the table above.

**Links followed from article 1** (12 total found; all are legitimate help-center cross-references,
listed for completeness): profile photo, certificates, education, description guidelines, video
guidelines, work hours/time off, pricing, profile editing, classroom, tutor development, teaching
style tags, teaching conference. The profile-editing link (article 2 above) was the only one
followed and read in full, since it was the only one plausibly relevant to About-stage fields; the
rest cover later wizard stages or post-approval tutor tooling, out of this report's scope.

**No official source documents**: required/optional status for any About field, validation rules,
character limits, the exact on-screen step title, or a default proficiency value for a newly added
language. These remain genuinely undocumented by Preply itself, not just unobserved by this
project.

## 3. Playwright navigation and access results

- **Tooling confirmed available**: Playwright MCP (`mcp__playwright__browser_*`), the same tooling
  used throughout this project's research. No `playwright.config.*` or `playwright` package
  dependency exists in this repository — the MCP server is the only Playwright capability present.
- **Fresh check performed today**: `browser_navigate` to `https://preply.com/en/tutor-signup`
  resolved to `https://preply.com/en/teach` (page title "Teach online: Become an online tutor at
  Preply"). **This confirms the redirect-to-anonymous-landing-page behavior still occurs as of
  2026-10-18.**
- **The authenticated About stage was NOT reached.** No bypass of authentication, CAPTCHA, or
  access control was attempted. No account was created. No session cookies, tokens, or credentials
  were fabricated or requested from the user.
- No CAPTCHA was observed anywhere in the public-facing part of the funnel (landing page,
  registration gate) — consistent with earlier findings this session on a related task.

## 4. Screenshots and verified evidence paths

No new screenshots were captured in this task (the live About stage could not be reached, so there
was nothing new to capture beyond what already exists). This report instead re-verifies two
existing evidence items by direct inspection:

- **SS01** (`docs/product/preply-reverse-engineering/evidence/tutor.zip`, entry
  `tutor/Screenshot 2026-10-06 074156.png`) — **opened and visually inspected directly this
  session** (temporarily extracted to an isolated, non-repository scratch path for viewing only,
  then deleted immediately after inspection; never copied into this repository or any deliverable).
  Confirmed: First name, Last name, Email, Country of birth (dropdown), Subject you teach (dropdown),
  Languages you speak (repeating language + proficiency-level dropdown rows with a delete icon),
  "Add another language" link, and the intro copy *"...automatically saved as you complete each
  section. You can return at any time to finish your registration."* — all exactly as already
  described in `18-screenshot-evidence.md` §SS01. No discrepancy found. The real name/email values
  visible in the source image are **not reproduced here or anywhere in this report**, consistent
  with this project's standing PII-redaction rule.
- `docs/product/preply-reverse-engineering/evidence/first-form-about-2026-10-18/` — TutorFlow's own
  `BasicsStep`/`TeachingStep` screenshots from the prior report, still valid (TutorFlow source was
  re-read fresh this session and confirmed unchanged — see §5).

## 5. Field-by-field About specification

Confidence tiers used: **OBSERVED** (direct screenshot evidence, re-verified today), **DOCUMENTED**
(official help-center article), **NOT VERIFIED** (no source establishes this).

### First name

- Evidence: OBSERVED (SS01) — labeled "First name," plain text input, populated in the captured
  instance.
- Required/optional: **NOT VERIFIED**. No asterisk, no blank-state, no validation error was ever
  captured.
- Validation, character limit, default value: **NOT VERIFIED**.
- Post-approval behavior: **DOCUMENTED** — per the profile-editing article, name becomes locked
  after approval; changes require contacting Support.
- Unknowns that must not be guessed: whether first/last name are one combined field or two on
  Preply's side elsewhere in the flow (SS01 shows two distinct inputs, so this is actually
  OBSERVED, not unknown); the exact validation message for an invalid/empty name.

### Last name

- Same evidence tier and unknowns as First name. OBSERVED as a separate text input in SS01.

### Email

- Evidence: OBSERVED (SS01, populated value) that an Email field **also appears again inside the
  wizard's first step**, not only at the earlier registration gate.
- This is a notable structural fact: Preply asks for email **twice** — once implicitly via the
  registration gate's email/password form (DOCUMENTED + OBSERVED fresh today,
  `docs/product/preply-reverse-engineering/evidence/playwright-tutor-onboarding-2026-10-18/`), and
  again as a field inside the About step itself (OBSERVED, SS01, historical). Whether the wizard's
  copy of the field is pre-filled read-only from the registration step, or independently editable,
  is **NOT VERIFIED** — SS01 shows it already populated, consistent with either possibility.
- Required/optional, validation: **NOT VERIFIED**.
- Post-approval behavior: **NOT VERIFIED** — the profile-editing article does not mention email at
  all.

### Country of birth

- Evidence: OBSERVED (SS01) — labeled "Country of birth," single-select dropdown, populated.
- Required/optional: **NOT VERIFIED** directly on this field, but see cross-evidence below.
- **Cross-evidence (OBSERVED, historical, different session)**: `18-screenshot-evidence.md` records
  that the Photo step's live preview card shows a small country-flag icon next to the tutor's name
  (SS02), and that a separate authenticated student-facing "Find your tutor" quiz includes "Tutor's
  country" as an explicit public search filter (SS27, `country_of_birth`). **This establishes
  country of birth is a public, student-facing field, not an internal/admin-only one** — consistent
  with, and stronger evidence for, TutorFlow's own helper text on this field ("Shown to students as
  a trust signal").
- Post-approval behavior: **DOCUMENTED** — locked after approval per the profile-editing article;
  changing it requires contacting Support.
- Default value: **NOT VERIFIED** for a fresh/empty state (SS01's dropdown is already populated).

### Teaching subject

- Evidence: OBSERVED (SS01) — labeled "Subject you teach," single-select dropdown, one subject
  visible, populated.
- **Single- vs. multi-subject**: SS01 shows exactly one dropdown in frame. Whether Preply's real
  About stage supports selecting more than one subject (off-screen in this crop, or via a different
  interaction entirely) is **NOT VERIFIED** — this was already correctly flagged as unknown in
  `22-...md` and remains unknown after today's direct re-inspection of the same image; the crop
  genuinely does not show more.
- Required/optional: **NOT VERIFIED** directly, but a subject is functionally necessary for a tutor
  application to mean anything, and TutorFlow's own requirement (`SubjectIds.Count == 0` blocks
  submission) is independently justified regardless of Preply's unconfirmed stance.
- Post-approval behavior: **DOCUMENTED** — locked after approval per the profile-editing article.

### Repeatable spoken-language entries

- Evidence: OBSERVED (SS01) — labeled "Languages you speak," a repeating row pattern: language
  dropdown + a separate "Level" dropdown per row, plus a trash/delete icon per row, and an "Add
  another language" text link below the rows to add more. Two rows populated in the capture.
- Repeat mechanism: **OBSERVED** — explicitly a repeating-row UI (add/remove per row), not a
  multi-select control. This differs structurally (not just visually) from TutorFlow's current
  multi-select + inline proficiency list, though the two are functionally equivalent in the data
  they capture.
- Required/optional: **NOT VERIFIED** — at least one row is populated in the capture, but whether
  zero rows is a valid submission state was never observed.
- Default/minimum: **NOT VERIFIED**.

### Language proficiency

- Evidence: OBSERVED (SS01) — a "Level" dropdown per language row, values visible in the captured
  instance (not reproduced here as they're tied to the redacted values, but the dropdown's
  existence and per-row placement are confirmed).
- Available options: **NOT VERIFIED** — SS01 shows only the two already-selected values, not the
  dropdown's full option list (never opened/expanded in the capture).
- Default value for a newly added row: **NOT VERIFIED** — this was already flagged as unknown in
  `22-...md` and remains unknown; no fresh/empty-row state was ever captured.
- Post-approval behavior: **DOCUMENTED** — per the profile-editing article, *approved* language
  levels are locked; changing an already-approved level requires contacting Support. This implies
  language level is reviewed/vetted content, not a casually-editable preference — relevant to how
  TutorFlow should treat post-submission edits to this field if it adopts similar review semantics
  (currently **out of scope** — TutorFlow has no post-approval profile-lock concept at all; this is
  a genuinely new consideration this report surfaces, not a pre-existing TutorFlow gap).

### Intro/explanatory copy (not a field, but part of the stage's content)

- Evidence: OBSERVED (SS01, re-confirmed verbatim today) — *"...automatically saved as you complete
  each section. You can return at any time to finish your registration."* Establishes per-section
  autosave and resumability as stated UI copy, though the actual resume mechanism (cookie? account
  session? URL state?) is **NOT VERIFIED**.

## 6. Evidence-backed comparison with TutorFlow

| Preply evidence-backed requirement | Current TutorFlow behavior | Gap / mismatch | Evidence source | Recommended change | Open question / risk |
|---|---|---|---|---|---|
| First name + Last name fields exist in the wizard's first step | No first/last name collected anywhere in the wizard; only a single freeform "Display name" (`BasicsStep.tsx`, required, maxLength 200) | TutorFlow has no equivalent data model for this at all | OBSERVED (SS01) + fresh source read (`TutorApplication.cs` — no `FirstName`/`LastName` properties exist) | Treat as a **separate, explicit product decision** — adding these means a new EF Core migration (confirmed: `DisplayName`/`Headline` are plain scalar columns via `builder.Property(...)` in `TutorApplicationConfiguration.cs`, not JSON-owned, so new scalar columns follow the same real-migration path) | Required/optional status, validation, and whether Display Name would still coexist with First+Last name are all unknown — implementing now would require guessing Preply's rules |
| Email field appears again inside the wizard itself, not just at registration | Email is collected once, at account registration (`/signup`), never re-asked in the wizard | TutorFlow's wizard has zero email field; Preply's has one (in addition to the registration-gate one) | OBSERVED (SS01, wizard-internal) + fresh source read (no `Email` property in `TutorApplication.cs`/`UpdateDraft.cs`) | **Product question, not resolved here**: redisplay the already-known email read-only for visual parity, or continue omitting it? Both are defensible; redisplaying read-only carries no migration cost (no new column needed, just reading the existing Identity-owned email) | Whether Preply's wizard-internal email is editable or read-only/pre-filled is unknown, so even a "redisplay" implementation would be guessing at interaction behavior |
| Country of birth — single-select, public-facing trust/search signal | `TeachingStep.tsx`: single-select, helper text "Shown to students as a trust signal — optional," scalar DB column (`CountryOfBirthCode`, `HasMaxLength(2)`), not required for submission | **Structural only** — same field, different step (Teaching, not a merged About); TutorFlow's "optional" framing is *not contradicted* by any Preply evidence (Preply's own required/optional stance is unverified) and is independently corroborated as public-facing by the cross-evidence in §5 | OBSERVED (SS01, SS02 preview card, SS27 search filter) + fresh source read (`TeachingStep.tsx:137`, `TutorApplicationConfiguration.cs:30`) | Move from Teaching into the merged About step. **No migration needed** — existing scalar column, frontend-only move | None significant — this is the lowest-risk item in this report |
| Subject you teach — Preply shows single-select in this crop | `TeachingStep.tsx`: multi-select (`MultiSelect` over `subjectIds`), required (`continueDisabled` + `SubjectIds.Count == 0` backend check), `PrimitiveCollection` DB column | Possible capability mismatch (single vs. multi), but **not confirmed** — SS01 is a partial crop and may not show Preply's full subject-selection UI | OBSERVED (SS01, partial) + fresh source read (`TeachingStep.tsx:81-88`, `UpdateDraft.cs:38`, `TutorApplicationConfiguration.cs:28`) | **Keep multi-select.** Downgrading an existing, working capability to match an unconfirmed single-select observation would be a regression based on incomplete evidence, not a parity fix | If Preply genuinely only supports one subject per application, TutorFlow's multi-subject capability is a deliberate superset — confirm this is intentional before any future change, don't silently remove it |
| Languages you speak + per-language proficiency, repeating-row UI | `TeachingStep.tsx`: multi-select adds entries to an inline list, each with a `Select` for proficiency (Basic/Conversational/Fluent/Native, new entries default to "Fluent"), required (`continueDisabled` + `SpokenLanguages.Count == 0`), JSON-owned column (`OwnsMany(...).ToJson("spoken_languages")`) | **Functionally equivalent, structurally different interaction pattern** (repeating add/remove rows vs. multi-select-driven list). Default proficiency ("Fluent") has no evidenced Preply counterpart | OBSERVED (SS01) + fresh source read (`TeachingStep.tsx:48-56`, `TutorApplicationConfiguration.cs:27`) | Move from Teaching into the merged About step. **No migration needed** — JSON-owned column, frontend-only move. Keep TutorFlow's existing multi-select interaction (no evidence Preply's repeating-row pattern is a hard requirement, just one observed implementation) | "Fluent" default is unverified against Preply and should not be assumed correct or incorrect without further evidence; low-risk either way since it's user-editable immediately |
| No evidenced About-stage field for: Specialties, Years of experience, Experience summary, Headline | `TeachingStep.tsx` collects Specialties (optional) + Years of experience (optional) + Experience summary (optional); `BasicsStep.tsx` collects Headline (required) | These are **TutorFlow additions with no Preply "About" justification** — not necessarily wrong, but not evidence-backed for this specific stage | OBSERVED absence (SS01 shows no such fields) + fresh source read confirming they exist in TutorFlow today | Do **not** fold these into the merged About step without a separate decision on where they belong. Headline conceptually fits Preply's *Description* stage (step 5) per SS06, not About (step 1) — already flagged in `22-...md`, reconfirmed here | Moving Headline out of Basics has knock-on effects on `GetSubmissionReadinessErrors()` (Headline is currently required there) — any step-boundary change must preserve that validation unless a product decision explicitly changes it |
| Post-approval field locking (name, country of origin, subject, approved language levels all locked; Support-contact required to change) | TutorFlow has **no post-approval field-lock concept at all** — `TutorApplication` fields remain editable via `UpdateDraft` regardless of `TutorApplicationStatus` (not verified against every status in this pass, but no lock logic was found in `GetSubmissionReadinessErrors()` or `ApplyDraftUpdate`) | **Genuinely new finding this report surfaces** — not a previously-identified gap in `22-...md`, since that report didn't research official documentation | DOCUMENTED (profile-editing help article, fetched today) | **Out of scope for this report's recommendation** — this is a material product question (should TutorFlow lock fields after approval, and if so, which ones, and how does Support-mediated change work?) that deserves its own decision brief, not a side-effect of an About-stage field move | Significant: if TutorFlow ever implements post-approval locking, it changes `ApplyDraftUpdate`'s behavior for an approved/published application — a backend behavior change, not just a frontend reshuffle |

## 7. Unknowns, contradictions, and risks (consolidated)

**Unknowns (do not guess during implementation):**
- Preply's exact on-screen heading/title for the About stage (still only inferred from SS06's
  breadcrumb, never directly read off an H1 in SS01).
- Required/optional status for every one of the 7 fields evidenced in SS01 — none carry a visible
  asterisk or were seen in a validation-error state.
- Whether "Subject you teach" supports multiple subjects on Preply's side.
- The full option list and default value for the "Level" (proficiency) dropdown.
- Whether the wizard-internal Email field is editable, read-only, or pre-filled-only.
- Preply's resume/autosave mechanism (cookie vs. account session vs. URL state) — only the UI copy
  claiming autosave exists, not its implementation.

**Contradiction found and already flagged in `comparison-with-existing-evidence.md`** (from a
sibling task completed earlier today, reused here since it's directly relevant): Preply's own
landing page claims application review takes "3 business days," while the official help article
claims "within 5 business days." Not resolved by either source inspected in this report; noted for
completeness since it touches the same documentation family.

**Risks:**
- **Migration risk**: confirmed low for the field-move-only scope (Country of birth, Subjects,
  Languages+proficiency are all existing scalar/JSON/primitive-collection columns — no schema
  change). Confirmed **real** if First/Last name are ever added (new scalar columns, real EF Core
  migration against the existing `tutor_applications` table).
- **Draft-preservation risk**: moving fields between wizard steps changes which step's "Save &
  continue" writes which fields, but does not change the underlying `UpdateDraft` command shape for
  the fields being moved (same field names, same validator rules) — so **existing in-progress
  drafts in the database remain valid** under a field-move-only change; no data transformation is
  needed, only UI step reorganization plus `GetSubmissionReadinessErrors()`'s resume-to-step logic
  needing to reflect the new boundaries (frontend `WIZARD_STEPS`/backend readiness-error step
  mapping, if one exists — not independently re-verified in this pass beyond what `22-...md` already
  found).
- **Compatibility risk**: none identified for the field-move-only scope. First/Last name addition
  would need a product decision on default/backfill value for existing `Draft`-status applications
  that predate the new fields (not addressed in this report — out of scope until First/Last name is
  itself approved).

## 8. Recommended implementation scope and acceptance criteria

Unchanged from `22-first-form-about-parity-review-2026-10-18.md` §9–10, reconfirmed by today's
additional research rather than revised:

- **In scope now** (sufficient evidence): move Country of birth, Subjects, and Languages+proficiency
  from `TeachingStep.tsx` into a new merged `AboutStep.tsx` alongside `BasicsStep.tsx`'s Display
  name. No migration required. Acceptance: all three moved fields retain their exact current
  validation rules and backend contract; `GetSubmissionReadinessErrors()` behavior is unchanged;
  resume-to-first-incomplete-step logic correctly reflects the new step boundary. Tests: split the
  existing `BasicsStep.test.tsx`/`TeachingStep.test.tsx` coverage into the new step's test file; no
  backend test changes needed since no backend contract changes.
- **Not in scope now** (insufficient evidence): First name, Last name, wizard-internal Email
  redisplay, Headline's final placement, and any post-approval field-locking behavior. Each needs
  its own product decision before implementation, per the unknowns in §7.

## 9. Decision: is the evidence sufficient to start implementation?

**Partially.** The evidence is sufficient to implement the **field-move-only** part of the About
consolidation (Country of birth, Subjects, Languages+proficiency into a merged step) safely, today,
with no migration and no guessed behavior. The evidence is **not sufficient** to implement
First name, Last name, wizard-internal Email, or any post-approval field-locking — these remain
genuinely unknown or require a separate product decision, and implementing them now would mean
inventing Preply's required/optional rules, validation, and copy rather than observing them.

**Recommended minimum next research action**, if the full About stage (including name/email) is
wanted before implementation: either (a) the user explicitly authorizes creating a throwaway Preply
tutor account and completes the real email-verification/OAuth step themselves (credentials never
typed into chat — only an already-authenticated browser session would be used), or (b) the product
owner accepts starting with the field-move-only scope now and defers First/Last/Email to a later,
separately-evidenced phase once/if authenticated access becomes available.

**This report does not begin implementation.** No files outside this report were created or
modified.
