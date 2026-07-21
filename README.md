# TutorFlow

## What TutorFlow is

TutorFlow is a multi-sided scheduling and booking marketplace connecting Students, Tutors, and Parents/Guardians, with Admin/Staff oversight. Its core mission is a reliable, trustworthy scheduling foundation: no session is ever double-booked, lost, or left in an inconsistent state, and every role reads one non-contradictory view of the same schedule data. It is explicitly scoped to scheduling and booking only — it does not process payments, host session delivery (video/messaging), or track learning progress. This is v1 of a real product, not a demo or prototype.

Source: `PROJECT_CONSTITUTION.md` (Mission, Product Goals, Project Scope).

## Scope and Regional Constraints

TutorFlow v1 targets **Iran only** — English-only UI (no i18n), `Asia/Tehran` (UTC+03:30, no DST) as the operating timezone, a single currency (Rial/Toman), and hosting inside Iran. No payment module exists in v1; when one is built, it must use an Iranian PSP under a redirect + callback + server-side-verify model. The system must never depend on Stripe, PayPal, Twilio, SendGrid, or any other US-hosted cloud service. v1 is Iran-first, not Iran-locked — see `docs/adr/ADR-018-regional-deployment-and-market-scope.md` for the full decision and the specific seams (currency, timezone conversion, payment provider, notification provider) that must stay abstract for v2.

## Architecture at a glance

Backend is a Clean Architecture / Modular Monolith, partitioned into four bounded contexts (Identity & Relationship, Scheduling & Booking, Discovery, Marketplace Oversight). Layer dependency direction is strict and enforced by project references:

```
Domain  ←  Application  ←  Infrastructure / Web
```

Domain has zero outward dependency. Application depends only on Domain. Infrastructure and Web both depend on Application; Web additionally depends on Infrastructure to wire up dependency injection. Never the reverse.

Full rationale: `docs/adr/` (17 accepted ADRs). Frontend is a standard feature-sliced React app (`frontend/src/features/{identity,scheduling,discovery,oversight,auth}`) consuming the backend over HTTP.

## Prerequisites

Exact versions verified in this session's environment:

- **.NET SDK 9.0.x** — this repo targets `net9.0` (`backend/Directory.Build.props`); verified working with SDK `9.0.300`.
- **Node.js** — no strict minimum is pinned anywhere in the repo (no `.nvmrc`, no `engines` field in `package.json`); verified working with Node `v24.13.0` in this session. `@types/node` is pinned to `^22.10.2`, so Node 22+ is the safest assumption if you're not on 24.
- **PostgreSQL** — required for real backend usage (`docs/adr/ADR-013-persistence-technology.md`). No specific version is pinned by any project document. **Not verified in this session** — no local Postgres instance was available; every backend test in this repo currently runs against an isolated in-memory SQLite database instead. See "Current status" below.

## Running the backend

```bash
cd backend
dotnet restore TutorFlow.sln
dotnet build TutorFlow.sln
```

To actually run the API against a real database, you first need a PostgreSQL instance and a real connection string — `backend/src/Web/appsettings.json` ships with a **placeholder** (`Username=REPLACE_ME;Password=REPLACE_ME`) that will not connect to anything. Set your real connection string via user-secrets or an environment variable, never by editing `appsettings.json` directly (see `CLAUDE.md`). This has not been exercised in this session — no `dotnet run` or `dotnet ef database update` was performed.

## Running the frontend

```bash
cd frontend
npm install
npm run dev      # local dev server
npm run build    # production build
npm run lint      # ESLint
```

By default the frontend points at `http://localhost:5046` for the API (`frontend/src/services/api/apiClient.ts`); override with `VITE_API_BASE_URL` if your backend runs elsewhere.

## Running the tests

**Backend** — from `backend/`:

```bash
dotnet test
```

This runs all four test projects (Domain.Tests, Application.Tests, Infrastructure.Tests, Web.Tests) — **478/478, 0 failed**, confirmed across 10 consecutive runs (`docs/phases/PHASE-01B-REPORT.md`).

> **Resolved:** `TutorFlow.Web.Tests` was previously observed to fail intermittently (~7 failures out of 146, varying between runs). This turned out to be two separate, unrelated problems, both now fixed — see `docs/phases/PHASE-01-REPORT.md` and `docs/phases/PHASE-01B-REPORT.md` for the full investigation: (1) one genuinely order-dependent test, caused by `IClassFixture<TutorFlowWebApplicationFactory>` sharing one in-memory database across every test method in a class (fixed by resetting the schema before each `[Fact]`); (2) six deterministic failures — not flakiness at all — caused by the persistence defect described in "Known pitfalls" below.

**Frontend** — from `frontend/`:

```bash
npm test -- --run
```

Verified to pass **173/173, 0 failed** across 47 test files in this session.

## Project layout

```
backend/            .NET 9 solution — Domain, Application, Infrastructure, Web + one test project per layer
frontend/            React 19 + Vite + TypeScript app, feature-sliced under src/features/
docs/adr/            17 Architecture Decision Records — the detailed "why" behind every structural choice
docs/api/            AUTHORIZATION_MATRIX.md (endpoint-by-endpoint auth rules) and API_SPECIFICATION.md
docs/database/       Logical/physical database design notes
docs/architecture/   Contains its own copy of FINAL_ARCHITECTURE_PLAN.md — see Open Decisions in PHASE-00-REPORT.md
docs/phases/         Phase-gated stabilization/hardening reports (this file's sibling)
docker/              Reserved for future containerization — currently empty by design (see .gitkeep)
scripts/             Reserved for future dev/ops scripts — currently empty by design (see .gitkeep)
.github/workflows/   CI — currently backend-only; no frontend CI job exists yet
```

