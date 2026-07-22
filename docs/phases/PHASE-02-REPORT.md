# Phase 2 Report — First Real Run (PostgreSQL 17)

Branch: `develop` | Commits this phase: `f790268`, `e7da798` (Task 0),
`3468054` (Task 1), `4a95998`, `c4c74e3` (Task 2), `aa461ae` (Task 4),
`fd6858b` (Task 6), `d496ad5` (Task 7), plus this report's own commit.
Tasks 3 and 5 produced no file changes (pure investigation/verification) —
their results are recorded below instead.

**Status: complete.** This is the first time any part of this codebase has
ever connected to a real PostgreSQL instance. It found real breakage
exactly where expected (Section 4) and one thing that turned out *not* to
be broken (Section 3's `timestamptz` answer) — both reported as observed
fact, not assumption.

## 1. Clean Architecture validation

`backend/src/` changed in exactly two places, both narrowly scoped: a new
file, `backend/src/Web/DevelopmentSeeder.cs` (Task 6 — orchestrates
existing repositories/`IUnitOfWork` through the same interfaces every real
command handler uses; no Domain or Application code touched), and
`backend/src/Web/Program.cs` (two `using` additions plus a 6-line
`IsDevelopment()`-guarded call to it). No Domain entity behaviour and no
API contract changed anywhere. `frontend/src/` was never touched.
`dotnet build TutorFlow.sln`: **0 Warnings, 0 Errors** throughout.

## 2. Repository convention validation

Nine commits on `develop`, matching the task order (0 → 1 → 2 → 4 → 6 → 7;
3 and 5 needed none). No `bin/`/`obj/`/`node_modules/` staged. No file
under source control contains a real password anywhere — verified
explicitly in Section 2.3 below (Task 2's own requirement) and re-checked
via `git status`/`git diff` before every commit this phase.

## 3. Task 0, 1, 2 — cleanup, version story, databases and secrets

**Task 0 (approved, done first).** All 18 `Application.Tests` tests named
`..._and_saves` (Phase 1C Section 6) renamed to `..._and_calls_SaveChanges`
— no assertion or logic changed, 218/218 still pass (`f790268`). Vitest's
per-test timeout raised from the 5000ms default to 15000ms in
`frontend/vitest.config.ts` — config only, no `frontend/src/` change — and
the full frontend suite re-run once: **173/173 passed, 51.20s** (`e7da798`).

**Task 1 — PostgreSQL version story.** `.devcontainer/` was unmodified
GitHub `dotnet-postgres` template scaffolding: `postgres:14.3` and a
`.NET 8.0` base image, neither matching this project's actual PostgreSQL 17
target or .NET 9 (`backend/global.json`), and pulling from Docker Hub,
which ADR-018 flags as potentially unreachable from Iran. Recommended and
applied: banner comments marking all three `.devcontainer/` files as
unused, unsupported scaffolding, rather than updating them to track
`postgres:17` — the supported path is a natively-installed instance (this
phase's own approach), and investing in a devcontainer nobody has used or
tested is a bigger commitment than this phase's scope; a
technically-current-but-never-tested devcontainer would be a worse trap
than a clearly-labeled unused one. `ADR-013-persistence-technology.md`
gained a dated addendum recording PostgreSQL 17 (verified: 17.10) as the
target — existing ADR content unchanged (`3468054`).

**Task 2 — databases and secrets.**
1. `tutorflow_dev` and `tutorflow_test` created via `psql -U postgres -h localhost -c "CREATE DATABASE ...;"` against the local PostgreSQL 17.10 service (`postgresql-x64-17`, confirmed running via `Get-Service`). The instance also hosts unrelated pre-existing databases (`erp_dev`, `erp_test`, `erp_portfolio_dev`) — untouched.
2. `dotnet user-secrets init` run from `backend/src/Web` (`4a95998`) — added only `<UserSecretsId>c8b4f769-5011-45d7-99eb-3cdb718d0ea5</UserSecretsId>` to `TutorFlow.Web.csproj`, itself just an identifier, safe to commit. Command used to store the real connection string (password masked, never logged in full anywhere):
   ```
   dotnet user-secrets set "ConnectionStrings:TutorFlow" "Host=localhost;Database=tutorflow_dev;Username=postgres;Password=***"
   ```
3. Verified: `git status`/`git diff` show no credential in any tracked file; `appsettings.json`'s `ConnectionStrings:TutorFlow` is still exactly `Host=localhost;Database=tutorflow;Username=REPLACE_ME;Password=REPLACE_ME`.
4. Setup steps documented in `README.md`'s new "Database setup" section (`c4c74e3`), including the `TUTORFLOW_TEST_CONNECTION` environment variable Task 4's tests require.

The postgres superuser password itself was requested directly from the
owner mid-session (not guessed, not brute-forced, not left as a default) —
the structured question tool used earlier in this project for
multiple-choice decisions turned out not to support free-text secret
collection, so the request was made as a plain chat message instead, and
the value was used only for ephemeral `$env:PGPASSWORD`/user-secrets calls,
never written to a file or echoed back.

## 3. (continued) Task 3 — first migration and the actual schema

`ASPNETCORE_ENVIRONMENT=Development dotnet ef database update --verbose`,
run from `backend/src/Web` against `tutorflow_dev`. Full output (verbatim,
build noise trimmed):

```
Using project 'D:\Projects\TutorFlow\backend\src\Web\TutorFlow.Web.csproj'.
Using startup project 'D:\Projects\TutorFlow\backend\src\Web\TutorFlow.Web.csproj'.
Build succeeded.
Using environment 'Development'.
Found DbContext 'TutorFlowDbContext'.
Using context 'TutorFlowDbContext'.
Using design-time services from provider 'Npgsql.EntityFrameworkCore.PostgreSQL'.
fail: Microsoft.EntityFrameworkCore.Database.Command[20102]
      Failed executing DbCommand (17ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      SELECT "MigrationId", "ProductVersion"
      FROM "__EFMigrationsHistory"
      ORDER BY "MigrationId";
info: Microsoft.EntityFrameworkCore.Migrations[20411]
      Acquiring an exclusive lock for migration application.
info: Microsoft.EntityFrameworkCore.Migrations[20402]
      Applying migration '20260719212355_InitialCreate'.
Applying migration '20260719215841_AddHourlyRatePrecision'.
Applying migration '20260720044906_AddAuditEntries'.
Applying migration '20260720175732_AddAuthentication'.
Applying migration '20260720200951_AddAccountLockoutAndAbsoluteSessionLifetime'.
Applying migration '20260721064101_AddRelationshipInvitedByAccountId'.
Done.
```

**Succeeded on the first attempt — no diagnosis or workaround needed.** The
one `fail:`-level log line is EF Core's own routine "does
`__EFMigrationsHistory` exist yet" probe on a brand-new database (it
doesn't, so the `SELECT` fails once, which is expected and not a real
error) — `dotnet ef migrations list` afterward confirms all 6 migrations
applied cleanly:

```
20260719212355_InitialCreate
20260719215841_AddHourlyRatePrecision
20260720044906_AddAuditEntries
20260720175732_AddAuthentication
20260720200951_AddAccountLockoutAndAbsoluteSessionLifetime
20260721064101_AddRelationshipInvitedByAccountId
```

**Full resulting schema** (`psql -d tutorflow_dev -c "\d+ <table>"`, every
table, verbatim column/index/constraint output):

```
                                                        Table "public.AdminStaffs"
         Column          |           Type           | Collation | Nullable | Default | Storage  | Compression | Stats target | Description
-------------------------+--------------------------+-----------+----------+---------+----------+-------------+--------------+-------------
 Id                      | uuid                     |           | not null |         | plain    |             |              |
 Email                   | text                     |           | not null |         | extended |             |              |
 PasswordHash            | text                     |           | not null |         | extended |             |              |
 FailedLoginAttemptCount | integer                  |           | not null | 0       | plain    |             |              |
 LockedUntilUtc          | timestamp with time zone |           |          |         | plain    |             |              |
Indexes:
    "PK_AdminStaffs" PRIMARY KEY, btree ("Id")
    "IX_AdminStaffs_Email" UNIQUE, btree ("Email")

                                                   Table "public.AuditEntries"
    Column     |           Type           | Collation | Nullable | Default | Storage  | Compression | Stats target | Description
---------------+--------------------------+-----------+----------+---------+----------+-------------+--------------+-------------
 Id            | uuid                     |           | not null |         | plain    |             |              |
 OccurredOnUtc | timestamp with time zone |           | not null |         | plain    |             |              |
 Action        | text                     |           | not null |         | extended |             |              |
 SubjectId     | uuid                     |           |          |         | plain    |             |              |
 ActorId       | text                     |           |          |         | extended |             |              |
 ActorRole     | text                     |           |          |         | extended |             |              |
Indexes:
    "PK_AuditEntries" PRIMARY KEY, btree ("Id")
    "IX_AuditEntries_SubjectId" btree ("SubjectId")

                                                                      Table "public.AuthTokens"
        Column        |           Type           | Collation | Nullable |                Default                | Storage  | Compression | Stats target | Description
----------------------+--------------------------+-----------+----------+---------------------------------------+----------+-------------+--------------+-------------
 Id                   | uuid                     |           | not null |                                       | plain    |             |              |
 AccountId            | uuid                     |           | not null |                                       | plain    |             |              |
 Role                 | text                     |           | not null |                                       | extended |             |              |
 TokenHash            | text                     |           | not null |                                       | extended |             |              |
 CreatedAtUtc         | timestamp with time zone |           | not null |                                       | plain    |             |              |
 ExpiresAtUtc         | timestamp with time zone |           | not null |                                       | plain    |             |              |
 RevokedAtUtc         | timestamp with time zone |           |          |                                       | plain    |             |              |
 AbsoluteExpiresAtUtc | timestamp with time zone |           | not null | '-infinity'::timestamp with time zone | plain    |             |              |
Indexes:
    "PK_AuthTokens" PRIMARY KEY, btree ("Id")
    "IX_AuthTokens_AccountId" btree ("AccountId")
    "IX_AuthTokens_TokenHash" UNIQUE, btree ("TokenHash")

                                               Table "public.AvailabilitySlots"
    Column    |           Type           | Collation | Nullable | Default | Storage | Compression | Stats target | Description
--------------+--------------------------+-----------+----------+---------+---------+-------------+--------------+-------------
 Id           | uuid                     |           | not null |         | plain   |             |              |
 TutorId      | uuid                     |           | not null |         | plain   |             |              |
 StartTimeUtc | timestamp with time zone |           | not null |         | plain   |             |              |
 Duration     | interval                 |           | not null |         | plain   |             |              |
 DeliveryMode | integer                  |           | not null |         | plain   |             |              |
 IsConsumed   | boolean                  |           | not null |         | plain   |             |              |
Indexes:
    "PK_AvailabilitySlots" PRIMARY KEY, btree ("Id")
Referenced by:
    TABLE "Sessions" CONSTRAINT "FK_Sessions_AvailabilitySlots_AvailabilitySlotId" FOREIGN KEY ("AvailabilitySlotId") REFERENCES "AvailabilitySlots"("Id") ON DELETE RESTRICT

                                                       Table "public.ParentGuardians"
         Column          |           Type           | Collation | Nullable | Default  | Storage  | Compression | Stats target | Description
-------------------------+--------------------------+-----------+----------+----------+----------+-------------+--------------+-------------
 Id                      | uuid                     |           | not null |          | plain    |             |              |
 Email                   | text                     |           | not null | ''::text | extended |             |              |
 PasswordHash            | text                     |           | not null | ''::text | extended |             |              |
 FailedLoginAttemptCount | integer                  |           | not null | 0        | plain    |             |              |
 LockedUntilUtc          | timestamp with time zone |           |          |          | plain    |             |              |
Indexes:
    "PK_ParentGuardians" PRIMARY KEY, btree ("Id")
    "IX_ParentGuardians_Email" UNIQUE, btree ("Email")

                                                              Table "public.Relationships"
       Column       |  Type   | Collation | Nullable |                   Default                    | Storage | Compression | Stats target | Description
--------------------+---------+-----------+----------+----------------------------------------------+---------+-------------+--------------+-------------
 Id                 | uuid    |           | not null |                                              | plain   |             |              |
 ParentGuardianId   | uuid    |           | not null |                                              | plain   |             |              |
 StudentId          | uuid    |           | not null |                                              | plain   |             |              |
 Status             | integer |           | not null |                                              | plain   |             |              |
 InvitedByAccountId | uuid    |           | not null | '00000000-0000-0000-0000-000000000000'::uuid | plain   |             |              |
Indexes:
    "PK_Relationships" PRIMARY KEY, btree ("Id")

                                                       Table "public.Sessions"
       Column       |           Type           | Collation | Nullable | Default | Storage | Compression | Stats target | Description
--------------------+--------------------------+-----------+----------+---------+---------+-------------+--------------+-------------
 Id                 | uuid                     |           | not null |         | plain   |             |              |
 TutorId            | uuid                     |           | not null |         | plain   |             |              |
 StudentId          | uuid                     |           | not null |         | plain   |             |              |
 ParentGuardianId   | uuid                     |           |          |         | plain   |             |              |
 AvailabilitySlotId | uuid                     |           | not null |         | plain   |             |              |
 ScheduledTimeUtc   | timestamp with time zone |           | not null |         | plain   |             |              |
 Duration           | interval                 |           | not null |         | plain   |             |              |
 DeliveryMode       | integer                  |           | not null |         | plain   |             |              |
 Status             | integer                  |           | not null |         | plain   |             |              |
Indexes:
    "PK_Sessions" PRIMARY KEY, btree ("Id")
    "IX_Sessions_AvailabilitySlotId" UNIQUE, btree ("AvailabilitySlotId")
    "IX_Sessions_StudentId" btree ("StudentId")
    "IX_Sessions_TutorId" btree ("TutorId")
Foreign-key constraints:
    "FK_Sessions_AvailabilitySlots_AvailabilitySlotId" FOREIGN KEY ("AvailabilitySlotId") REFERENCES "AvailabilitySlots"("Id") ON DELETE RESTRICT

                                                          Table "public.Students"
         Column          |           Type           | Collation | Nullable | Default  | Storage  | Compression | Stats target | Description
-------------------------+--------------------------+-----------+----------+----------+----------+-------------+--------------+-------------
 Id                      | uuid                     |           | not null |          | plain    |             |              |
 IsMinor                 | boolean                  |           | not null |          | plain    |             |              |
 Email                   | text                     |           | not null | ''::text | extended |             |              |
 PasswordHash            | text                     |           | not null | ''::text | extended |             |              |
 FailedLoginAttemptCount | integer                  |           | not null | 0        | plain    |             |              |
 LockedUntilUtc          | timestamp with time zone |           |          |          | plain    |             |              |
Indexes:
    "PK_Students" PRIMARY KEY, btree ("Id")
    "IX_Students_Email" UNIQUE, btree ("Email")

                                                           Table "public.Tutors"
         Column          |           Type           | Collation | Nullable | Default  | Storage  | Compression | Stats target | Description
-------------------------+--------------------------+-----------+----------+----------+----------+-------------+--------------+-------------
 Id                      | uuid                     |           | not null |          | plain    |             |              |
 IsApproved              | boolean                  |           | not null |          | plain    |             |              |
 IsSuspended             | boolean                  |           | not null |          | plain    |             |              |
 HourlyRate              | numeric(10,2)            |           |          |          | main     |             |              |
 Subject                 | text                     |           |          |          | extended |             |              |
 Language                | text                     |           |          |          | extended |             |              |
 Location                | text                     |           |          |          | extended |             |              |
 OfferedDurations        | text                     |           | not null |          | extended |             |              |
 Email                   | text                     |           | not null | ''::text | extended |             |              |
 PasswordHash            | text                     |           | not null | ''::text | extended |             |              |
 FailedLoginAttemptCount | integer                  |           | not null | 0        | plain    |             |              |
 LockedUntilUtc          | timestamp with time zone |           |          |          | plain    |             |              |
Indexes:
    "PK_Tutors" PRIMARY KEY, btree ("Id")
    "IX_Tutors_Email" UNIQUE, btree ("Email")
```

**The `timestamptz` question, answered as fact, not expectation.** Every
single `DateTime` column in the schema — `AdminStaffs.LockedUntilUtc`,
`AuditEntries.OccurredOnUtc`, `AuthTokens.CreatedAtUtc`/`ExpiresAtUtc`/
`RevokedAtUtc`/`AbsoluteExpiresAtUtc`, `AvailabilitySlots.StartTimeUtc`,
`ParentGuardians.LockedUntilUtc`, `Sessions.ScheduledTimeUtc`,
`Students.LockedUntilUtc`, `Tutors.LockedUntilUtc` — is **`timestamp with
time zone`**, with no exceptions, despite no EF configuration anywhere
specifying a column type for any of them (confirmed by Phase 0.5's earlier
audit and re-confirmed here: `grep -r "HasColumnType" Configurations/`
finds nothing timestamp-related). This is Npgsql's own default convention
for a plain C# `DateTime` property, and it is the *safer* of the two
possible outcomes — but it comes with a real, sharp edge, proven directly
in Section 4 below: Npgsql enforces `DateTimeKind.Utc` on write to a
`timestamptz` column and throws if given `Kind.Unspecified`. Every write
path in this codebase already uses `DateTime.UtcNow` exclusively (Phase
0.5's finding, unchanged), so this has not yet been a problem in
practice — but it is a hard failure waiting for the first code path that
doesn't.

`HourlyRate`: **`numeric(10,2)`** — confirmed exactly as the
`AddHourlyRatePrecision` migration and Phase 0.5's audit both stated.

**Three columns carry migration-artifact defaults**, worth recording as
observed facts even though none are currently a correctness problem (this
is a fresh database with no pre-existing rows to backfill): `AuthTokens.AbsoluteExpiresAtUtc`
defaults to `'-infinity'::timestamp with time zone`, `Relationships.InvitedByAccountId`
defaults to the all-zero UUID, and `Email`/`PasswordHash` on `ParentGuardians`/
`Students`/`Tutors` default to `''::text` — all produced by EF Core needing
*some* value to satisfy a `NOT NULL` constraint when a later migration added
these columns to tables that (in a database with existing rows) would
already have data. Harmless here; would need an explicit backfill strategy
before ever running these migrations against a database with real rows.

## 4. Task 4 — Postgres-backed integration tests, honestly reported

`backend/tests/Infrastructure.Tests/Postgres/` (`PostgresTestFixture.cs`,
`PostgresIntegrationTests.cs`): 7 tests, each targeting one specific
SQLite/PostgreSQL divergence risk, via the real `AddInfrastructure` DI
graph pointed at Npgsql — not SQLite, not a mock.

**Unconfigured behavior, verified first:** running with
`TUTORFLOW_TEST_CONNECTION` unset fails all 7 tests immediately with
`PostgresTestFixture`'s own message ("Environment variable
'TUTORFLOW_TEST_CONNECTION' is not set...") — confirmed via
`Class fixture type 'PostgresTestFixture' threw in its constructor` in the
actual test output. Not a skip.

**First run, `TUTORFLOW_TEST_CONNECTION` configured: 6 of 7 passed, 1
failed — and that failure is the successful outcome this task asked for.**
`DateTime_with_Unspecified_Kind_is_rejected_by_Npgsql` was written
expecting `InvalidOperationException`; the actual result was:

```
Assert.Throws() Failure: Exception type was not an exact match
Expected: typeof(System.InvalidOperationException)
Actual:   typeof(Microsoft.EntityFrameworkCore.DbUpdateException)
---- Microsoft.EntityFrameworkCore.DbUpdateException : An error occurred while saving the entity changes.
-------- System.ArgumentException : Cannot write DateTime with Kind=Unspecified to PostgreSQL type
'timestamp with time zone', only UTC is supported. Note that it's not possible to mix DateTimes
with different Kinds in an array, range, or multirange. (Parameter 'value')
```

This is exactly the "Npgsql is stricter than SQLite" risk this task named,
now proven rather than assumed: **the failure was in the test's own
assumption about the exception type, not in the underlying behavior it was
testing** — Npgsql does reject `Kind=Unspecified`, just via
`DbUpdateException` wrapping an `ArgumentException`, not a raw
`InvalidOperationException`. The assertion was corrected to match the
observed reality (`Assert.IsType<ArgumentException>(exception.InnerException)`,
checking the message contains `"Kind=Unspecified"`) — not weakened, not
changed to accept a wider range of outcomes, just pointed at what actually
happens. **7/7 passed after that one correction**, confirmed by re-running
with and without the environment variable configured (loud failure
confirmed again in the unconfigured case) and again as part of the full
`scripts/verify.ps1` run in Section 5 below.

Every other test passed on the first try:
`DateTime_with_Utc_Kind_round_trips_with_the_same_instant` (proves the
*safe* half of the same risk: a correctly-`Kind.Utc` `DateTime` round-trips
exactly, including through a real `Tutor.RecordFailedLoginAttempt` →
lockout flow), `BookSession_persists_AvailabilitySlot_IsConsumed_against_real_Postgres`,
`Sessions_AvailabilitySlotId_unique_index_rejects_a_genuine_concurrent_double_booking`
(a raw-SQL duplicate insert — bypassing `Session.Book`, which is `internal`
with no `InternalsVisibleTo` grant to this test project — hit PostgreSQL
error `23505 unique_violation`, confirming the physical constraint CONST-1
ultimately depends on is real and enforced), `HourlyRate_decimal_precision_round_trips_exactly`
(`123456.78m` exact), `Approving_a_tutor_writes_the_audit_entry_in_the_same_SaveChanges_call`
(real `EfUnitOfWork` + real `AuditDomainEventHandler`, both writes present
after one `SaveChangesAsync` call), and
`Repeated_failed_login_attempts_persist_the_lockout_counter_and_locked_until_against_real_Postgres`
(5 real fetch-mutate-save cycles against Postgres, matching
`LoginCommandHandler`'s exact shape — this is the same code path Phase 1B
proved was broken against SQLite before the `TutorRepository` fix; this
test proves the fix holds against the real provider too).

**Excluded from the default `dotnet test` run**, via
`[Trait("Category", "Postgres")]` and `--filter "Category!=Postgres"` in
both `backend-ci.yml` (updated as a necessary consequence — without this,
adding these tests would have silently broken every future CI run, since
GitHub Actions has no Postgres service and ADR-018 forbids depending on one
being reachable there) and `scripts/verify.ps1`'s two determinism-guard
test steps. `scripts/verify.ps1` runs them as their own explicit step
instead (Task 7, Section 5).

## 5. Task 5, 6, 7 — booting the API, seed data, and the updated gate

**Task 5.** `ASPNETCORE_ENVIRONMENT=Development dotnet run` against
`tutorflow_dev`: **started successfully, first attempt.** Full startup log:

```
info: TutorFlow.Web[0]
      CORS: no allowed origins configured (Cors:AllowedOrigins); cross-origin browser requests will be rejected.
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5199
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
info: Microsoft.Hosting.Lifetime[0]
      Hosting environment: Development
info: Microsoft.Hosting.Lifetime[0]
      Content root path: D:\Projects\TutorFlow\backend\src\Web
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (37ms) [...] SELECT ... FROM "AuthTokens" AS a WHERE ...
warn: Microsoft.AspNetCore.HttpsPolicy.HttpsRedirectionMiddleware[3]
      Failed to determine the https port for redirect.
```

The `AuthTokenCleanupService` background service's first pass ran
immediately and successfully queried real PostgreSQL. **One warning, no
errors**: "Failed to determine the https port for redirect" — expected and
benign, since no HTTPS launch profile is configured (the same warning seen
throughout every `Web.Tests` run against SQLite in earlier phases).
`GET /health` → `200 OK`, body `Healthy` (the real `AddDbContextCheck<TutorFlowDbContext>()`
health check, meaning this is a genuine, successful live database
connectivity probe, not a hardcoded response). `GET /health/ready` → `404` —
**there is only one health endpoint, `/health`**, no separate readiness
probe exists in this codebase. As an extra smoke test (not required by the
task, done for confidence): `POST /tutors` against the live host with a
real payload succeeded (`200`, a real `tutorId` returned), confirming a
full write path works end-to-end through the actual running process, not
just through test fixtures.

**Task 6.** `backend/src/Web/DevelopmentSeeder.cs`, called from
`Program.cs` only inside `if (app.Environment.IsDevelopment())`. Seeds one
`AdminStaff`, one approved `Tutor` (subject, language, location, hourly
rate, offered durations, one open `AvailabilitySlot`), one `Student`, and
one `ParentGuardian` with a `Confirmed` `Relationship` to that `Student` —
via the same repositories and `IUnitOfWork` every real command handler
uses. Verified directly, not assumed:
- **Fails loudly without a configured admin password**: ran with
  `Seed:AdminPassword` unset → `Unhandled exception. System.InvalidOperationException:
  Development seed data requires 'Seed:AdminPassword' to be configured...` —
  the process exits, nothing is silently defaulted.
- **Idempotent**: configured the secret, ran once (7 `INSERT` statements
  across `AdminStaffs`/`Tutors`/`Students`/`ParentGuardians`/`AuditEntries`/
  `AvailabilitySlots`/`Relationships`, confirmed in the live SQL log), then
  ran a second time — only a `SELECT` against `AdminStaffs` executed, no
  inserts, no exception. Confirmed independently at the database level via
  `psql`: exactly 1 row per seeded entity after both runs.

**Task 7.** `scripts/verify.ps1`'s two `dotnet test` steps now filter
`Category!=Postgres`; a new, separately-labelled `Backend: Postgres
integration tests` step runs `--filter "Category=Postgres"`. Full,
real, end-to-end run with `TUTORFLOW_TEST_CONNECTION` configured:

```
=================== VERIFY SUMMARY ===================
dotnet restore                                PASS                              1.5s
dotnet build (0 warnings)                     PASS                              5.9s
Backend: dotnet test TutorFlow.sln (run 1 of 2) PASS                             25.8s
Backend: dotnet test TutorFlow.sln (run 2 of 2) PASS                             19.1s
Backend: Postgres integration tests           PASS                              4.5s
npm ci                                        PASS                             29.5s
Frontend: npm run lint                        PASS                             13.2s
Frontend: npm run build                       PASS                             15.8s
Frontend: npm test -- --run                   PASS                             54.2s
-------------------------------------------------------
Total elapsed: 169.6s

RESULT: PASS
```

All 9 steps green. Backend totals: 53 + 218 + 59 + 161 = 491 (Category!=Postgres)
plus 7 (Category=Postgres) = **498 backend tests**; frontend 173. 671 total,
every one passing in this run.

## 6. Open items for the owner — not decided here

- **The three migration-artifact column defaults** (Section 3):
  `AbsoluteExpiresAtUtc = '-infinity'`, `InvitedByAccountId = 00000000-...`,
  three `Email`/`PasswordHash` pairs defaulting to `''`. Harmless against a
  fresh database; would need an explicit backfill decision before these
  specific migrations are ever run against a database with pre-existing
  rows (not a concern for this project yet, since no database has ever had
  rows before this phase).
- **`DateTime.Kind=Unspecified` is a real, proven failure mode** (Section
  4), currently avoided only because every write path in this codebase
  happens to use `DateTime.UtcNow`. No enforcement (analyzer rule, wrapper
  type, or `DateTimeOffset` migration) exists to keep it that way as the
  codebase grows — flagged, not fixed, since fixing it would mean either a
  new coding-convention rule or a broader `DateTimeOffset` migration,
  either a scope decision for the owner.
- **`.devcontainer/` remains unused, unmaintained scaffolding** (Task 1) —
  a future phase could invest in making it a real, tested PostgreSQL-17-based
  dev environment, but that is a new piece of infrastructure to build and
  maintain, not a fix to an existing one, and wasn't undertaken here.
- **Only one health endpoint exists** (`/health`), no `/health/ready`
  distinguishing liveness from readiness — noted as an observed fact per
  Task 5's instruction, not flagged as a defect (no approved document
  requires the distinction).

## 7. Merge recommendation

**Ready to merge to `main`.** Every absolute rule was honored: no real
password in any committed file (verified explicitly, twice), no seed data
reachable outside `Development`, no Domain entity or API contract changed,
and the one migration attempt succeeded cleanly with no hand-editing. The
one genuine "found breakage" this phase produced — the `DateTime`-Kind
exception-type assumption in Task 4's own test — was corrected transparently,
with the actual observed exception documented rather than the assumption
silently kept or the test loosened to tolerate either outcome. `scripts/verify.ps1`
is now a stronger gate than it was entering this phase: it proves the real
database path works, not just the SQLite substitute every prior phase relied
on exclusively.
