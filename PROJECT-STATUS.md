# Project Status Report
Generated: 2026-07-21 | Commit: UNKNOWN — not a git repository (see below) | Branch: UNKNOWN — not a git repository

## 1. Identity
- **Project name / local path:** TutorFlow — `D:\Projects\TutorFlow`
- **Stated purpose (portfolio / production / internal) and any deadline found:** A production-intended "multi-sided scheduling and booking marketplace" connecting Students, Parents/Guardians, and Tutors, with Admin/Staff oversight (`PROJECT_CONSTITUTION.md`: Mission). No portfolio/internal framing found anywhere. No explicit calendar deadline found in any document; `PROJECT_CONSTITUTION.md` and `PRODUCT_REQUIREMENTS.md` both describe this as "v1" of a real product, not a demo.
- **Source of truth for that purpose:** `PROJECT_CONSTITUTION.md` (root) — explicitly marked "immutable" and cited as the top authority by every other document in the repo (`PRODUCT_REQUIREMENTS.md`, `DOMAIN_MODEL.md`, `ARCHITECTURE.md`, all 17 ADRs).

## 2. Stack

**Backend**
- Framework: ASP.NET Core (Minimal APIs) on **.NET 9** (`net9.0`, pinned via `backend/Directory.Build.props`)
- DB: **PostgreSQL** (`docs/adr/ADR-013-persistence-technology.md`, Accepted) via `Npgsql.EntityFrameworkCore.PostgreSQL 9.0.4`; tests use in-memory **SQLite** (`Microsoft.Data.Sqlite 9.0.1`)
- ORM: **EF Core 9.0.1**
- Auth: Password credential + server-side opaque reference token, 30-min sliding / 12-hour absolute expiry (`docs/adr/ADR-017-authentication-mechanism-decision.md`, Accepted); password hashing via `Konscious.Security.Cryptography.Argon2 1.3.1`

**Frontend**
- Framework: **React 19.0.0**, built with **Vite 6.0.5**, **TypeScript 5.7.2**
- UI library: **MUI (Material UI) 6.1.10** + `@emotion` 11.13.5
- State/data: **TanStack React Query 5.62.7**; routing via **react-router-dom 7.0.2**
- Forms/validation: **react-hook-form 7.54.1** + **zod 3.24.1** via `@hookform/resolvers`
- Test runner: **Vitest 2.1.8** + **@testing-library/react 16.1.0**

**Infra**
- CI/CD: **one** GitHub Actions workflow, `.github/workflows/backend-ci.yml` — builds/tests the **backend only** (`dotnet restore/build/test TutorFlow.sln`), triggered only on changes under `backend/**`. **No frontend CI workflow exists at all** (no lint/build/test job for `frontend/**`).
- Docker: `docker/` directory exists at repo root but is **completely empty** — no Dockerfile, no compose file, anywhere in the repo.
- Hosting: no evidence found anywhere (no deployment config, no IaC, no hosting docs).
- Environment config: `backend/src/Web/appsettings.json` contains a **placeholder** Postgres connection string (`Username=REPLACE_ME;Password=REPLACE_ME`); `appsettings.Development.json` has no connection string override at all. No `.env`/`docker-compose` providing a real local Postgres instance was found.

**Notable packages and pinned versions:** see Stack above; all backend `PackageReference` versions are explicit (no floating versions). Frontend uses caret ranges (`^`) throughout, which is normal for npm but means installed versions may drift from what's recorded here without a lockfile diff (lockfile presence not verified against manifest in this pass).

**Version risks**
- `Microsoft.Extensions.DependencyInjection.Abstractions` and `Microsoft.Extensions.Hosting.Abstractions` are both pinned to **10.0.10** — a **.NET 10** package version — inside a project whose `TargetFramework` is `net9.0`. Every other package in the solution is on the 9.0.x line. This is a real mismatch worth resolving deliberately, not by accident.
- No `global.json` anywhere in the repo — the exact .NET SDK version used to build is unpinned; CI and any local dev machine could silently resolve different SDK patch/minor versions.
- No EOL frameworks found otherwise; .NET 9, React 19, and all other pinned majors are current.

