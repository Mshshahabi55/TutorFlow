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
- **PostgreSQL 17** — required for real backend usage (`docs/adr/ADR-013-persistence-technology.md`, addendum). Verified against 17.10, installed natively. A dev/CI-only Docker alternative also exists (`.devcontainer/` — see the banner comments in that directory for scope/limits); it is not the primary documented path below, and hasn't been build-verified in every environment. Every backend *test* still runs against an isolated in-memory SQLite database (`tests/Web.Tests/TutorFlowWebApplicationFactory.cs`); PostgreSQL is required only to run the API itself and the Postgres-backed integration tests (`docs/phases/PHASE-02-REPORT.md`).

## Database setup

A new machine needs a running PostgreSQL 17 server and two databases:
`tutorflow_dev` (the API's own database) and `tutorflow_test` (used only by
the Postgres-backed integration tests in `backend/tests/Infrastructure.Tests/Postgres/`).

1. Create both databases (adjust `-U`/`-h` to match your install):
   ```bash
   psql -U postgres -h localhost -c "CREATE DATABASE tutorflow_dev;"
   psql -U postgres -h localhost -c "CREATE DATABASE tutorflow_test;"
   ```
2. Initialize user-secrets for the Web project (one-time per checkout — adds
   only a `<UserSecretsId>` to `TutorFlow.Web.csproj`, never a credential):
   ```bash
   cd backend/src/Web
   dotnet user-secrets init
   ```
3. Store your real connection string as a secret — **never** in
   `appsettings.json`, which must keep its `REPLACE_ME` placeholder:
   ```bash
   dotnet user-secrets set "ConnectionStrings:TutorFlow" "Host=localhost;Database=tutorflow_dev;Username=postgres;Password=<your-password>"
   ```
4. The Postgres-backed integration tests (Task 4, `docs/phases/PHASE-02-REPORT.md`)
   read their own connection string from the `TUTORFLOW_TEST_CONNECTION`
   environment variable, pointed at `tutorflow_test` — set it in your shell
   profile, not committed anywhere:
   ```bash
   export TUTORFLOW_TEST_CONNECTION="Host=localhost;Database=tutorflow_test;Username=postgres;Password=<your-password>"
   ```
   If this variable is unset, those tests fail loudly with a clear message
   rather than silently skipping (`docs/phases/PHASE-02-REPORT.md` Section 4).
5. Apply migrations (see "Running the backend" below).

## Running the backend

```bash
cd backend
dotnet restore TutorFlow.sln
dotnet build TutorFlow.sln
```

To run the API against a real database: complete "Database setup" above,
then apply migrations and run:

```bash
cd backend/src/Web
ASPNETCORE_ENVIRONMENT=Development dotnet ef database update
ASPNETCORE_ENVIRONMENT=Development dotnet run
```

`appsettings.json` ships with a **placeholder** connection string
(`Username=REPLACE_ME;Password=REPLACE_ME`) that will not connect to
anything by design — the real one comes from user-secrets, loaded only in
the `Development` environment. Verified working end-to-end in
`docs/phases/PHASE-02-REPORT.md`: migrations applied cleanly against
PostgreSQL 17.10, the API starts with no errors (one benign
`Failed to determine the https port for redirect.` warning, expected with
no HTTPS launch profile configured), and `GET /health` returns `Healthy`.
There is currently only one health endpoint — no separate `/health/ready`.

### Development seed data

In the `Development` environment only, the API seeds a minimal dataset on
startup if it isn't already there (idempotent — safe to restart repeatedly):
one Admin/Staff account, one approved Tutor with an offering and an open
Availability Slot, one Student, and one Parent/Guardian with a confirmed
Relationship to that Student (`backend/src/Web/DevelopmentSeeder.cs`).

The Admin account's password is never hardcoded — set it before running:

```bash
cd backend/src/Web
dotnet user-secrets set "Seed:AdminPassword" "<your-dev-password>"
# or: export Seed__AdminPassword="<your-dev-password>"
```

