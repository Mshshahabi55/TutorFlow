# ADR-0026: Tutor Onboarding / Application Wizard

**Status:** Accepted

## Context

A fresh live Preply benchmark this phase (help-center "Register to teach on Preply" + independent 2026 guides, cross-checked against the prior session's `PREPLY_FORM_DATA_ENTRY_SPEC.md`/`PREPLY_WIZARD_COMPONENT_INVENTORY.md`) confirms: registration (email/password) → an 8-section application (profile picture, certifications, education, profile description, intro video, calendar/availability+timezone, tutoring rate, +one section never reliably sourced) → a single submit action → review within ~3–5 business days (~60% approved outright, ~30% sent back with a feedback request, ~10% rejected) → approved tutors can keep editing and go live. No source describes draft persistence either way; building it is this phase's own best-practice choice, not a copied Preply fact.

A direct audit of the existing repository (not the prior session's reports) found the **entire backend domain and application layer for this already exists, fully built and tested**: `Tutors.TutorApplication` (`Draft → Submitted → UnderReview → Approved → Rejected → Published → Suspended`), `CreateApplication`, `UpdateDraft` (every field below), `SubmitApplication` (with real readiness validation), `GetCurrentApplication`, `GetCurrentStatus`, `GetPublicProfile` — 20 passing tests. `/become-a-tutor` is, however, a literal `ComingSoon` placeholder; **no frontend wizard exists at all.**

The audit also found two genuine, previously-undetected product gaps that block this flow from ever working end-to-end outside tests:

1. **No account can ever gain the `Tutor` role.** `Register` unconditionally assigns only `RoleNames.Student`. `Tutors.CreateApplication` requires `PolicyNames.TutorOnly`. There is no endpoint, event, or admin action anywhere that grants `Tutor` to an existing account — the only reason `TutorsEndpointsTests` works today is that its fixture mints a JWT with the role claim directly. In production, "Become a Tutor" is unreachable.
2. **`TutorApplication.Reject` is a dead end.** Once `Status` is `Rejected`, no domain method transitions it anywhere else — `ApplyDraftUpdate` and `Submit` both require `Draft`. A rejected applicant can never correct and resubmit, contradicting both this phase's explicit requirement ("do not force users to restart the entire application... resubmit where appropriate") and the benchmarked Preply behavior (a "feedback letter asking for changes" implies exactly this path).

Neither gap is scope creep: both are load-bearing prerequisites for the one flow this phase exists to ship.

## Decisions

### 1. Self-service `Tutor` role grant — owned by `Identity`, not `Tutors`

`Identity` gains `POST /api/identity/me/become-tutor`: adds `RoleNames.Tutor` to the caller's own account (idempotent — already-`Tutor` is a no-op success), reissues the session cookie with the updated role set (same `ITokenService`/`AuthCookie.Issue` mechanism `Login`/`Register` already use), and returns the same `SessionResponse` shape. This stays entirely inside `Identity` — "who can hold which role" is Identity's own concern, exactly where `Login`/`Register` already live — rather than `Tutors` reaching into Identity's `UserManager` (a `ProjectReference` violation) or a new cross-module event/command for what is fundamentally a same-request, same-caller, side-effecting self-action (not a fit for `ADR-0014`'s read-only query contracts or an async integration event). The dual-role model this grants into is the same one already established and tested for Student+Tutor accounts elsewhere in the codebase — unchanged, just now actually reachable.

**Deliberately not built:** a generic "add any role to myself" endpoint. Only `Tutor` is needed; a general-purpose role-grant capability would be speculative.

### 2. `Rejected → Draft`: `TutorApplication.ReopenForRevision`

A new domain method, mirroring `Submit`'s own shape:

```csharp
public Result ReopenForRevision(TimeProvider clock)
{
    if (Status != TutorApplicationStatus.Rejected)
        return Result.Failure($"Cannot revise an application in '{Status}' status.");
    Status = TutorApplicationStatus.Draft;
    RejectionReason = null;
    UpdatedAtUtc = clock.GetUtcNow();
    return Result.Success();
}
```

New feature `ReopenApplicationForRevision`, new self-service endpoint `POST /api/tutors/applications/me/reopen` (`TutorOnly`, same ownership shape as every other `/applications/me/*` route). Reusing `Draft` rather than inventing a new `NeedsRevision` status means the **existing** `ApplyDraftUpdate`/`Submit`/every wizard section-save call works unchanged for a resubmission — no second edit path, no duplicated validation. `PublicSlug`, once assigned, is never reassigned (`Submit`'s existing guard), so a resubmitted application keeps its original profile URL.

### 3. Wizard section order and required/optional status — benchmarked, not invented

Matches the benchmarked 8-section Preply order, mapped onto fields the aggregate already has (no new domain fields):

| Step | Fields | Required (per `GetSubmissionReadinessErrors`) |
|---|---|---|
| 1. Basics | Display name, headline, about | Display name **and headline** are backend-enforced; about is optional (no source, either side, requires it) |
| 2. Photo | Profile photo reference (URL) | **Backend-enforced** |
| 3. Teaching | Subjects, specialties, spoken languages, country of birth, years of experience/summary | Subjects + languages are backend-enforced |
| 4. Education & certifications | Repeating entries | Optional both sides — no source confirms required |
| 5. Intro video | Video reference (URL) | Optional (no hard rule sourced; "~2 min" is content guidance, not a validation rule — not invented as one) |
| 6. Availability & timezone | Delegates to the existing `Availability` module (see §5) | Not backend-enforced on the application itself (the flag is informational) |
| 7. Rate | Hourly rate (minor units + currency) | Backend-enforced |

**Correction (2026-10-05, per the independent audit):** this table originally claimed "no field-level rule sourced either side" for the photo and listed the headline as merely "strongly encouraged." Both claims were wrong — `PREPLY_FORM_DATA_ENTRY_SPEC.md`'s own already-gathered research (row 1 of its tutor-application table) marks the photo **"Required"** and the headline **"Required (implied)"**; that evidence existed in this same project when this ADR was first written and was not correctly reflected here. `TutorApplication.GetSubmissionReadinessErrors` now enforces both. This is a correction of this ADR's own prior inaccuracy, not a new product decision — the underlying Preply evidence was already sourced, just misstated.

The sourced-but-never-identified "8th section" is **not invented** — TutorFlow ships 7 real steps plus an explicit final Review step (recommended best practice, not Preply-confirmed, called out as such in `PREPLY_WIZARD_COMPONENT_INVENTORY.md`).

### 4. Draft persistence: the existing aggregate, not a second mechanism

"Save progress / navigate back / close and resume" is already fully solved by `PATCH /api/tutors/applications/me` + `GET /api/tutors/applications/me` — every section save is a normal partial-update call; there is no separate local/session draft store, and the wizard never buffers unsaved cross-section state beyond the field(s) actively being edited. **Which step the UI currently shows is not persisted anywhere** (not local storage, not the URL) — on every load, the wizard derives its starting step fresh from the returned application data (first section with a missing required field, or the Review step if none). This avoids both a second persistence mechanism and URL-state duplication for something that loses nothing by resetting (only the *view*, never the *data*, resets on refresh).

### 5. Availability step composes the existing `Availability` module, unchanged

The wizard's Availability step does not reimplement a calendar — it links the tutor to the exact same self-service `Availability` endpoints (`PUT /me/weekly-rules`, `PUT /me/booking-settings`, `GET /me`) a published tutor already uses to manage their calendar, reusing the already-built UI primitives one layer up where practical. Once both are configured, the wizard calls `PATCH /applications/me` with `AvailabilitySettingsConfigured: true`/`BookingSettingsConfigured: true` — these two booleans already exist on the aggregate for exactly this purpose and were previously unused by any caller.

### 6. Photo/video: URL reference input, not a new upload pipeline

Confirmed via `apps/web/src/features/tutor-profile/mediaUrl.ts`'s own existing comment: no file-storage abstraction (`IFileStorage`/S3/MinIO/etc.) exists anywhere in this codebase, and `resolveMediaUrl` already only trusts an already-absolute `https://` URL. The wizard's photo/video steps are plain URL text inputs with explicit helper copy ("paste a link to an already-hosted image/video — TutorFlow doesn't have its own upload storage yet") — honest about the boundary, not a fake upload control, and not a new local-disk persistence hack. Building real object storage is out of scope for this phase and is tracked in `OPEN_QUESTIONS.md`, not silently invented here.

### 7. No new design-system primitives; no new wizard-shell abstraction speculatively generalized

`WizardShell`/`WizardStep`/`WizardProgress`/`ReviewStep` are built as onboarding-specific components composing the **existing** `FormField`, `Input`, `Select`, `MultiSelect` (reused from Search's filter panel), `Button`, `Card`, `LoadingState`, `ErrorState`, `EmptyState` — no second component library, no generic multi-purpose wizard engine built ahead of a second consumer.

## Consequences

- `Identity`: one new endpoint, no schema change (role tables already exist via `AddRoles<IdentityRole<Guid>>()`).
- `Tutors`: one new domain method + feature + endpoint, no schema change (`Rejected`/`Draft`/`RejectionReason` all already exist).
- `apps/web`: `/become-a-tutor` changes from a `ComingSoon` stub to a real entry point; new `/become-a-tutor/apply` wizard route; new `features/tutor-onboarding/` feature folder.
- No new module, no second `TutorApplication`-like aggregate, no second draft-storage mechanism, no new cross-module event, no file-storage infrastructure.

## Noted but not fixed here

- `docs/design/PREPLY_SEARCH_PARITY_MATRIX.md` is stale relative to `ADR-0024` (flagged previously, still not corrected — out of this phase's direct scope, revisited only if the onboarding audit itself touches Search, which it does not).
- Real object/media storage remains an open architectural gap (`mediaUrl.ts`'s own comment, restated in `OPEN_QUESTIONS.md`).
- `Approve`/`Reject`/`BeginReview`/`Publish`/`Suspend` remain uncallable via any HTTP endpoint (no `Admin` role exists yet, `OPEN_QUESTIONS.md` A35) — unchanged by this phase, consistent with the already-accepted precedent for `Publish`/`Suspend`.