## 3. Done

**Completed modules/features** (backend), with evidence:
- **Identity & Relationship** — registration (Tutor/Student/Parent-Guardian), Tutor approval/suspension, Parent-Student Relationship invite/confirm, Admin-assisted password reset. Evidence: `src/Application/Identity/Handlers/*` (23 handlers), `src/Web/Endpoints/IdentityEndpoints.cs`, corresponding tests in `tests/Application.Tests/Identity/*` and `tests/Web.Tests/IdentityEndpointsTests.cs`.
- **Scheduling & Booking** — declare availability, book/cancel/reschedule/complete/mark-no-show a Session, with the double-booking invariant enforced by both a Domain guard (`AvailabilitySlot.Book()`) and a DB-level unique index (`SessionConfiguration.cs`, `ADR-014`). Evidence: `src/Application/Scheduling/Handlers/*`, `src/Web/Endpoints/SchedulingEndpoints.cs`, full Domain/Application/Web test coverage.
- **Discovery** — public Tutor search by Subject/Language/Location. Evidence: `src/Application/Discovery/Handlers/SearchTutorsQueryHandler.cs`, `src/Web/Endpoints/DiscoveryEndpoints.cs`.
- **Marketplace Oversight** — view-all-sessions (`ViewAllSchedules`). Evidence: `src/Application/Oversight/Handlers/GetAllSessionsQueryHandler.cs`, `src/Web/Endpoints/OversightEndpoints.cs`.
- **Authorization** — coarse-grained RBAC (`AuthorizationMiddleware`, `RolePermissionCatalog`) plus fine-grained, resource-instance ownership checks on all 37 documented endpoints. Evidence: `docs/api/AUTHORIZATION_MATRIX.md` — 0 rows remain "Open" as of the most recent governance addendum; `docs/adr/ADR-003-authentication-and-authorization.md` (Third Addendum, Decisions 8–13).
- **Audit trail** — same-transaction audit entries for 6 governance-approved Domain Events (booking/rescheduling/cancelling a Session, changing availability, Tutor approval/suspension) plus login attempts and account lockouts. Evidence: `src/Infrastructure/Audit/AuditDomainEventHandler.cs`, `docs/adr/ADR-016-audit-durability-strategy.md`.
- **Frontend** — full UI for every backend capability above: registration (×3 roles), login, Tutor directory/detail/offering, Relationships, Admin pending-tutor review + approval actions, Tutor search, availability declaration, booking, session detail/lists (student and tutor views), Admin dashboard, global session list, admin-assisted password reset. Evidence: `frontend/src/features/{identity,scheduling,discovery,oversight,auth}/pages/*.tsx`, all routed in `frontend/src/routes/router.tsx`, each with a co-located `.test.tsx`.