If it isn't configured, startup fails immediately with a clear error
rather than falling back to a default password. The Tutor/Student/
Parent-Guardian demo accounts share one fixed password
(`Seed-Password-123!`, in `DevelopmentSeeder.cs`) — they exist only for
local manual testing, not to hold anything sensitive.

### Meeting providers (online lesson delivery)

`Development` defaults to the Mock provider (`appsettings.Development.json`'s
`Meeting:DefaultProvider`) — "Start Lesson" works immediately with no setup,
returning an obviously-fake `mock-meeting.tutorflow.dev` link, never a real
one (`docs/adr/ADR-023-online-lesson-meeting-provider-architecture.md`).

To exercise a real provider instead, set its own section via user-secrets
(never `appsettings.json`):

```bash
dotnet user-secrets set "Meeting:Google:ClientId" "<...>"
dotnet user-secrets set "Meeting:Google:ClientSecret" "<...>"
dotnet user-secrets set "Meeting:Google:RefreshToken" "<...>"
# Microsoft:TenantId/ClientId/ClientSecret/OrganizerUserId, Zoom:AccountId/ClientId/ClientSecret — same pattern
```

An unconfigured provider never fabricates a meeting link — "Start Lesson"
fails with an honest "Provider not configured" error instead.

### Rate limiting

Every request is throttled per client IP (`RateLimiting:General`, default
100 requests / 10s); `POST /auth/login` additionally requires clearing a
much tighter `RateLimiting:Auth` budget (default 5 attempts / 60s) — the
one anonymous, credential-guessing-shaped endpoint in this API. A
throttled request gets `429 Too Many Requests`, a `Retry-After` header, and
the same `ApiResponse` envelope every other error uses. Both limits are
config-driven (`appsettings.json`) so they can be retuned per environment
without a code change.

### Booking notice & horizon

A Session may only be booked (or rescheduled) at least 24 hours before its
start (`SchedulingConstraints:MinimumBookingNoticeHours`, default 24) and no
more than 90 days before its start (`SchedulingConstraints:MaximumBookingHorizonDays`,
default 90), platform-wide — resolved 2026-07-29, see
`docs/adr/ADR-025-scheduling-recurring-availability-and-booking-constraints.md`'s
Addendum. Both limits are enforced in `BookSessionCommandHandler` and
`RescheduleSessionCommandHandler` (Application layer, against the current
instant) — not in `AvailabilitySlot`/`Session` (Domain), since a booking-time
policy check against "now" isn't a fact either aggregate can express from its
own data alone. A violation returns `400 Bad Request` with error code
`BookSessionCommand.BookingTooSoon`/`.BookingTooFarAhead` (or the
`RescheduleSessionCommand.*` equivalents). Config-driven, same pattern as
Rate limiting above, so either limit can be retuned per environment without
a code change.

### Startup configuration validation

Rate-limiting, Meeting-provider, and Scheduling-constraints settings are
validated when the host starts (`ValidateOnStart`), not lazily on first use —
a zero/negative `PermitLimit`/`WindowSeconds`, a `Meeting:DefaultProvider`
value that isn't a recognized provider name, a zero/negative
`MinimumBookingNoticeHours`/`MaximumBookingHorizonDays`, or a notice floor
that meets or exceeds the horizon ceiling, all fail the host immediately with
a clear message instead of surfacing as an unhandled exception on whatever
request happens to touch it first. Same "fail fast" principle already applied
to a placeholder database connection string.

### Request logging

Every request produces exactly one structured log line — method, path,
status code, elapsed milliseconds, and a `TraceId` — regardless of outcome
(success, a rate-limit rejection, or an unhandled exception). The same
`TraceId` is echoed back as an `X-Trace-Id` response header, so a caller's
own error report can be correlated to the matching server-side log line.
5xx responses log at Warning; everything else logs at Information.

## Running the frontend

```bash
cd frontend
npm install
npm run dev      # local dev server
npm run build    # production build
npm run lint      # ESLint
```

By default the frontend points at `http://localhost:5046` for the API (`frontend/src/services/api/apiClient.ts`); override with `VITE_API_BASE_URL` if your backend runs elsewhere.

