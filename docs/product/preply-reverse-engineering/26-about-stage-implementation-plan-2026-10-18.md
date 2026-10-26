# About Stage — Preply-Parity Implementation Plan, 2026-10-18

**Status: planning only. No application code was changed to produce this document.**

**Mandate applied in this document** (per the current task, superseding this project's prior
default of treating every difference as an open question): Preply parity is the product target,
not a generic marketplace inspiration. Where evidence is sufficiently strong, this document commits
to a plan rather than reopening the question — including proposing the smallest appropriate
amendment to an Accepted ADR where it conflicts with confirmed Preply behavior. The three
carve-outs this task itself preserves — legal, identity/architecture, and security/compliance
questions — are still treated as genuine blockers below, not resolved unilaterally.

## 0. Verification performed before writing this plan

- Current branch: `upgrade/dotnet-10-csharp-14`. Working tree: 172 pre-existing entries (confirmed
  via `git status --porcelain`, none touched).
- Both prior reports verified at their actual paths (both confirmed to exist, both read in full):
  `docs/product/preply-reverse-engineering/24-about-stage-live-evidence-2026-10-18.md`,
  `docs/product/preply-reverse-engineering/25-about-stage-product-decision-brief-2026-10-18.md`.
  Evidence directory re-confirmed: `docs/product/preply-reverse-engineering/evidence/Preply_About/`,
  7 files, unchanged since `24-...md` was written.
- `ADR-0026-tutor-onboarding-wizard.md` re-read in full (not just §3 this time) — its §3 table and
  the "Status: Accepted" marker are the specific prior decision this plan proposes amending in §7
  below.
- Fresh source re-reads this task: `BasicsStep.tsx`, `TeachingStep.tsx`, `UpdateDraft.cs`,
  `TutorApplication.cs`, `TutorApplicationConfiguration.cs`, `TutorSpokenLanguage.cs`,
  `ApplicationUser.cs`, `Program.cs` (JSON serialization config).
- **Root-cause investigation of the `LanguageProficiency` persistence inconsistency — completed
  this task**, not merely re-cited from `25-...md`. See §3.

### Root-cause investigation: `LanguageProficiency` serialization

A second, deeper read-only query (no data modified) correlated each existing row's serialization
format with its creation time and display name:

```
2026-10-04  "Jane Dev"                      → [{"Proficiency": "Native", ...}]   (string)
2026-10-05  "Smoke Test Tutor"              → []                                  (no data point)
2026-10-13  "Mohammad Shahabihamzekolahei"  → [{"Proficiency": 2, ...}]           (integer)
2026-10-18  "Wizard Parity Test"            → []                                  (no data point)
```