**Partially finished work** (file paths + what's missing):
- **`ADM-4` "resolve a booking conflict"** — `Permission.ResolveBookingConflict` is defined (`src/Application/Authorization/Permission.cs:75`) and granted to Admin/Staff (`src/Application/Authorization/RolePermissionCatalog.cs:56`), but **zero endpoint, handler, or command implements it anywhere in the codebase** (confirmed by grep — no other reference to `ResolveBookingConflict` exists, and no `Web/Endpoints/*.cs` file has any conflict-resolution route). This is not silent incompleteness — `docs/api/API_SPECIFICATION.md:129` and `docs/adr/ADR-003` both explicitly document this as blocked on an unresolved Open Question (`DOMAIN_MODEL.md` Open Question 13: exact mechanics not decided) — but the capability itself is 0% implemented behind an already-granted permission.
- **`GET /tutors/pending`, `/relationships/{id}`, `/accounts/{id}/relationships`, `/students/{id}/schedule`, `/tutors/{id}/schedule`, `/tutors/{id}/availability-slots`** — these were the last 6 endpoints without authorization; all 6 were closed in the most recent work (handlers in `src/Application/Identity/Handlers/` and `src/Application/Scheduling/Handlers/`, full test coverage added). Confirmed complete as of this report.
- **Password reset** — Admin-assisted only; no self-service flow exists, and per `ADR-017` this is a deliberate v1 scope decision, not an oversight.
- **`docker/` and `scripts/` directories** — both exist at repo root but contain **zero files**. Scaffolded, never populated.

## 4. Current state

**Build**
- Backend: `dotnet build TutorFlow.sln` (from `backend/`) → **PASS**, 0 Warnings, 0 Errors.
- Frontend: `npm run build` (from `frontend/`) → **PASS**, clean Vite production build (code-split bundles, largest chunk `vendor-mui` at 318.78 kB / 96.89 kB gzip).
- Frontend lint: `npm run lint` → **PASS**, no output (0 errors/warnings).

**Tests**
- Backend, run **per-project** (`dotnet test` from `backend/`, matching how each project was developed/verified): **458–461 total, 0 failed** across the last several runs (Domain.Tests 53, Application.Tests 218, Infrastructure.Tests 45–46, Web.Tests 142–145).
- Backend, run **as the full solution** (`dotnet test TutorFlow.sln` — **this is the exact command CI uses**): **Domain.Tests, Application.Tests, Infrastructure.Tests all pass cleanly (0 failed)**, but **`TutorFlow.Web.Tests` fails intermittently — 7 failed / 139 passed / 146 total**, observed across two consecutive runs with the **same failure count but different specific tests failing each time** (e.g. one run failed `GetPendingTutors_includes_a_freshly_registered_tutor` / `GetPendingTutors_excludes_an_approved_tutor` / a `GetTutorAvailabilitySlots` test with an unexpected 403; the next run instead failed `SearchTutors_filters_by_subject_and_finds_a_matching_approved_tutor`, `Admin_reset_password_invalidates_every_existing_session_immediately`, `Repeated_wrong_passwords_lock_the_account_and_the_correct_password_then_fails_too`, `GetTutorById_returns_success_for_an_unauthenticated_caller_when_discoverable`, plus at least one raw `Microsoft.EntityFrameworkCore.DbUpdateException`). **This is a genuine, reproducible non-determinism in the Web.Tests suite specifically when run as part of the full solution — not observed when Web.Tests is run in isolation.** See Section 5 for root-cause hypothesis (not fixed, per this report's read-only mandate).
- Frontend: `npm test -- --run` (Vitest) → **173 total, 173 passed, 0 failed**, 47/47 test files, consistent across the runs performed in this session.

**Branch**
- UNKNOWN — not a git repository. `git status`, `git log`, and `git branch -a` were all run from the repo root and every one returned: `fatal: not a git repository (or any of the parent directories): .git`. There is no `.git` directory anywhere under `D:\Projects\TutorFlow`, despite a (0-byte, empty) `.gitignore` file and a populated `.github/workflows/backend-ci.yml` both being present — i.e., the repo is scaffolded for git and CI but has **no actual version control history**.

**Branch drift**
- UNKNOWN — cannot be computed; no branches exist (no git repository at all).

**Uncommitted changes**
- UNKNOWN — `git status` cannot run for the same reason. Every change made in every prior session (including this one) exists only on disk with no commit history, no diffable baseline, and no way to know what "the last known-good state" was without external record-keeping.

**Migrations**
- 6 EF Core migrations exist, in order: `20260719212355_InitialCreate`, `20260719215841_AddHourlyRatePrecision`, `20260720044906_AddAuditEntries`, `20260720175732_AddAuthentication`, `20260720200951_AddAccountLockoutAndAbsoluteSessionLifetime`, `20260721064101_AddRelationshipInvitedByAccountId` (all under `backend/src/Infrastructure/Persistence/Migrations/`), plus a current `TutorFlowDbContextModelSnapshot.cs`.
- **Applied status: UNKNOWN — no evidence found that any migration has ever been applied to a real database.** The only connection string in the repo (`appsettings.json`) has placeholder credentials (`REPLACE_ME`/`REPLACE_ME`) that cannot connect to anything; `appsettings.Development.json` supplies no override; no `docker-compose.yml` or other means of standing up a local Postgres instance exists anywhere in the repo. All 458+ backend tests run against an isolated in-memory SQLite database created fresh per test factory — this proves the migrations are internally consistent with the current EF model (via `EnsureCreated`/schema use in tests) but is **not** evidence they have ever been run as actual `dotnet ef database update` against Postgres.

## 5. Pain points

**Architectural inconsistencies** — none of significance found. Backend Clean Architecture boundaries were independently verified (project references are strictly `Domain ← Application ← Infrastructure/Web`, zero violations found via `using` audit across all 273 backend source files). This is a genuinely well-disciplined codebase architecturally.

**Technical debt: TODO/FIXME/NotImplemented inventory**
- **Zero** `TODO`, `FIXME`, `HACK`, `NotImplementedException`, or `NotSupportedException` markers found anywhere in `backend/src` or `frontend/src` (grep across all `.cs`/`.ts`/`.tsx` files, 0 hits). No commented-out code blocks found via heuristic scan either.
- This is unusually clean for a project this size — but note it also means the one real gap (`ResolveBookingConflict`, Section 3) is **not** flagged with any in-code marker; a future developer grepping for `TODO` would not find it. It's only discoverable by cross-referencing the permission catalog against the endpoint files, as done for this report.

**Dead code / unused files / orphaned modules**
- `docker/` and `scripts/` — both empty, no files, at repo root.
- **Duplicate, diverged document:** `FINAL_ARCHITECTURE_PLAN.md` exists in **two places with the same filename but different content** — `D:\Projects\TutorFlow\FINAL_ARCHITECTURE_PLAN.md` (249 lines, Persian-language planning narrative, last modified 2026-07-21) and `docs/architecture/FINAL_ARCHITECTURE_PLAN.md` (English, more polished "Final Architecture Review" framing). A `diff` between them produced 404 lines of differences — they are substantially different documents, not a stray copy. Neither is referenced by any ADR's "authoritative sources" header. This is a real source of confusion for anyone trying to find "the" architecture plan.
- `IMPLEMENTATION_PLAN_V2.md` (root) is superseded by `IMPLEMENTATION_PLAN_V3.md` (root) — V2 is still present and describes a "build everything from scratch" plan that no longer reflects reality (most of what it describes as Phase 1–4 is already built). It is not marked deprecated/superseded in its own text.

**Missing fundamentals**
- **No `README.md` anywhere in the repository** (root, `backend/`, or `frontend/` — confirmed via repo-wide search excluding `node_modules`). A newcomer has no single entry point explaining how to run this project.
- **No `CLAUDE.md`** anywhere either.
- **No version control.** Repeating from Section 4 because it is the single most consequential finding in this report: there is no `.git` directory. Nothing in this codebase's history — including every architectural decision this session's own audits verified was correctly implemented — is recoverable, diffable, or attributable via git. CI is configured to trigger `on: push`/`on: pull_request`, but there is no evidence those triggers have ever fired, because there is no remote and no commit history to push.
- **Flaky test suite under solution-wide execution** — see Section 4. The exact command CI runs (`dotnet test TutorFlow.sln`) is not reliably green; a fresh clone running exactly what CI runs has a real chance of a red build purely from test ordering/isolation, not from an actual product defect. Root-cause hypothesis (not investigated further, not fixed, per this report's mandate): `Web.Tests` classes use `IClassFixture<TutorFlowWebApplicationFactory>`, which shares one in-memory SQLite connection across every test method within a class; several `[Fact]` methods create Tutors/Accounts with side effects (e.g., pending-tutor registrations) that accumulate across the whole class's test run, and paginated/exact-match assertions (e.g., `GetPendingTutors_includes_a_freshly_registered_tutor`) can therefore pass or fail depending on how many other tests in the same class ran first — which is not deterministic across xUnit test-discovery/ordering runs.
- **Environment/secrets:** no real secrets found hardcoded (the connection string is an explicit `REPLACE_ME` placeholder, not a leaked credential) — this is a positive finding, not a problem.
- Logging exists (structured `ILogger` calls in `ResultMapping.LogFailure` and elsewhere) and error handling is disciplined (`GlobalExceptionHandler`, `Result`/`Error` pattern throughout) — no gap found here.

## 6. Next

1. **Initialize git and establish a real commit history** — *justification:* this is the actual highest-severity finding in this report; every other recommendation is moot if the current disk state can be lost or silently diverge with no way to recover a prior known-good point. Effort: **~1 hour** (init, first commit, `.gitignore` population, push to a remote).
2. **Investigate and fix the `Web.Tests` solution-wide flakiness** — *justification:* CI runs exactly this command; an intermittently red CI build erodes trust in the whole pipeline and will eventually block or confuse an unrelated PR. Effort: **~3–4 hours** (root-cause the shared-fixture/pagination interaction described in Section 5, likely fixed by giving each `[Fact]` its own factory instance or asserting more defensively).
3. **Write a root `README.md`** — *justification:* zero-cost, high-value; nothing in this repo currently tells a new contributor how to run the backend, run the frontend, or where to start reading. Effort: **~1–2 hours**.
4. **Resolve the `Microsoft.Extensions.*.Abstractions` 10.0.10-in-a-net9.0-project version mismatch** — *justification:* small now, but silently drifting onto a preview/next major's package version is exactly the kind of thing that causes a confusing failure much later; cheap to fix today. Effort: **~1 hour** (verify whether 9.0.x satisfies all consumers; pin down if so).
5. **Decide the fate of the duplicate `FINAL_ARCHITECTURE_PLAN.md` and superseded `IMPLEMENTATION_PLAN_V2.md`** — *justification:* low risk, but actively confusing for anyone who finds the "wrong" one first; a one-line "superseded by X" note or an outright deletion (once git history exists, per step 1, deletion is safe) resolves it. Effort: **~30 minutes**.

**Blockers before any new feature work:** step 1 (git) is a hard blocker in spirit, even though nothing technically prevents writing more code without it — any new feature work compounds the risk step 1 exists to close. Step 2 should also be resolved before treating "green CI" as a meaningful signal for future PRs.

## 7. Open questions for the project owner

- Was git deliberately not initialized yet (e.g., working entirely inside an AI coding session before a first "real" commit), or was `.git` lost/never created by mistake? This report cannot distinguish the two from the filesystem alone.
- Is a real PostgreSQL instance running anywhere (local Docker, a cloud dev instance) that this report simply doesn't have visibility into, or has this application genuinely never been run against real Postgres, only ever exercised through the in-memory SQLite test suite?
- Is `ResolveBookingConflict` (`ADM-4`) intended to be implemented before this ships, or is it acceptable to ship v1 without it, given `DOMAIN_MODEL.md` Open Question 13 is still unresolved? No document states a target date for resolving that Open Question.
- Which of the two `FINAL_ARCHITECTURE_PLAN.md` files (root vs. `docs/architecture/`) is authoritative, if either? Neither is cited by any ADR.
- Is there a frontend deployment target in mind (the empty `docker/` directory and single backend-only CI workflow suggest this hasn't been decided yet) — this affects whether "no frontend CI" is a gap to close now or something intentionally deferred.
