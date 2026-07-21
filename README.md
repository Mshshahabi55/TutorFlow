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

This runs all four test projects (Domain.Tests, Application.Tests, Infrastructure.Tests, Web.Tests). Across the runs performed in this session, results ranged from a clean **458–461/461, 0 failed** to an intermittent **`TutorFlow.Web.Tests` failure — ~7 failures out of 146**, with the specific failing tests varying between runs.

> **Known issue:** `TutorFlow.Web.Tests` is genuinely flaky — confirmed to fail intermittently under **both** `dotnet test` (per-project) and `dotnet test TutorFlow.sln` (the exact command CI uses), not only the latter. Domain.Tests, Application.Tests, and Infrastructure.Tests were never observed to fail in this session. This looks like shared-fixture state leaking across `[Fact]` methods within the same Web.Tests class (`IClassFixture<TutorFlowWebApplicationFactory>` shares one in-memory database across every test method in a class), surfacing non-deterministically depending on test execution order. **Do not treat a single green `dotnet test` run as proof the suite is healthy — re-run at least twice before trusting a result.** Not yet root-caused or fixed — tracked for a future phase. See `docs/phases/PHASE-00-REPORT.md`.

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

## Current status

**What works:** the full v1 feature set described in `PROJECT_CONSTITUTION.md` — registration for all three self-registering roles, Tutor approval/suspension, Parent-Student relationships, Tutor search/discovery, availability declaration, booking/cancel/reschedule/complete/no-show, coarse- and fine-grained authorization on all 37 documented endpoints (0 rows remain "Open" in `AUTHORIZATION_MATRIX.md`), and an audit trail for governance-approved events. Backend: Domain/Application/Infrastructure tests are consistently green (53 + 218 + 45–46); Web.Tests is functionally complete but **intermittently flaky** (see "Running the tests" above — do not trust a single run). Frontend: 173 tests passing, full UI for every capability above.

**What has never been run against a real database:** everything. All backend tests run against an isolated in-memory SQLite instance created fresh per test run (`tests/Web.Tests/TutorFlowWebApplicationFactory.cs`). No PostgreSQL instance, Docker Compose file, or connection string with real credentials exists anywhere in this repo as of this commit. The 6 EF Core migrations under `backend/src/Infrastructure/Persistence/Migrations/` are internally consistent with the current model (proven by the SQLite-backed test suite) but have no evidence of ever being applied via `dotnet ef database update` against a real Postgres instance.

**What is not implemented:** `ADM-4` ("Admin can resolve booking conflicts"). The `Permission.ResolveBookingConflict` value is defined and granted to Admin/Staff in `RolePermissionCatalog.cs`, but no endpoint, handler, or command implements it anywhere. This is a deliberate, documented gap — `DOMAIN_MODEL.md` Open Question 13 (exact conflict-resolution mechanics) is still unresolved — not an oversight.

Full detail: `PROJECT-STATUS.md` (repo root) and `docs/phases/PHASE-00-REPORT.md`.