**This rules out a simple "old rows are one format, new rows are another" timeline explanation** —
the string-serialized row is *older* than the integer-serialized row, under the exact same,
never-modified `TutorApplicationConfiguration.cs`. The real cause is structural, not temporal:
`OwnsMany(a => a.SpokenLanguages, l => l.ToJson("spoken_languages"))` applies **no explicit
conversion** to the `Proficiency` member of the owned type, unlike `Status`, which explicitly uses
`HasConversion<string>()` at the aggregate root level (confirmed, same file, line 25 vs. line 27).
Without an explicit, deterministic converter, this property's on-disk JSON representation is an
unenforced implementation detail of whichever EF Core internal code path performed a given write —
and it has already, demonstrably, produced two different outputs for the same property under the
same configuration. (`Program.cs`'s `JsonStringEnumConverter` registration, confirmed at
`options.SerializerOptions.Converters.Add(...)`, governs only ASP.NET Core's HTTP response
serialization — it has no bearing on EF Core's independent internal JSON-column writer.)

**Compatibility implication for any fix**: adding `HasConversion<string>()` (or an equivalent,
exact API to be confirmed against the installed EF Core 10 surface at implementation time — the
`dotnet-claude-kit` `ef-core` skill's own documented pattern, `builder.Property(...).HasConversion<string>()`,
is written for a top-level scalar column, not a property nested inside a `ToJson()` owned
collection; the equivalent call for a nested owned-JSON property needs to be verified against
actual EF Core 10 behavior before being applied) going forward, **without first normalizing
existing rows, risks a read-time failure** on any row currently storing an integer (the Oct-13 row,
confirmed to exist right now). This is a genuine backward-compatibility risk to real, already-saved
user data — not a theoretical one — and is reflected in the dependency ordering in §8.

## 1. Verified current Preply About-stage specification (consolidated from `24-...md`)

Single stage, labeled "About" (H1), URL `preply.com/en/tutor-signup`, step 1 of 8 in the breadcrumb
(About, Photo, Certification, Education, Description, Video, Availability, Pricing). Intro copy:
*"Start creating your public tutor profile. Your progress will be automatically saved as you
complete each section. You can return at any time to finish your registration."* Fields, in order:
First name, Last name, Email, Country of birth (single-select, "Choose country..."), Subject you
teach (single-select, "Choose subject..."), Languages you speak + Level (repeating rows; Level =
CEFR A1/A2/B1/B2/C1/C2/Native, 7 values; "Add another language" link), Phone number — explicitly
"(optional)", with a country-calling-code picker, I confirm I'm over 18 (checkbox, unchecked by
default), "Save and continue" (user-confirmed always enabled and sticky-positioned on desktop).
**No required-field marker (asterisk or text) was ever observed on any field except Phone's
"(optional)" suffix** — this remains true after two independent evidence passes.

## 2. Current TutorFlow behavior, with source references

- `apps/web/src/features/tutor-onboarding/components/BasicsStep.tsx`: step 1 of TutorFlow's 8,
  fields Display name (required, maxLength 200), Headline (required, maxLength 200), About you
  (optional, maxLength 4000). `continueDisabled={!displayName.trim() || !headline.trim()}`.
