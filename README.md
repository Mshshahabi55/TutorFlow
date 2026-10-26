# TutorFlow

TutorFlow is a two-sided online tutoring marketplace connecting students with independent
tutors for 1:1 lessons across academic and language subjects — comparable in scope to
Preply (tutor discovery, trial lessons, recurring bookings, subscriptions, messaging,
reviews, tutor payouts), built as a from-scratch implementation, not a clone of Preply's
code, copy, or visual design. See [`docs/product/PRODUCT_VISION.md`](docs/product/PRODUCT_VISION.md)
for the full product vision and target market.

**This is a work in progress, not a production-ready product.** Several core workflows are
implemented end-to-end; others are intentionally stubbed or deferred pending product/architecture
decisions. See [Implementation status](#implementation-status) below before assuming any
feature works.

## Architecture

TutorFlow is a **.NET 10 modular monolith**: 14 independent modules running in a single
deployable (`TutorFlow.Host`), each owning its own data (one PostgreSQL schema per module) and
domain logic. Modules never reference each other's code or database directly — they communicate
exclusively through integration events (`TutorFlow.Shared`), published and consumed via
[Wolverine](https://wolverinefx.net/).

| Modules | Internal style |
|---|---|
| Bookings, Lessons, Subscriptions, Payments, Availability | DDD tactical patterns (aggregates, value objects, domain events) where a real invariant justifies it |
| Identity, Students, Tutors, Subjects, Search, Messaging, Reviews, Notifications, Learning | Vertical Slice (one feature, one file) until complexity proves otherwise |

Full module responsibilities and boundaries: [`docs/domain/DOMAIN_MODEL.md`](docs/domain/DOMAIN_MODEL.md).
Architecture decisions are recorded as ADRs under [`docs/decisions/`](docs/decisions/) — each one
documents the actual options considered and which was accepted.

## Implementation status

**12 of 14 modules** have real domain models, EF Core persistence, and feature handlers:
Availability, Bookings, Identity, Lessons, Messaging, Notifications, Payments, Reviews, Search,
Students, Subjects, Tutors. Of these, 11 also expose HTTP endpoints — **Notifications is
event-driven only** (consumes integration events, sends email, registers no routes by design).

**2 modules remain empty stubs** — a class library with only `Add<Name>Module()` /
`Map<Name>Module()`, referenced by the Host, no persistence or endpoints yet: **Learning**,
**Subscriptions**.

The frontend (`apps/web`, Next.js) covers public marketing pages, search/discovery, the tutor
onboarding wizard, student/tutor messaging, lesson booking, and an admin tutor-application review
flow.

**Preply feature parity is explicitly not complete.** Known, deliberately deferred gaps (pending
product/architecture decisions) include the Certification catalog combobox and an Education
"degree type" field in the tutor onboarding wizard. Don't assume any surface matches Preply's
behavior beyond what's described in `docs/product/` and `docs/design/`.

Everything under `docs/` outside of ADRs marked `Accepted` is a **specification/proposal**, not a
guarantee of implemented behavior — see the conventions in [`docs/README.md`](docs/README.md).

## Tech stack

**Backend**
- .NET 10 / C# 14
- ASP.NET Core Minimal APIs
- Entity Framework Core + PostgreSQL (`Npgsql.EntityFrameworkCore.PostgreSQL`) — one `DbContext` per module
- [Wolverine](https://wolverinefx.net/) — inter-module integration events (transactional outbox) and intra-module mediator, running in static (compile-time) codegen mode
- FluentValidation, Serilog (structured logging), JWT bearer auth
- Central NuGet package management (`Directory.Packages.props`) — every version lives there, `.csproj` files only reference package names
- xUnit v3 + Microsoft.Testing.Platform + Testcontainers.PostgreSql for tests

**Frontend** (`apps/web`)
- Next.js 16 / React 19, TypeScript
- TanStack Query, Radix UI primitives
- Stripe.js (`@stripe/react-stripe-js`) for payment UI
- Vitest + Testing Library for tests

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/) — version pinned by [`global.json`](global.json) (`10.0.100`)
- [Node.js](https://nodejs.org/) (CI uses Node 22; no version is pinned in this repo)
- [Docker](https://www.docker.com/) — for the local development PostgreSQL and for Testcontainers-based automated tests

## Local setup

### Backend

```bash
dotnet restore TutorFlow.slnx

# One-time: start a persistent local PostgreSQL for manual dev/testing.
# Automated tests do NOT use this — they spin up their own disposable
# Testcontainers instance per run, independent of this.
cp .env.example .env
# edit .env: set TUTORFLOW_DB_ADMIN_PASSWORD / TUTORFLOW_DB_APP_PASSWORD to
# real, locally-generated random values — never commit .env
docker compose up -d

# Configure per-module connection strings via user-secrets (see
# docs/DEVELOPMENT_DATABASE.md for the full list and alternatives)
dotnet user-secrets set "ConnectionStrings:IdentityDb" "Host=localhost;Port=5432;Database=tutorflow_identity;Username=tutorflow_app;Password=<your-password>" --project src/TutorFlow.Host

# Apply migrations per module, e.g.:
dotnet ef database update --project src/Modules/Identity/TutorFlow.Modules.Identity --startup-project src/TutorFlow.Host --context IdentityDbContext

# Run the host
dotnet run --project src/TutorFlow.Host
```

Full database setup, migration commands for every module, and troubleshooting:
[`docs/DEVELOPMENT_DATABASE.md`](docs/DEVELOPMENT_DATABASE.md).

### Frontend

```bash
cd apps/web
npm install
npm run dev
```

## Configuration

- **Dev database**: `.env` (copied from `.env.example`, gitignored) supplies Docker Compose
  bootstrap credentials only — the application itself never reads it.
- **Connection strings, JWT signing key, Stripe secret key, Google/Facebook OAuth client
  secrets, Postmark server token**: all configured via [.NET user-secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets)
  or environment variables against `TutorFlow.Host` — **never** committed to `appsettings*.json`.
- When a given secret (Stripe key, Postmark token, OAuth credentials) is left unconfigured, the
  corresponding module falls back to a deterministic local test adapter (`TestPaymentProviderClient`,
  `TestEmailSender`) instead of calling a real third-party service — so the app and its test suite
  run with no production credentials at all.

## Tests and builds

```bash
# Backend: full solution build
dotnet build

# Backend: format check (CI-enforced)
dotnet format TutorFlow.slnx --verify-no-changes

# Backend: run a module's tests — plain `dotnet test` reports "zero tests ran" against
# this repo's xUnit v3 / Microsoft.Testing.Platform projects; build then exec the DLL directly
# (see .github/workflows/backend.yml for the exact pattern used in CI):
dotnet build tests/Modules/Bookings/TutorFlow.Modules.Bookings.Tests
dotnet exec tests/Modules/Bookings/TutorFlow.Modules.Bookings.Tests/bin/Debug/net10.0/TutorFlow.Modules.Bookings.Tests.dll

# Frontend
cd apps/web
npm run lint
npx tsc --noEmit
npm run test
npm run build
```

CI workflows ([`.github/workflows/backend.yml`](.github/workflows/backend.yml),
[`.github/workflows/frontend.yml`](.github/workflows/frontend.yml)) run the authoritative version
of each of these steps, including a drift check that Wolverine's committed static codegen output
matches current handler code.

## Documentation

- [`docs/README.md`](docs/README.md) — documentation structure and reading order
- [`docs/domain/DOMAIN_MODEL.md`](docs/domain/DOMAIN_MODEL.md) — the 14 modules, their responsibilities and boundaries
- [`docs/decisions/`](docs/decisions/) — architecture decision records (ADRs)
- [`docs/product/`](docs/product/) — product vision, journeys, and workflow specs
- [`docs/OPEN_QUESTIONS.md`](docs/OPEN_QUESTIONS.md) — unresolved product/architecture decisions
- [`docs/product/preply-reverse-engineering/`](docs/product/preply-reverse-engineering/) — evidence behind Preply-parity UI/UX decisions
- [`docs/HANDOFF-2026-10-26-delivery-sprint.md`](docs/HANDOFF-2026-10-26-delivery-sprint.md) — most recent delivery sprint handoff and known carried-forward items

## Known limitations

- **Preply parity is incomplete** — several wizard fields (Certification catalog combobox,
  Education degree-type) are deliberately deferred pending data-source/schema decisions.
- **Learning and Subscriptions modules are stubs** — no persistence, handlers, or endpoints yet.
- **Local disk only for uploads** — tutor profile photos are stored under `wwwroot/uploads/`
  on local disk (no object storage provider is wired up); not suitable for a multi-instance
  deployment as-is.
- **No production deployment target defined yet** — no Dockerfile, Aspire AppHost, or
  cloud-specific configuration exists in this repository.
- Known local-only flakiness: running many backend/frontend test suites concurrently on one
  machine can produce spurious failures from CPU contention, not logic bugs — re-run a suite in
  isolation before treating a failure as a regression.

## Development conventions

Engineering conventions (module structure, coding standards, anti-patterns to avoid, and the
full command reference) are documented in [`CLAUDE.md`](CLAUDE.md) — read it before making
changes. In short: file-scoped namespaces, `TreatWarningsAsErrors` everywhere, no cross-module
references outside integration events, `Result`/`ProblemDetails` error handling, and a
plan-first workflow for non-trivial changes.
