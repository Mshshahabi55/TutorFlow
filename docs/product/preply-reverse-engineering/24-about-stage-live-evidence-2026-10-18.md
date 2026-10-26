# About Stage — Live Authenticated Evidence, 2026-10-18

**Scope**: read-only evidence analysis. No application code, styling, dependency, schema, or test
was modified by this task. This report supplements (does not replace) `22-first-form-about-parity-review-2026-10-18.md`
and `23-about-stage-evidence-and-implementation-spec-2026-10-18.md` with the **first genuinely
authenticated, live view** of Preply's actual About stage this project has ever obtained — supplied
directly by the account holder from their own real, pre-existing Preply session (same account
already disclosed and redaction-governed by `18-screenshot-evidence.md`'s PII Notice).

## 1. Evidence inventory and a filename/content mismatch to report explicitly

Evidence location as actually found: `docs/product/preply-reverse-engineering/evidence/Preply_About/`
(the task referenced `evidence/preply-about/`, lowercase-hyphenated — that exact path does not
exist; this is the only matching directory and is treated as the intended one). 7 PNG files, all
distinct sizes, none zero-byte, none duplicated by content hash-equivalent inspection.

**Four of the seven filenames do not match their actual on-screen content.** Reported explicitly,
not silently worked around — I did not assume a file's content from its name anywhere in this
report:

| Filename | Name implies | Actual content (directly observed) |
|---|---|---|
| `about-main-form.png` | main form | **Correct.** Top of page: heading, breadcrumb, First/Last name, Email, Country of birth label. |
| `about-country-dropdown.png` | Country dropdown open | **Partially correct.** Shows Country of birth **filled** (not expanded), plus Subject (filled), Languages/Level rows, Phone number, age checkbox, and the Save button — i.e. "the rest of the form," not a dropdown interaction. |
| `about-subject-dropdown.png` | Subject dropdown open | **Mismatched.** Actually shows the **Country of birth** dropdown expanded (placeholder "Choose country...", alphabetical list starting Afghanistan/Åland Islands/Albania/Algeria). |
| `about-language-dropdown.png` | Language dropdown open | **Mismatched.** Actually shows the **Subject you teach** dropdown expanded (placeholder "Choose subject...", list English/Spanish/French/German...). |
| `about-language-proficiency.png` | Language + proficiency | **Correct.** Shows the Language dropdown (placeholder "Language...", alphabetical: Afrikaans/Albanian/Amharic/Ancient Greek...) and the Level dropdown (A1/A2/B1/B2/C1/C2/Native) both expanded simultaneously, for the first (already-filled) language row. |
| `about-phone-country-code.png` | Phone country-code control | **Mismatched.** Actually shows the same Language+Level dropdown pair expanded again, for a second, empty language row — not the phone field at all. |
| `about-form-footer.png` | Footer/Save button | **Mismatched.** Actually shows the **phone number's country-calling-code dropdown** expanded (flag + country name + dial code, alphabetical: Eritrea +291, Estonia +372, Ethiopia +251...). |

Net effect: every UI element the 7 filenames collectively promise **is** actually present somewhere
in the set — nothing is missing or unreadable — but 4 of the 7 require looking at a differently-named
file than their label suggests. No image was blank, corrupted, or a true duplicate of another.

**No screenshot was supplied specifically proving the sticky/fixed-footer scroll behavior** (e.g.
two captures at different scroll depths with a visibly-moved scrollbar thumb but a stationary
button). That specific claim is recorded below as **user-confirmed**, not screenshot-confirmed —
see §2 and §4.

## 2. Observed-field inventory

Tier key: **SCREENSHOT-CONFIRMED** (directly visible in one of the 7 images), **USER-CONFIRMED**
(stated by the account holder, not independently re-derived from pixels here), **INFERRED**
(reasoned from adjacent evidence), **UNKNOWN** (not evidenced by anything supplied).

### Page chrome

- URL: `preply.com/en/tutor-signup` — SCREENSHOT-CONFIRMED (`about-main-form.png` address bar).
- Breadcrumb, full 8 steps, exact labels and order: **About, Photo, Certification, Education,
  Description, Video, Availability, Pricing** — SCREENSHOT-CONFIRMED, step 1 ("About") shown in a
  solid dark/active badge, steps 2–8 in light/inactive badges with thin connecting lines between
  them — SCREENSHOT-CONFIRMED (`about-main-form.png`). This directly corroborates the step-order
  already recorded in `18-screenshot-evidence.md` SS06, now additionally confirmed in the same
  frame as the About stage's own fields for the first time.
- On-page H1: literally **"About"**, rendered large and in the accent/blue color — SCREENSHOT-CONFIRMED.
  This resolves a gap explicitly flagged as unverified in both `18-...md` and `22-...md`/`23-...md`
  ("Preply's exact on-screen heading/title for the About stage... never read directly off an H1").
- Intro copy, read fresh and in full for the first time (not mid-scroll this time): *"Start creating
  your public tutor profile. Your progress will be automatically saved as you complete each
  section. You can return at any time to finish your registration."* — SCREENSHOT-CONFIRMED. The
  historical SS01 crop only captured the second sentence onward; the lead sentence is new.

### Fields, in visible order

1. **First name** — text input, populated in the capture (real PII — not reproduced here, consistent
   with `18-screenshot-evidence.md`'s standing redaction rule). No asterisk or "(required)"/"(optional)"
   marker visible anywhere near it. SCREENSHOT-CONFIRMED (presence, label, type, absence of a
   required-marker); required/optional status itself remains UNKNOWN — absence of a marker is not
   proof either way.
2. **Last name** — same as above. SCREENSHOT-CONFIRMED (presence/label/type); required/optional UNKNOWN.
3. **Email** — text input, populated. Same marker-absence caveat applies. SCREENSHOT-CONFIRMED;
   required/optional UNKNOWN.
4. **Country of birth** — single-select dropdown. Collapsed value shown: a populated country (not
   reproduced — real account data). Expanded state (different file, see §1) shows placeholder
   **"Choose country..."** and an alphabetically-sorted list (Afghanistan, Åland Islands, Albania,
   Algeria, ...) in a scrollable panel with a visible native-style scrollbar. SCREENSHOT-CONFIRMED.
   No required-marker visible.
5. **Subject you teach** — single-select dropdown. Collapsed value shown: a populated subject.
   Expanded state shows placeholder **"Choose subject..."** and a list (English, Spanish, French,
   German, ...) — SCREENSHOT-CONFIRMED. This is still only ever shown as a **single**-value dropdown
   across every image supplied (both collapsed and expanded) — no multi-select affordance (chips,
   checkboxes, "+Add another subject") appears anywhere. This raises confidence beyond `22-...md`'s
   earlier "partial crop, not verified" caveat, but does not eliminate it entirely: no screenshot
   shows an attempt to select a second subject and its outcome, so single-subject-only remains
   SCREENSHOT-CONFIRMED-FOR-THE-UI-SHOWN rather than confirmed-as-a-hard-platform-limit.
6. **Languages you speak** / **Level** — a repeating two-column row: language dropdown (placeholder
   **"Language..."**, alphabetical list: Afrikaans, Albanian, Amharic, Ancient Greek, ...) paired
   with a proficiency dropdown, plus a trash-can icon per row, plus an **"Add another language"**
   text link below the rows to append more. SCREENSHOT-CONFIRMED, now additionally confirmed with
   a **second, empty row actually added** (visible mid-interaction, with both its dropdowns open),
   proving the repeat affordance is real and working, not just a static label.
   - **Proficiency options — the full list, confirmed for the first time**: **A1, A2, B1, B2, C1, C2,
     Native** — a 7-value CEFR scale. SCREENSHOT-CONFIRMED. This is new, significant evidence:
     prior reports could not see past the two already-filled values (Native, B2) in the historical
     crop and explicitly flagged the full option list and default as unknown.
   - Default value for a freshly-added, not-yet-chosen row: still UNKNOWN — the newly-added second
     row's Level dropdown is shown already opened/interacted-with in the evidence, not in its
     untouched just-appeared state.
7. **Phone number (optional)** — text input with a leading **country-calling-code selector**
   (flag icon + "+" + chevron, e.g. shown collapsed as "+358"). This is a field never documented
   anywhere in this project before this evidence. SCREENSHOT-CONFIRMED, including the label's
   explicit **"(optional)"** suffix — the only field in the entire stage carrying an explicit
   optional marker anywhere in the supplied evidence. Expanded country-code picker: flag + country
   name + dial code, alphabetically sorted (Eritrea +291, Estonia +372, Ethiopia +251, ...),
   scrollable. SCREENSHOT-CONFIRMED. The phone number value itself is real PII and is not reproduced.
8. **"I confirm I'm over 18"** — a plain checkbox with adjacent label text, shown **unchecked** in
   every frame it appears in. SCREENSHOT-CONFIRMED to exist and be present on this stage; also
   never documented anywhere in this project before this evidence. No asterisk/required-marker
   directly on it either. Whether submission is blocked while unchecked is UNKNOWN — no error/blocked
   state was captured.

### Footer / submit

- **"Save and continue"** button — SCREENSHOT-CONFIRMED present, full-width, pink/accent background
  with bold black text, positioned after the age checkbox.
- **Always enabled** — **USER-CONFIRMED**, not independently re-derived from these static images
  (no screenshot shows an attempted-click-while-invalid outcome).
- **Fixed/sticky while scrolling** — **USER-CONFIRMED**. The two images that show this button
  (`about-main-form.png`, cut off at the viewport's bottom edge; `about-country-dropdown.png`, fully
  visible) do show the button at a visually consistent position near the bottom of frame while the
  scrollbar thumb differs between them, which is *consistent with* a sticky footer — but this report
  treats that as corroborating, not independently proving, the user's own statement, since a single
  pair of static screenshots cannot rule out coincidence the way a scroll recording would.

## 3. What remains genuinely unverified after this evidence pass

- Required/optional status for First name, Last name, Email, Country of birth, Subject, Languages,
  and the age checkbox — no asterisk, no validation-error state, and (per the user's own note) the
  Continue button is always enabled anyway, so this project has still never observed Preply enforce
  a required field on this stage.
- Whether "Subject you teach" is a hard single-subject platform limit or just the only state shown.
- Default proficiency value for a freshly-added language row.
- Full behavior of the phone field: format validation, whether a bare "(optional)" label is the only
  gate or whether an invalid-but-present number blocks submission.
- Whether the age checkbox has any submit-time consequence at all.
- The exact mechanism and timing of the stated per-section autosave (copy only, never observed firing).

## 4. Comparison with TutorFlow's current implementation

Re-confirmed fresh against the actual current source (`BasicsStep.tsx`, `TeachingStep.tsx`,
`UpdateDraft.cs`, `TutorApplication.cs` — unchanged since `23-...md` was written; `TutorApplication.cs`
re-checked explicitly for `Phone`/`Age`/`DateOfBirth`-related fields, none exist anywhere in the
Tutors module, confirmed by source search, not by absence-from-memory).

| Preply "About" — now evidenced | TutorFlow today | Gap | Evidence tier |
|---|---|---|---|
| First name, Last name | Not collected anywhere in the wizard; only a single "Display name" free-text field (`BasicsStep.tsx`) | **Missing** — unchanged conclusion from `22-23-...md`, now on firmer evidence (full-page screenshot, not a partial crop) | SCREENSHOT-CONFIRMED (Preply) |
| Email, shown again inside the wizard itself | Collected once, at registration; never re-asked in the wizard | **Missing** — unchanged conclusion | SCREENSHOT-CONFIRMED (Preply) |
| Country of birth, single-select, alphabetical, placeholder "Choose country..." | Exists in `TeachingStep.tsx` (not About), single-select, helper text "optional — trust signal", no placeholder text confirmed in source read | **Structural only** (step placement) — field itself already exists and is a reasonable match | SCREENSHOT-CONFIRMED (Preply) + source-confirmed (TutorFlow) |
| Subject you teach, single-select only in every observed state | `TeachingStep.tsx`: **multi**-select (`MultiSelect`), required | **Capability mismatch, direction unclear** — TutorFlow allows more than Preply ever shows; per `22-...md`'s existing recommendation, keep multi-select rather than downgrade on still-incomplete evidence | SCREENSHOT-CONFIRMED (Preply) + source-confirmed (TutorFlow) |
| Languages + Level, repeating rows, **7-value CEFR scale (A1–C2, Native)** | `TeachingStep.tsx`: multi-select + inline proficiency select, **4-value scale (Basic/Conversational/Fluent/Native)**, new entries default to "Fluent" | **New, concrete content gap** — TutorFlow's proficiency scale does not match Preply's evidenced CEFR scale at all, neither in option count nor in naming convention | SCREENSHOT-CONFIRMED (Preply) + source-confirmed (TutorFlow) |
| **Phone number (optional)**, with country-calling-code selector | **No phone field exists anywhere in TutorFlow** — not in the wizard, not in Identity's `ApplicationUser`, not in `TutorApplication` | **Entirely missing field** — newly discovered this pass, not previously documented in this project at all | SCREENSHOT-CONFIRMED (Preply); confirmed absent in TutorFlow by source search |
| **"I confirm I'm over 18"** checkbox | **No age-confirmation control exists anywhere in TutorFlow** | **Entirely missing field** — newly discovered this pass | SCREENSHOT-CONFIRMED (Preply); confirmed absent in TutorFlow by source search |
| "Save and continue" always enabled, fixed/sticky footer | `BasicsStep.tsx`: Continue **disabled** until Display name + Headline non-empty; `TeachingStep.tsx`: Continue **disabled** until Subjects + Languages each have ≥1 entry; neither step's shell is sticky-footer styled (not independently re-verified in this pass, but no prior report has ever claimed it is) | **Behavioral mismatch** — TutorFlow actively gates Continue on client-side validation; Preply's evidenced behavior (per the user) never does, on this stage | USER-CONFIRMED (Preply) + source-confirmed (TutorFlow) |

**Intended mapping, scoped as instructed**: consolidating TutorFlow's existing Basics + Teaching
content into a merged About stage remains supported by this evidence for the fields that already
exist on both sides — Country of birth, Subjects, Languages (with the proficiency-scale caveat
above). First name/Last name/Email/Phone/Age-confirmation are **new fields**, not a reorganization
of existing ones, and are called out separately below rather than folded silently into "the merge."
This report does not redesign the rest of the 8-stage wizard and does not add any stage beyond
About.

## 5. Acceptance criteria (for whichever scope is approved — not decided by this report)

- **If only the already-evidenced, already-existing fields are consolidated** (Country of birth,
  Subjects, Languages) into a merged About step: acceptance = identical validation/backend contract
  to today, `GetSubmissionReadinessErrors()` unchanged, no migration (all three are existing
  scalar/JSON/primitive-collection columns, confirmed in `22-...md`/`23-...md`).
- **If the proficiency scale is changed to match Preply's CEFR list (A1–C2, Native)**: this is a
  **breaking change to an existing, already-released enum** (`LanguageProficiency`), not an
  additive field — acceptance would need a migration/backfill plan for any already-stored
  `Basic`/`Conversational`/`Fluent` values, and is explicitly flagged here as a **product decision**,
  not something this report recommends unilaterally.
- **If First/Last name, Phone, or the age checkbox are added**: each needs its own field-level
  product decision (required or optional — note Preply marks only Phone as optional, implying but
  never proving the rest are required), a new EF Core migration (new scalar/boolean columns on
  `TutorApplication`, confirmed not JSON-owned), and new `UpdateDraft` validator rules. None of this
  is implemented by this report.
- **If TutorFlow's Continue-button gating is relaxed to match "always enabled"**: this is a
  deliberate behavior change to existing, tested client-side validation (`BasicsStep.test.tsx`,
  `TeachingStep.test.tsx` both assert the disabled state today) and needs an explicit decision, not
  an assumption that Preply's looser gating is automatically correct for TutorFlow.

## 6. Unresolved questions (not answered by this report)

1. Should TutorFlow add First name/Last name/Phone/age-confirmation at all, given none are
   Preply-documented as required anywhere (official help-center research in `23-...md` found no
   dedicated article for this stage), or is Preply's own choice here not necessarily the bar
   TutorFlow should clear?
2. If Phone is added, should it be optional (matching Preply's explicit marker) from day one?
3. Should TutorFlow's 4-value proficiency scale be widened to match CEFR, and if so, how should
   existing `Basic`/`Conversational` values map onto the new scale?
4. Should TutorFlow's wizard adopt Preply's "always-enabled Continue, validate later" pattern, or
   keep its current "disabled until valid" pattern — these are two different, both-defensible UX
   philosophies, not a parity bug.
5. Does Subject really need to stay multi-select, or was that always a TutorFlow-only choice worth
   re-examining now that *every* piece of Preply evidence (historical and fresh) shows only ever a
   single subject?

## 7. Files inspected (no code changed)

- `docs/product/preply-reverse-engineering/evidence/Preply_About/*.png` (all 7)
- `apps/web/src/features/tutor-onboarding/components/BasicsStep.tsx`, `TeachingStep.tsx` (unchanged
  since the last read this project performed; re-confirmed, not re-assumed)
- `src/Modules/Tutors/TutorFlow.Modules.Tutors/Domain/TutorApplication.cs`,
  `Features/UpdateDraft/UpdateDraft.cs` (re-searched for phone/age fields — none found)
- `docs/product/preply-reverse-engineering/18-screenshot-evidence.md`, `22-...md`, `23-...md` (cross-referenced, not restated wholesale)