Root-level `.md` files (`PROJECT_CONSTITUTION.md`, `PRODUCT_REQUIREMENTS.md`, `DOMAIN_MODEL.md`, `ARCHITECTURE.md`, `BUSINESS_MODEL.md`) are the original product/architecture planning documents, in that priority order. `IMPLEMENTATION_PLAN_V2.md`, `IMPLEMENTATION_PLAN_V3.md`, and the root `FINAL_ARCHITECTURE_PLAN.md` are planning/status artifacts from the project's history — see "Where the documentation lives" and `PHASE-00-REPORT.md` for which are current.

## Where the documentation lives

`PROJECT_CONSTITUTION.md` is the single, immutable top authority for this project — every other document is required to trace back to it. Below that, in descending priority:

1. `PROJECT_CONSTITUTION.md` — immutable, top authority
2. `docs/adr/` — Architecture Decision Records, the binding technical decisions
3. `PRODUCT_REQUIREMENTS.md`
4. `DOMAIN_MODEL.md`
5. `ARCHITECTURE.md`
6. The code itself

`docs/api/AUTHORIZATION_MATRIX.md` is the single reference for what every HTTP endpoint requires to be called. See `CLAUDE.md` for the full precedence rule and non-negotiable rules for future work in this repo.

## Known pitfalls

**`.AsNoTracking()` on a command path silently discards writes — and nothing in the Application-layer tests can catch it.** `EfUnitOfWork.SaveChangesAsync` (`backend/src/Infrastructure/Persistence/EfUnitOfWork.cs`) takes the aggregates a handler touched and calls `_dbContext.SaveChangesAsync()` — it never calls `_dbContext.Update(aggregate)` or otherwise re-attaches anything. If a repository method fetched that aggregate with `.AsNoTracking()`, EF Core's change tracker never saw it in the first place, so mutating it in memory and "saving" produces **no SQL `UPDATE` at all** — the handler still returns `Result.Success()`, because the in-memory mutation and the success `Result` both happen before the no-op save.

This is exactly what happened to `TutorRepository.GetByIdAsync`/`GetByEmailAsync` (found in `docs/phases/PHASE-01-REPORT.md`, fixed in `docs/phases/PHASE-01B-REPORT.md`): `Approve`, `Suspend`, `SetHourlyRate`, `SetSubject`, `SetLanguage`, `SetLocation`, `SetOfferedDurations`, `Login`'s lockout tracking, and `AdminResetPassword` (for a Tutor target) all fetched a `Tutor` this way, mutated it, and silently never saved the change — while every call still returned success.

**Why `Application.Tests` never caught it:** those tests run against `FakeUnitOfWork` (`backend/tests/Application.Tests/TestDoubles/FakeUnitOfWork.cs`), a hand-rolled double that only increments a call counter and clears domain events — it does not simulate EF Core's change-tracking semantics, so it cannot tell the difference between "this mutation will persist" and "this mutation is about to vanish." `Web.Tests` was the only test project exercising the real `EfUnitOfWork` against a real EF Core provider, and its failures were dismissed as flakiness for a full phase before being root-caused.

**The rule going forward** (also in `CLAUDE.md`'s non-negotiable rules): a test that asserts a write succeeded must re-read the entity from a fresh `DbContext`/scope and check the persisted state — not just that `SaveChangesAsync` was called, and not from the same context/response body the write used. And `.AsNoTracking()` belongs only on repository methods whose result is never mutated and saved afterward.

## Current status

**What works:** the full v1 feature set described in `PROJECT_CONSTITUTION.md` — registration for all three self-registering roles, Tutor approval/suspension, Parent-Student relationships, Tutor search/discovery, availability declaration, booking/cancel/reschedule/complete/no-show, coarse- and fine-grained authorization on all 37 documented endpoints (0 rows remain "Open" in `AUTHORIZATION_MATRIX.md`), and an audit trail for governance-approved events. Backend: all four projects (Domain/Application/Infrastructure/Web) are consistently green — 53 + 218 + 46 + 161, confirmed over 10 consecutive runs (see "Running the tests" above). Frontend: 173 tests passing, full UI for every capability above.

**What has never been run against a real database:** everything. All backend tests run against an isolated in-memory SQLite instance created fresh per test run (`tests/Web.Tests/TutorFlowWebApplicationFactory.cs`). No PostgreSQL instance, Docker Compose file, or connection string with real credentials exists anywhere in this repo as of this commit. The 6 EF Core migrations under `backend/src/Infrastructure/Persistence/Migrations/` are internally consistent with the current model (proven by the SQLite-backed test suite) but have no evidence of ever being applied via `dotnet ef database update` against a real Postgres instance.

**What is not implemented:** `ADM-4` ("Admin can resolve booking conflicts"). The `Permission.ResolveBookingConflict` value is defined and granted to Admin/Staff in `RolePermissionCatalog.cs`, but no endpoint, handler, or command implements it anywhere. This is a deliberate, documented gap — `DOMAIN_MODEL.md` Open Question 13 (exact conflict-resolution mechanics) is still unresolved — not an oversight.

Full detail: `PROJECT-STATUS.md` (repo root) and `docs/phases/PHASE-00-REPORT.md`.
