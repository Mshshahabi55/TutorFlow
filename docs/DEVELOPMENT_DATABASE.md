# Local Development PostgreSQL

A persistent local PostgreSQL instance for day-to-day `dotnet run` and manual testing — distinct
from the ephemeral Testcontainers instances each module's automated test suite spins up for
itself (see [Tests still use Testcontainers](#tests-still-use-testcontainers) below; this setup
never replaces that).

Pinned to `postgres:18` — the same major version Testcontainers uses in every test fixture — with
a persistent named Docker volume, so data survives container restarts.

## 1. Start PostgreSQL

```bash
cp .env.example .env
# edit .env: set TUTORFLOW_DB_ADMIN_PASSWORD and TUTORFLOW_DB_APP_PASSWORD to real,
# locally-generated random values (e.g. `openssl rand -base64 24`) — never reuse a personal
# PostgreSQL password, and never commit this file (.env is already gitignored).

docker compose up -d
```

This starts one container, `tutorflow-postgres`, and — **the first time only**, when the named
volume `tutorflow_postgres_data` is empty — runs `docker/postgres/init/01-create-app-role.sh`,
which:

- creates a dedicated, **non-superuser** login role (`tutorflow_app` by default — confirm with
  `docker exec tutorflow-postgres psql -U tutorflow_admin -d postgres -c '\du tutorflow_app'`; its
  `Attributes` column is empty, i.e. no `SUPERUSER`/`CREATEDB`/`CREATEROLE`)
- creates databases — `tutorflow_identity`, `tutorflow_tutors`, `tutorflow_availability`,
  `tutorflow_bookings`, `tutorflow_search`, `tutorflow_subjects`, `tutorflow_students`,
  `tutorflow_payments`, `tutorflow_lessons`, `tutorflow_reviews`, `tutorflow_notifications`,
  `tutorflow_messaging` (the `Messaging` module's own conversations/messages database) — each
  **owned by** `tutorflow_app`

**Not provisioned here — needs its own database, resolved separately:**
- **`WolverineDb`** (the A44 Host-owned Main envelope store, `Program.cs`/`WolverineMessagingDbContext.cs`)
  needs a database of its own — not any module's own database, per that call site's own comment
  ("rather than piggybacking on a business module"). **Resolved (2026-10-26):** named
  `tutorflow_wolverine`, distinct from `tutorflow_messaging` above. An earlier debugging session
  (`docs/product/preply-reverse-engineering/27-phase3-audit-completion-2026-10-25.md` §0) had
  named it `tutorflow_messaging` ad hoc, which collided with the `Messaging` module's own rightful
  name under this project's per-module convention — the local database was renamed
  (`ALTER DATABASE tutorflow_messaging RENAME TO tutorflow_wolverine;`, no data lost; verified by
  confirming all 8 `wolverine_*`/`messaging` schema tables survived the rename) and the
  `WolverineDb` user-secret updated to match. `01-create-app-role.sh` does not provision
  `tutorflow_wolverine` — it's Host infrastructure, not a per-module database, and only ever needed
  locally for manual `dotnet run`/debugging (automated tests always get a fresh Testcontainers
  instance via `WolverineTestConnectionStrings`). Create it by hand the same way as
  `tutorflow_messaging` above if you need to run the Host locally.

`TUTORFLOW_DB_ADMIN_USER`/`TUTORFLOW_DB_ADMIN_PASSWORD` are a bootstrap role docker's own
entrypoint requires to initialize the instance — the application never authenticates as it.
Every module's connection string authenticates as `tutorflow_app` only.

If port 5432 is already taken locally (by a native install or another project's container), set
`TUTORFLOW_DB_PORT` in `.env` to something else (e.g. `5433`) before the first `docker compose up`.

Check it's healthy: `docker inspect --format='{{.State.Health.Status}}' tutorflow-postgres`
should report `healthy` within a few seconds.

## 2. Configure credentials (user-secrets)

The committed `appsettings.Development.json` deliberately has **no** `ConnectionStrings` section
— connection strings contain a password, so they're never committed. Set them via
[.NET user-secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) against the
Host project (already has a `UserSecretsId`):

```bash
source .env   # reuses the values you just set, instead of retyping them

dotnet user-secrets set "ConnectionStrings:IdentityDb" \
  "Host=localhost;Port=${TUTORFLOW_DB_PORT:-5432};Database=tutorflow_identity;Username=${TUTORFLOW_DB_APP_USER};Password=${TUTORFLOW_DB_APP_PASSWORD}" \
  --project src/TutorFlow.Host

dotnet user-secrets set "ConnectionStrings:TutorsDb" \
  "Host=localhost;Port=${TUTORFLOW_DB_PORT:-5432};Database=tutorflow_tutors;Username=${TUTORFLOW_DB_APP_USER};Password=${TUTORFLOW_DB_APP_PASSWORD}" \
  --project src/TutorFlow.Host

dotnet user-secrets set "ConnectionStrings:AvailabilityDb" \
  "Host=localhost;Port=${TUTORFLOW_DB_PORT:-5432};Database=tutorflow_availability;Username=${TUTORFLOW_DB_APP_USER};Password=${TUTORFLOW_DB_APP_PASSWORD}" \
  --project src/TutorFlow.Host

dotnet user-secrets set "ConnectionStrings:BookingsDb" \
  "Host=localhost;Port=${TUTORFLOW_DB_PORT:-5432};Database=tutorflow_bookings;Username=${TUTORFLOW_DB_APP_USER};Password=${TUTORFLOW_DB_APP_PASSWORD}" \
  --project src/TutorFlow.Host

dotnet user-secrets set "ConnectionStrings:SearchDb" \
  "Host=localhost;Port=${TUTORFLOW_DB_PORT:-5432};Database=tutorflow_search;Username=${TUTORFLOW_DB_APP_USER};Password=${TUTORFLOW_DB_APP_PASSWORD}" \
  --project src/TutorFlow.Host

dotnet user-secrets set "ConnectionStrings:SubjectsDb" \
  "Host=localhost;Port=${TUTORFLOW_DB_PORT:-5432};Database=tutorflow_subjects;Username=${TUTORFLOW_DB_APP_USER};Password=${TUTORFLOW_DB_APP_PASSWORD}" \
  --project src/TutorFlow.Host

dotnet user-secrets set "ConnectionStrings:StudentsDb" \
  "Host=localhost;Port=${TUTORFLOW_DB_PORT:-5432};Database=tutorflow_students;Username=${TUTORFLOW_DB_APP_USER};Password=${TUTORFLOW_DB_APP_PASSWORD}" \
  --project src/TutorFlow.Host

dotnet user-secrets set "ConnectionStrings:LessonsDb" \
  "Host=localhost;Port=${TUTORFLOW_DB_PORT:-5432};Database=tutorflow_lessons;Username=${TUTORFLOW_DB_APP_USER};Password=${TUTORFLOW_DB_APP_PASSWORD}" \
  --project src/TutorFlow.Host

dotnet user-secrets set "ConnectionStrings:ReviewsDb" \
  "Host=localhost;Port=${TUTORFLOW_DB_PORT:-5432};Database=tutorflow_reviews;Username=${TUTORFLOW_DB_APP_USER};Password=${TUTORFLOW_DB_APP_PASSWORD}" \
  --project src/TutorFlow.Host

dotnet user-secrets set "ConnectionStrings:PaymentsDb" \
  "Host=localhost;Port=${TUTORFLOW_DB_PORT:-5432};Database=tutorflow_payments;Username=${TUTORFLOW_DB_APP_USER};Password=${TUTORFLOW_DB_APP_PASSWORD}" \
  --project src/TutorFlow.Host

dotnet user-secrets set "ConnectionStrings:NotificationsDb" \
  "Host=localhost;Port=${TUTORFLOW_DB_PORT:-5432};Database=tutorflow_notifications;Username=${TUTORFLOW_DB_APP_USER};Password=${TUTORFLOW_DB_APP_PASSWORD}" \
  --project src/TutorFlow.Host

dotnet user-secrets set "ConnectionStrings:MessagingDb" \
  "Host=localhost;Port=${TUTORFLOW_DB_PORT:-5432};Database=tutorflow_messaging;Username=${TUTORFLOW_DB_APP_USER};Password=${TUTORFLOW_DB_APP_PASSWORD}" \
  --project src/TutorFlow.Host

dotnet user-secrets set "ConnectionStrings:WolverineDb" \
  "Host=localhost;Port=${TUTORFLOW_DB_PORT:-5432};Database=tutorflow_wolverine;Username=${TUTORFLOW_DB_APP_USER};Password=${TUTORFLOW_DB_APP_PASSWORD}" \
  --project src/TutorFlow.Host
```

`tutorflow_wolverine` isn't created by `01-create-app-role.sh` (§1) — create it by hand first:
`docker exec tutorflow-postgres psql -U tutorflow_admin -d postgres -c "CREATE DATABASE tutorflow_wolverine OWNER tutorflow_app;"`.

(`dotnet user-secrets list --project src/TutorFlow.Host` to confirm — secrets live outside the
repo, under your user profile, never in source control.)

**Alternative: environment variables.** ASP.NET Core's configuration also reads double-underscore
environment variables, so `ConnectionStrings__BookingsDb=...` works identically to a user-secret
of the same key — useful for CI or containerized `dotnet run`, where user-secrets' per-user JSON
file isn't available. Either mechanism is fine; never put the value in a file under source control.

## 3. Apply migrations

EF Core migrations remain the single source of truth for schema — nothing calls
`EnsureCreated()`, and the Host never resets or auto-migrates the database on startup.

```bash
dotnet ef database update --project src/Modules/Identity/TutorFlow.Modules.Identity --startup-project src/TutorFlow.Host --context IdentityDbContext
dotnet ef database update --project src/Modules/Tutors/TutorFlow.Modules.Tutors --startup-project src/TutorFlow.Host --context TutorsDbContext
dotnet ef database update --project src/Modules/Availability/TutorFlow.Modules.Availability --startup-project src/TutorFlow.Host --context AvailabilityDbContext
dotnet ef database update --project src/Modules/Bookings/TutorFlow.Modules.Bookings --startup-project src/TutorFlow.Host --context BookingsDbContext
dotnet ef database update --project src/Modules/Search/TutorFlow.Modules.Search --startup-project src/TutorFlow.Host --context SearchDbContext
dotnet ef database update --project src/Modules/Subjects/TutorFlow.Modules.Subjects --startup-project src/TutorFlow.Host --context SubjectsDbContext
dotnet ef database update --project src/Modules/Students/TutorFlow.Modules.Students --startup-project src/TutorFlow.Host --context StudentsDbContext
dotnet ef database update --project src/Modules/Lessons/TutorFlow.Modules.Lessons --startup-project src/TutorFlow.Host --context LessonsDbContext
dotnet ef database update --project src/Modules/Reviews/TutorFlow.Modules.Reviews --startup-project src/TutorFlow.Host --context ReviewsDbContext
```

Each is independent (one schema per module, `ADR-0001`) — run only the ones you need, in any
order. Re-running an already-applied migration is a safe no-op (`dotnet ef` checks
`__EFMigrationsHistory` first).

**These migrations do not provision Wolverine's own message storage** (the
`wolverine_incoming_envelopes`/`wolverine_outgoing_envelopes`/etc. tables A44 depends on) —
Wolverine's own CLI command is a separate, required step. See
[`docs/WOLVERINE_PROVISIONING.md`](./WOLVERINE_PROVISIONING.md).

## 4. Reset the local database safely (only when explicitly requested)

This **destroys all local data** in every `tutorflow_*` database — never run it as part of a
normal workflow, startup script, or "fix a weird error" reflex. Only do this when a human has
explicitly asked for a clean local database.

```bash
docker compose down -v   # stops the container AND deletes the tutorflow_postgres_data volume
docker compose up -d     # recreates it empty; 01-create-app-role.sh runs again (fresh volume)
# then re-run every `dotnet ef database update` command from §3 — the new databases start with
# no schema at all until you do.
```

If you only want to drop one module's data without touching the others, drop just that
database instead of the whole volume:

```bash
docker exec tutorflow-postgres psql -U tutorflow_admin -d postgres -c "DROP DATABASE tutorflow_bookings;"
docker exec tutorflow-postgres psql -U tutorflow_admin -d postgres -c "CREATE DATABASE tutorflow_bookings OWNER tutorflow_app;"
dotnet ef database update --project src/Modules/Bookings/TutorFlow.Modules.Bookings --startup-project src/TutorFlow.Host --context BookingsDbContext
```

## Troubleshooting: "database does not exist" or password authentication failures

`docker-entrypoint-initdb.d` scripts (`01-create-app-role.sh`) only run the **first** time a
volume is initialized — if your local `tutorflow_postgres_data` volume predates a database or
user-secret being added to this doc/script (e.g. you created it before `tutorflow_students` or
`tutorflow_search` existed), it will silently be missing, even though the script itself is
correct for a fresh setup. This is a local-environment drift issue, not a repository defect —
confirmed directly (A47, `docs/OPEN_QUESTIONS.md`): the init script already lists every database
correctly; only one already-initialized local volume was stale relative to it.

Fix without a destructive full reset (§4) — create just the missing database(s):

```bash
docker exec tutorflow-postgres psql -U tutorflow_admin -d postgres -c "CREATE DATABASE tutorflow_search OWNER tutorflow_app;"
dotnet ef database update --project src/Modules/Search/TutorFlow.Modules.Search --startup-project src/TutorFlow.Host --context SearchDbContext
```

If instead you get `password authentication failed for user "tutorflow_app"` on a connection
string that looks right, the stored user-secret doesn't match the role's actual password (every
module's connection string must authenticate as the *same* `tutorflow_app` role/password — if one
module's secret was set at a different time with a different value, only that module will fail).
Confirm the real password by checking a connection string you know works
(`dotnet user-secrets list --project src/TutorFlow.Host`), then re-set the broken one to match —
never invent a new password here; all modules share one role.

## Tests still use Testcontainers

Nothing above changes how automated tests run. Every module's integration tests (`*ApiFixture`
classes) start their own ephemeral `postgres:18` Testcontainers instance per test run,
independent of this persistent dev database and of each other — migrated fresh, torn down after.
This is intentional and unchanged: CI and `dotnet test` must never depend on a developer's local
Docker Compose state being up, configured, or even installed.

## What's committed vs. not

| File | Committed? | Contains secrets? |
|---|---|---|
| `docker-compose.yml` | Yes | No — only `${VAR}` references |
| `docker/postgres/init/01-create-app-role.sh` | Yes | No — reads the password from env vars at container startup, never hardcodes one |
| `.env.example` | Yes | No — placeholder values only |
| `.env` | **No** (gitignored) | Yes — real local passwords |
| `appsettings.Development.json` | Yes | No — intentionally has no `ConnectionStrings` section |
| .NET user-secrets | Never (lives outside the repo) | Yes |