## Verifying the build (authoritative)

```powershell
./scripts/verify.ps1
```

Per `docs/adr/ADR-018-regional-deployment-and-market-scope.md`, this project cannot depend on GitHub being reachable, so this script — not `.github/workflows/*.yml` — is the real "is the build green" gate. It runs `dotnet restore` + `dotnet build` (0 warnings required), the backend test suite **twice** (a determinism guard — a suite that only passes once isn't proven green), then `npm ci`, lint, build, and test in `frontend/`, and prints a PASS/FAIL summary with per-step timing. Non-zero exit on any failure. See `docs/phases/PHASE-01C-REPORT.md` for a full run's real output.

## Running the tests

**Backend** — from `backend/`:

```bash
dotnet test
```

This runs all four test projects (Domain.Tests, Application.Tests, Infrastructure.Tests, Web.Tests) — **791/791, 0 failed** (147 + 302 + 72 + 270) as of this commit, confirmed across repeated runs. The 491/53+218+59+161 figures below elsewhere in project history (`docs/phases/PHASE-01B-REPORT.md`) reflect an earlier point in the project's growth, not current counts — re-run `dotnet test` for the authoritative count rather than trusting any number in this file.

> **Resolved:** `TutorFlow.Web.Tests` was previously observed to fail intermittently (~7 failures out of 146, varying between runs). This turned out to be two separate, unrelated problems, both now fixed — see `docs/phases/PHASE-01-REPORT.md` and `docs/phases/PHASE-01B-REPORT.md` for the full investigation: (1) one genuinely order-dependent test, caused by `IClassFixture<TutorFlowWebApplicationFactory>` sharing one in-memory database across every test method in a class (fixed by resetting the schema before each `[Fact]`); (2) six deterministic failures — not flakiness at all — caused by the persistence defect described in "Known pitfalls" below.

**Frontend** — from `frontend/`:

```bash
npm test -- --run
```

721/721 passing (136 test files) as of this commit, run in isolation. Running the full `scripts/verify.ps1` pipeline back-to-back with the backend steps has been observed to occasionally hit vitest's fixed 5000ms per-test timeout on a handful of slower UI tests under machine load right after a fresh `npm ci`/`npm run build` — see `docs/phases/PHASE-01C-REPORT.md` Section 3 for the full characterization (written when the suite was much smaller; the underlying timing sensitivity, not the specific counts, is still the relevant point). Re-running `npm test -- --run` alone reliably passes in full; this looks like environment timing, not a product or test defect.

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

**Tehran's `+03:30` UTC offset is a hardcoded constant (`TEHRAN_OFFSET_MINUTES`, `frontend/src/shared/time/tehranTime.ts`), not a value resolved from `Intl`/the host's IANA timezone database.** That's deliberate — Phase 3's own reasoning was that correctness should never depend on the runtime's tz-data being current — but it is an assumption with a real expiry condition, not a law of physics: Iran has changed this before (DST was abolished only in 2022) and could change its offset again. If that happens, `TEHRAN_OFFSET_MINUTES` is the only place in the codebase that needs to change; every conversion in the module, and every caller across the app, reads through it.

**System.Text.Json's default `DateTime` converter resolves a non-`Z` numeric offset (e.g. `+03:30`) against the server process's own system timezone, not the offset in the string — and produces `Kind=Local`, not `Kind=Utc`.** Confirmed empirically while building Phase 3 (`docs/phases/PHASE-03-REPORT.md` Task 4): `JsonSerializer.Deserialize<DateTime>("\"2026-08-01T14:00:00+00:00\"")` returns a value shifted into whatever timezone the host machine happens to be running in, with `Kind=Local`. Combined with `UtcDateTimeValueConverter`'s `Kind=Local` → throw behavior (`backend/src/Infrastructure/Persistence/UtcDateTimeValueConverter.cs`), an incoming request with an explicit-but-non-`Z` offset would either 500 or silently compute the wrong instant depending on the server's own timezone — neither of which is what the client asked for. `RequireUtcDateTimeJsonConverter` (`backend/src/Web/Json/RequireUtcDateTimeJsonConverter.cs`) replaces the default converter for every JSON-bound `DateTime`/`DateTime?`, parsing via `DateTimeOffset` (anchored to the offset actually present in the string, never the host's) and rejecting any value with no `Z`/offset at all. All times displayed to or entered by a user are Asia/Tehran (UTC+03:30, no DST) — conversion happens only in `frontend/src/shared/time/`, never inline in a component.

**A Rial amount not evenly divisible by 10 cannot be expressed as a whole Toman value.** Discovered building Phase 4 (`docs/phases/PHASE-04-REPORT.md` Section 4/Section 3): `HourlyRate.Of` originally only rejected a *fractional* Rial amount, not one that's merely not a multiple of 10 — so e.g. `45` Rial was a legal `HourlyRate` with no whole-Toman representation (`4.5` Toman), and the frontend's `rialToToman` threw when asked to display one. Phase 4.5 (ADR-019 Addendum 1) moved the invariant to where it belongs: `HourlyRate.Of` (`backend/src/Domain/Identity/ValueObjects/HourlyRate.cs`) now rejects any amount not divisible by 10 at the source, so no conforming write can produce this again; `HourlyRate.MaxAmount` moved from `999,999,999,999` to `999,999,999,990` (the old value was itself illegal under the corrected rule). `frontend/src/shared/money/rial.ts` still has to handle **data written before this invariant existed** — `rialToToman` stays a strict, throwing primitive (a programmer-error guard, never called from a page render path), while `formatToman`/`toTomanInputValue` round to the nearest Toman for display rather than crashing, so a Tutor's profile with a legacy odd-Rial rate remains viewable. The Phase 4 migration's own defensive check (a `DO $$ ... RAISE EXCEPTION` block in `20260722100927_HourlyRateWholeRialPrecision.cs`) found exactly this scenario as leftover data from a prior test run in `tutorflow_test` and refused to silently round it during the schema migration itself — the same "fail loudly, don't invent a rounding rule" instinct that motivated keeping `rialToToman` strict.

## Current status

**What works:** the full RC1 feature set described in `PROJECT_CONSTITUTION.md`/`PRODUCT_REQUIREMENTS.md` — registration for all three self-registering roles, Tutor approval/suspension, Parent-Student relationships, Tutor search/discovery, availability declaration, booking/cancel/reschedule/complete/no-show (including the 24-hour minimum-notice/90-day maximum-horizon constraints), in-platform messaging, third-party online-meeting orchestration, Tutor profile enrichment/onboarding, Admin/Tutor/Student/Parent dashboards, and an audit trail for governance-approved events — with coarse- and fine-grained authorization on all 53 application endpoints (0 rows remain "Open" in `AUTHORIZATION_MATRIX.md`). Backend: all four projects (Domain/Application/Infrastructure/Web) are consistently green — 147 + 302 + 72 + 270 (see "Running the tests" above). Frontend: 721 tests passing, full UI for every capability above.

**What has never been run against a real database:** everything. All backend tests run against an isolated in-memory SQLite instance created fresh per test run (`tests/Web.Tests/TutorFlowWebApplicationFactory.cs`). No connection string with real credentials exists anywhere in this repo as of this commit. The 14 EF Core migrations under `backend/src/Infrastructure/Persistence/Migrations/` are internally consistent with the current model (proven by the SQLite-backed test suite) but have no evidence of ever being applied via `dotnet ef database update` against a real Postgres instance.

**What is not implemented:** `ADM-4` ("Admin can resolve booking conflicts"). The `Permission.ResolveBookingConflict` value is defined and granted to Admin/Staff in `RolePermissionCatalog.cs`, but no endpoint, handler, or command implements it anywhere. This is a deliberate, documented gap — `DOMAIN_MODEL.md` Open Question 13 (exact conflict-resolution mechanics) is still unresolved — not an oversight.

Full detail: `PROJECT-STATUS.md` (repo root) and `docs/phases/PHASE-00-REPORT.md`.
