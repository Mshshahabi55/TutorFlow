# ADR-0028: Tutor Approval / Admin Review Pipeline

**Status:** Accepted

## Context

`Tutors.TutorApplication` already had a complete, fully-tested state machine (`Draft → Submitted → UnderReview → Approved/Rejected → Published`, plus `Suspended`) and even fully-built handlers for `PublishApplication` and `SuspendApplication` — but **no HTTP endpoint anywhere exposed `BeginReview`/`Approve`/`Reject`/`Publish`/`Suspend`**, because no `Admin` role/policy existed in `Identity` (`OPEN_QUESTIONS.md` A35, `docs/product/preply-reverse-engineering/13-tutorflow-current-state-and-parity.md`: *"Tutor application review/publish pipeline — MISSING — P1, blocks the entire real-world tutor journey"*). The tutor-facing side (`ApplicationStatusPanel.tsx`) was already shipped and already honest about this gap: its own comment reads *"`Approved` is deliberately honest about the still-platform-gated `Publish` step (no Admin surface exists yet, ADR-0026)"*.

This ADR builds exactly that missing surface: the `Admin` role, and the admin HTTP endpoints over the already-existing domain.

## Decision

### 1. `Admin` role — Identity-owned, internal provisioning only

Per the pre-existing `docs/product/ROLES_AND_PERMISSIONS.md` ("`Admin` | Granted via: Internal provisioning only, never self-service signup"), `Admin` is added as a real seeded `IdentityRole` (mirroring `Student`/`Tutor`'s existing `HasData` seed) but, unlike `Tutor` (self-granted via `BecomeTutor`), **no endpoint ever grants it**. Actually assigning `Admin` to a real account is an explicit out-of-band operational step (direct DB/ops action), not a feature built in this slice — building a self-service or admin-granted "make this user an Admin" endpoint was explicitly out of scope and would contradict the "never self-service" decision already on record. Tests mint the role directly onto a test JWT, the same precedent already established for `Tutor` (`TutorsApiFixture.CreateAccessToken`).

`Tutors` defines its own `RoleNames.Admin`/`PolicyNames.AdminOnly` copy rather than referencing `Identity`'s assembly — the same module-boundary pattern it already used for `Tutor`/`TutorOnly`.

### 2. Five separate admin actions — not auto-chained (a TutorFlow architectural choice, not a Preply-verified fact)

`BeginReview`, `Approve`, `Reject`, `Publish`, `Suspend` are mapped as five independent admin endpoints, matching exactly how the pre-existing TutorFlow tests and domain already modeled them (`TutorPublishedPipelineTests`, `TutorsEndpointsTests.CreatePublishedTutorAsync` both already call `Approve()` then `Publish()` as two separate steps) and the already-shipped tutor-facing copy quoted above. **Approve does not auto-publish, in TutorFlow.**

**Evidence boundary — corrected 2026-10-06, do not re-overclaim:** "verified from code" above means verified against *TutorFlow's own pre-existing implementation and tests* — a real, inspectable fact. It does **not** mean verified against Preply's internal admin tooling. The distinction matters:

- **OBSERVED (external behavior, strong evidence):** a real public tutor search page and a real public tutor profile were directly browsed anonymously this project (`01-real-user-observations.md`) — approved/active tutors are genuinely publicly discoverable without an account. **DOCUMENTED:** Preply's own marketing copy states approved tutors "can be approved and start teaching in as little as three days" (preply.com/en/teach).
- **NOT VERIFIED (internal implementation):** whether Preply's own back-office actually exposes "Approve" and "Publish" as two separate administrative actions, or a single combined one, is **not publicly observable** — no screenshot evidence or help-center article describes Preply's internal admin tooling at all. TutorFlow keeping them separate is a defensible architectural choice (it reuses the aggregate's own pre-existing, already-tested domain methods unchanged) — it is not a claim about Preply's private implementation, and must never be cited as one.

`PublishApplication`/`SuspendApplication`'s existing `Handler` classes are reused completely unchanged (their signatures are untouched, so `TutorPublishedPipelineTests`, which call them directly, keep passing unmodified); only a new `Request`/`Validator` was added to `SuspendApplication.cs` additively, and three new features (`BeginReviewApplication`, `ApproveApplication`, `RejectApplication`) were added following the exact same shape.

### 3. Composed Admin (ADR-0008) — no new module

All five actions, plus the two new read queries (`GetPendingTutorApplications`, `GetTutorApplicationForAdmin`), live in `Tutors` — the owning module — under `/api/tutors/admin/*`, gated on `PolicyNames.AdminOnly`. No `Admin` bounded context was created, consistent with ADR-0008.

### 4. Server-side self-action guard

Every mutation (`begin-review`/`approve`/`reject`/`publish`/`suspend`) independently re-looks-up the target application's owning `UserId` and rejects (403) if it matches the caller's own `UserId` — enforced fresh on every request from the database, never trusted from a client-supplied role or UI state. This matters because `Identity`'s role model is additive (ADR-0002: one account can hold `Student` + `Tutor` simultaneously) — nothing prevents an account from also holding `Admin` + `Tutor` at once, so without this guard a dual-role account could approve/reject/suspend its own application.

### 5. Admin review queue shape — engineering proposal, not reverse-engineered

Preply's internal admin tooling is not publicly observable (no screenshot evidence, no help-center article describes it) — `GetPendingTutorApplications`'s shape (status-filterable list, oldest-submitted-first default ordering, 20/page) is this task's own minimal-viable proposal, explicitly **not** claimed as Preply parity. See the updated parity matrix in `13-tutorflow-current-state-and-parity.md`.

## Consequences

- `Identity`: one new seeded role (`Admin`), one new policy (`AdminOnly`), no new endpoint.
- `Tutors`: three new features (`BeginReviewApplication`, `ApproveApplication`, `RejectApplication`), two new read queries, one additive `Request`/`Validator` on `SuspendApplication`, seven new endpoints under `/api/tutors/admin/*`. No domain/schema change — `TutorApplication`'s state machine was already complete.
- `apps/web`: new `features/tutor-approval/` feature folder and `/admin/tutor-applications` (+ `/admin/tutor-applications/[id]`) routes. No change to the tutor-facing `tutor-onboarding` feature — it was already correct.
- Resolves `OPEN_QUESTIONS.md` A35 for `Tutors`' specific need. Payments' own V1 read-only admin endpoints (also blocked on A35, per `PAYMENTS_SPEC.md` §14) can now use the same `Admin` role, but building them is out of this slice's scope.

## Evidence corrections (2026-10-06 audit pass)

A follow-up audit found this ADR's original text, and the parity matrix reported alongside it, overclaimed Preply parity in three places. Corrected here; no code changed as a result (verified via `dotnet ef migrations has-pending-model-changes` — zero pending changes):

1. **"Approve/Publish as separate steps" was classified MATCHED.** Corrected to **NOT VERIFIED** for the internal-granularity claim specifically (see the Evidence boundary note in Decision §2 above). The external outcome — approval eventually results in a publicly discoverable tutor — remains independently supported (OBSERVED + DOCUMENTED, see §2).
2. **"Resubmission after rejection" was classified MATCHED** solely because TutorFlow's own `ReopenApplicationForRevision` exists. That is evidence of a TutorFlow feature, not of Preply behavior. Re-checked both official sources directly (help.preply.com/articles/4175138, preply.com/en/teach, 2026-10-06): neither explicitly describes a rejected applicant's ability to edit and resubmit. The closest adjacent text — preply.com/en/teach's FAQ: *"Sometimes, we don't approve tutor profiles at the first attempt because the profile photo, video or description doesn't meet our requirements"* — uses the phrase "first attempt," which **implies** (not states) that further attempts are possible, but describes neither the mechanism nor whether it's self-service. Corrected classification: **INFERRED** that re-attempts are possible at all; **NOT VERIFIED** for the actual mechanism (edit-in-place vs. new application, self-service vs. Support-mediated, whether another review cycle follows). TutorFlow's `ReopenApplicationForRevision` is recorded as a TutorFlow design decision satisfying this project's own non-Preply-sourced requirement ("don't force users to restart the entire application," `ADR-0026`), not as reverse-engineered Preply parity.
3. **Admin UI / visual parity.** Already correctly scoped as an engineering proposal in Decision §5 and the parity matrix — reconfirmed, not changed. Preply's back-office is not publicly observable; no screenshot evidence of it exists or was fabricated.
4. **Review timing (3 vs. 5 business days).** Unchanged — both sources remain preserved as a documented, unreconciled conflict; no SLA was or is implemented.
5. **External vs. internal framing, generally.** Every claim above now distinguishes what was directly OBSERVED/DOCUMENTED about Preply's public-facing behavior from what is a TutorFlow-internal implementation choice. Phrasing like "Preply has separate Approve and Publish admin buttons" is never asserted anywhere in this document.

## Noted but not fixed here

- Reinstating a `Suspended` tutor has no action anywhere (not evidenced by any Preply source as a defined flow) — tracked as a gap, not invented.
- A tutor's own-dashboard view of their rejection/suspension history beyond the single current reason already shown by `ApplicationStatusPanel`: not built, not evidenced.
- Admin sub-roles (`docs/product/ROLES_AND_PERMISSIONS.md`'s own open question) remain unresolved — `Admin` is still a single flat role.
- No audit log records who performed which admin action or when (beyond `ReviewedAtUtc`/`RejectionReason`/`SuspensionReason` already on the aggregate) — `ADR-0008`'s centralized Audit Logs recommendation remains non-binding and unbuilt.