- `apps/web/src/features/tutor-onboarding/components/TeachingStep.tsx`: step 3, fields Subjects
  (`MultiSelect`, required), Specialties (`MultiSelect`, optional), Languages you speak + Level
  (`MultiSelect` + inline `Select`, required, 4-value enum `Basic/Conversational/Fluent/Native`,
  new rows default `Fluent`), Country of birth (`Select`, optional, helper "Shown to students as a
  trust signal — optional"), Years of experience (optional), Experience summary (optional).
  `continueDisabled={subjectIds.length === 0 || languages.length === 0}`.
- `src/Modules/Tutors/TutorFlow.Modules.Tutors/Domain/TutorApplication.cs`:
  `GetSubmissionReadinessErrors()` requires DisplayName, Headline, ProfilePhotoReference,
  SubjectIds≥1, SpokenLanguages≥1, HourlyRate, IntroVideoReference. No FirstName/LastName/Email/
  Phone/AgeConfirmed property exists anywhere on the aggregate.
- `src/Modules/Identity/TutorFlow.Modules.Identity/Domain/ApplicationUser.cs`: `IdentityUser<Guid>`
  + `CreatedAtUtc` only — no name fields anywhere in Identity either.
- `docs/decisions/ADR-0026-tutor-onboarding-wizard.md` §3: **Status: Accepted.** Established the
  current disabled-until-valid gating for Basics/Teaching as a deliberate decision, cross-referenced
  against a prior benchmark that itself never confirmed a required-field rule either (`"Correction
  (2026-10-05)..."` paragraph shows this ADR's own required/optional calls were already a mix of
  inferred-and-later-corrected judgment, not uniformly sourced).

## 3. Field-by-field and behavior-by-behavior parity matrix

| Area | Preply (evidenced) | TutorFlow (source-confirmed) | Verdict |
|---|---|---|---|
| Stage structure | 1 stage, all fields together | Split across Basics (step 1) + Teaching (step 3), Photo between them | **Mismatch — sufficient evidence to fix** |
| First/Last name | Present, text inputs | Absent entirely | **Mismatch — blocked** (identity ownership, see §5) |
| Email in-wizard | Present, populated | Absent (registration-only) | **Partial fix available now** (read-only redisplay, zero risk) |
| Country of birth | Single-select, "Choose country...", alphabetical | Single-select, exists, different placeholder/step | **Sufficient evidence — move + align copy** |
| Subject | Single-select, consistently, across 2 evidence passes | Multi-select | **Sufficient evidence — change to single-select** (reverses `22-...md`'s prior recommendation, justified below) |
| Languages + Level | Repeating rows, CEFR A1–C2+Native (7 values) | Multi-select + inline select, 4-value non-CEFR scale | **Sufficient evidence — adopt CEFR**, blocked only on the data-backfill step (§8), not on the scale decision itself |
| Phone number | Present, optional, country-code picker | Absent | **Sufficient evidence — add, optional, no invented validation** |
| Over-18 checkbox | Present, unchecked, no observed enforcement | Absent | **Blocked — compliance carve-out** (see §5) |
| Continue gating | User-confirmed always enabled | Disabled-until-valid (ADR-0026 §3, Accepted) | **Sufficient evidence — propose ADR amendment** (§7) |
| Sticky footer (desktop) | User-confirmed + visually corroborated | Not implemented | **Sufficient evidence — implement, desktop only** |
| Sticky footer (mobile/keyboard-open) | **Zero evidence of any kind** | N/A | **Cannot implement — no evidence to implement from; stays a documented gap, not invented** |
| Required-field markers | None observed anywhere, for any field | Several fields backend-enforced required | Not a "mismatch" to silently fix — see §7's replacement-validation design |

## 4. Conflicting ADR and proposed amendment

**ADR-0026 §3 (Accepted)** establishes Display name + Headline (Basics) and Subjects + Languages
(Teaching) as backend- and frontend-enforced required fields, with the frontend additionally
disabling Continue until satisfied.

**Confirmed conflict**: Preply's own About stage, per user-confirmed live observation, never
disables its equivalent Continue button, for any field state. This directly contradicts ADR-0026
§3's frontend-gating choice (not its backend-required-field list, which is a separate, still
evidence-thin question — see §1's "no required marker was ever observed" finding, which argues the
backend list itself is **also** unsourced, not just the frontend gating).

**Proposed smallest amendment** (not applied by this document): add an ADR-0026 §3 addendum —

> Frontend Continue-button gating is relaxed from disabled-until-valid to always-enabled, matching
> confirmed Preply behavior. The backend `GetSubmissionReadinessErrors()` required-field set is
> **unchanged** and remains the actual gate for submission readiness — it already exists
> independently of the frontend button state (the Review step already surfaces it, per
> `19-tutorflow-wizard-live-verification.md`'s prior finding that `ReviewStep.tsx` lists missing
> fields). The frontend's role changes from *preventing* an incomplete Continue click to
> *reporting* validation state inline, after a submit attempt, instead of before one.

This is the smallest change that resolves the confirmed conflict without inventing a new backend
rule Preply's evidence never supported either way.

## 5. Genuine blockers requiring ChatGPT's explicit decision

Per this task's own carve-out ("Do not independently decide unresolved legal, identity, security or
compliance questions"), these two remain blocked, unchanged from `25-...md`:

1. **First/Last name ownership** (identity/architecture): should these live on `ApplicationUser`
   (Identity, every account) or `TutorApplication` (Tutors, tutor-only)? This is a modular-monolith
   boundary decision (CLAUDE.md's "modules communicate through integration events, never shared
   tables" rule), not inferable from Preply's evidence (Preply's own internal architecture is not
   observable). **Decision needed**: pick an owner, or defer the fields entirely.
2. **Over-18 checkbox enforcement** (legal/compliance): Preply's evidence shows the control exists
   but never shows it enforced. Persisting an unenforced confirmation checkbox for a marketplace
   with real payment flows touches minor-user liability — a question qualitatively different from
   every other field in this plan. **Decision needed**: proceed on evidence alone (add, unenforced,
   matching what's observed), or require separate legal/compliance input before adding it at all.

Everything else in §3 is treated below as having sufficient evidence to plan for implementation,
per this task's explicit instruction not to invent unnecessary approval gates for ordinary product
behavior.

## 6. Persistence and backward-compatibility risk summary

| Change | New migration? | Existing-data risk |
|---|---|---|
| Move Country of birth / Subjects / Languages into a merged About step | No — existing scalar/`PrimitiveCollection`/JSON-owned columns, frontend-only move | None |
| Email read-only redisplay | No — already returned by `GetCurrentUser`/`useAuth()` | None |
| Add Phone number | Yes — new nullable scalar column on `TutorApplication` | None (purely additive) |
| Subject: multi-select → single-select | No schema change (`PrimitiveCollection` already supports 0..n) | **Yes** — any already-stored application with >1 `SubjectIds` entry needs an explicit product decision on what happens to the extra entries on next edit (truncate to first? Block save until resolved? Not decided here) |
| CEFR proficiency adoption | No migration **file** (column stays `jsonb`) but **requires** a one-time data-backfill operation | **Yes, confirmed real** — the Oct-13 integer-stored row would misread under a changed enum; backfill must run before the converter fix ships (§8 ordering) |
| Over-18 checkbox (if approved) | Yes — new nullable/boolean scalar column | None (additive); enforcement semantics pending §5 |
| First/Last name (if approved) | Yes — new scalar column(s), location pending §5 | None (additive) |
| Continue-button gating relaxation | No backend change | None — purely a frontend/test change |
| Desktop sticky footer | No backend change | None |

## 7. Testable acceptance criteria

### Ready to write now (evidence-sufficient items)
- **About-stage consolidation**: Country of birth, Subject, Languages+Level render as part of a
  single About-equivalent step; `GetCurrentApplication`/`UpdateDraft` round-trip unchanged for
  these three fields; existing `BasicsStep.test.tsx`/`TeachingStep.test.tsx` assertions for these
  three fields migrate to the new step's test file with identical expected behavior.
- **Email redisplay**: the About step shows the authenticated account's email, read-only, sourced
  from `useAuth()`; no new API call is made; no `UpdateDraft` field is added for it.
- **Phone number**: new optional field; omitting it round-trips as before (no regression to
  `GetSubmissionReadinessErrors()` — Phone is never added to that required set, since no required
  marker was evidenced); no format validation beyond non-empty-string-or-absent, matching the only
  thing evidenced (the optional marker, nothing stricter).
- **Subject single-select**: `TeachingStep`/`AboutStep` renders a single `Select`, not
  `MultiSelect`, for Subject; `UpdateDraft`'s `SubjectIds` contract can still technically accept a
  list (no backend narrowing forced), but the frontend only ever sends 0 or 1 entries.
- **Continue-button gating**: the About step's Continue is never disabled by client-side field
  state; a submit attempt with backend-required fields missing surfaces the existing
  `GetSubmissionReadinessErrors()` messages inline (reusing the pattern already proven in
  `ReviewStep.tsx`, per `19-...md`); `BasicsStep.test.tsx`/`TeachingStep.test.tsx`'s current
  `toBeDisabled()` assertions are rewritten, not merely relaxed.
- **Desktop sticky footer**: the Continue button remains viewport-fixed while the About step's
  content scrolls, at desktop widths; verified visually (Playwright screenshot at two scroll
  depths), consistent with the evidence in `24-...md`.

### Depends on a pending decision (§5) — cannot be written yet
- First/Last name collection, validation, and round-trip (depends on module ownership).
- Over-18 checkbox persistence and any enforcement rule (depends on compliance scope).

### Needs further live Preply observation before any criteria can be written
- Mobile and keyboard-open sticky-footer behavior — zero evidence exists; do not implement a guess.
- Whether Subject truly has a hard platform single-select limit (affects whether the frontend
  should also reject a hypothetical future multi-value `UpdateDraft` payload, or simply never send one).
- CEFR backfill's exact old→new value mapping for the 4 legacy enum members — this is an internal
  TutorFlow data-compatibility question, not a Preply-parity question, and is **not** blocked by §5
  (no legal/identity/security dimension), but still needs a one-line engineering sign-off on the
  mapping before the backfill script is written, since no evidence source provides it.

## 8. Dependency-ordered implementation plan (not executed by this document)

1. **`LanguageProficiency` converter fix + data backfill** (must come first — everything downstream
   that touches this enum depends on the data being in a known, consistent state):
   a. Confirm the exact EF Core 10 API for applying a deterministic conversion to an enum nested
      inside a `ToJson()` owned collection (not assumed by this plan — flagged for verification at
      implementation time).
   b. Write a one-time backfill (raw SQL or a small one-off operation) normalizing every existing
      `spoken_languages` row's `Proficiency` values to one consistent representation, run against
      the dev database first and verified by re-querying (the same technique used for root-cause
      investigation in §0).
   c. Apply the converter configuration change. No EF Core migration *file* is expected (the column
      stays `jsonb`), but this step should still be verified with `dotnet ef migrations add` to
      confirm no unexpected schema diff is generated.
2. **CEFR scale adoption** (depends on 1 completing cleanly, and on the one-line mapping sign-off
   noted in §7): update the `LanguageProficiency` enum, `PROFICIENCY_OPTIONS` in the frontend,
   remap the 4 legacy values per the signed-off mapping, update `TeachingStep.test.tsx`.
3. **Subject single-select** (independent of 1–2, can run in parallel): frontend-only change;
   resolve the "existing multi-subject application" edge case per §6 before shipping.
4. **About-stage consolidation** (benefits from 2–3 landing first, so the merged step is built once
   against the final field shapes, not twice): new `AboutStep.tsx`, `WIZARD_STEPS`/`STEP_LABELS`
   updates, `OnboardingWizard.tsx` resume-logic update, email read-only redisplay added here since
   it's naturally part of the same new step.
5. **Phone number** (additive, can land independently any time after step 4's shape is settled, to
   avoid two frontend-shape changes to the same step in quick succession): new column, new
   `UpdateDraft` field/validator, new form field.
6. **Continue-button gating relaxation** (last among the "sufficient evidence" items, since it
   changes behavior across whichever steps exist at that point — doing it after 4–5 avoids
   rewriting the same test assertions twice): ADR-0026 §3 amendment, frontend gating removal,
   inline submit-time validation, test rewrites.
7. **Desktop sticky footer**: purely additive CSS/layout change, can land any time after 4 (so it
   applies to the final merged step), independent of 1–3/5–6.
8. **Blocked items** (§5): First/Last name and the over-18 checkbox remain unscheduled pending
   explicit decisions; this plan deliberately does not assign them a step number.

## 9. Decisions that genuinely require ChatGPT's approval

1. First/Last name: add or not; if added, which module owns them (Identity vs. Tutors) and what
   that implies for Student accounts.
2. Over-18 checkbox: proceed on evidence alone (add, unenforced), or require separate
   legal/compliance review first.
3. CEFR backfill mapping: a one-line sign-off on the specific old→new value mapping for the 4
   legacy `LanguageProficiency` members (no evidence source supplies one; this plan does not invent
   one either, consistent with the standing evidence-only rule).
4. Confirmation that the ADR-0026 §3 amendment proposed in §4 is acceptable as described (keep
   backend-required-field list unchanged; relax only frontend gating).

Everything else in this plan (consolidation, Email redisplay, Phone addition, Subject single-select,
desktop sticky footer, and the `LanguageProficiency` converter fix + backfill *mechanism*, as
distinct from its value mapping) is presented as ready to proceed once implementation work begins,
per this task's instruction not to invent unnecessary approval gates for evidence-sufficient
ordinary product behavior.

## 10. Explicit confirmation

**No application code, configuration, dependency, schema, migration, or test file was changed to
produce this document.** The only database interaction performed was two read-only `SELECT`
queries against the local development database (via `docker compose exec postgres psql`), used
solely to investigate the `LanguageProficiency` root cause requested by this task; no row was
inserted, updated, or deleted. No git staging, commit, push, or branch operation was performed. All
172 pre-existing working-tree entries remain exactly as they were before this task began.
