# Phase 3.5 Report — Cleanup and Deferred Backlog

Branch: `develop` | Commits this phase: `be5218e` (Task 1), `e718e7f`
(Task 2), `2d8a274` (Task 3), `9b195ba` (Task 4).

**Status: complete, 9/9 green.** `scripts/verify.ps1` was run against a
real local PostgreSQL 17 instance already configured on this machine (via
`dotnet user-secrets`, per README's "Database setup") — a first for a
phase report in this repo; every prior phase's verify.ps1 run reported the
Postgres step as an environment-only gap. All four tasks are done: the
last raw-UTC-ISO input is converted, the hardcoded Tehran offset is
documented as a single change point, `DevelopmentSeeder`'s failure no
longer crashes the host, and the frontend's 5 audited vulnerabilities are
resolved via a vitest 2→4 upgrade with zero application-source changes.

## 1. Clean Architecture validation

`frontend/src/` changes are confined to Discovery's search filter (schema
+ page + test) and one deleted, now-dead validator
(`shared/validation/isoDateTime.ts`) — no Domain/Application-equivalent
layering exists on the frontend to violate. `backend/src/` changes are
confined to the Web layer: `DevelopmentSeeder.cs` (two new `internal`
exception types plus a scoped `try`/`catch` around its own risky phase)
and a corresponding `try`/`catch` in `Program.cs`. No Domain entity,
Application command/query, Infrastructure repository, or EF Core
configuration changed. No new endpoint, so no `AUTHORIZATION_MATRIX.md`
update was needed. `dotnet build TutorFlow.sln`: **0 Warnings, 0 Errors**.
`npm run build`/`tsc -b`: clean. `npm run lint`: clean.

## 2. Repository convention validation

Four commits on `develop`, one per task, Conventional Commits, in task
order — Task 4 last, after Tasks 1–3 were committed, exactly as instructed
("do not start early... so the previous commit is a known-good rollback
point"). No `bin/`/`obj/`/`node_modules`/`dist` staged. No secret
committed: the real local Postgres credentials discovered in this
environment (`dotnet user-secrets`, `%APPDATA%\Microsoft\UserSecrets\`)
were read only to set the `TUTORFLOW_TEST_CONNECTION` environment variable
for this session's `verify.ps1` run — never written to any tracked file,
config, or commit. No test weakened, skipped, or deleted; two inverted
assertions (Task 1) are each in their own commit with an explanation,
following the established Phase 1B/2.5/3 precedent.

## 3. Task 1 & Task 2 — the last raw-UTC input, and the offset's single change point

**Task 1** (`be5218e`): Search Tutors' `availableFrom` filter — the one
time input Phase 3 didn't touch, since it named only Declare Availability
and Reschedule Session — is now the same `type="datetime-local"` +
`fromTehranInput` pattern as every other time input, labelled "(Tehran)".
`grep -rln "isoDateTimeUtcSchema" frontend/src` confirmed zero remaining
consumers after the change, so `shared/validation/isoDateTime.ts` was
deleted (not left as dead code). The old "rejects a malformed
availableFrom filter" test is inverted to "searches with a Tehran-entered
availableFrom filter, converted to UTC" in the same commit, since a native
picker can no longer produce free-typed garbage — the same precedent Phase
3 Task 2 set. All 195 frontend tests pass; `npx tsc -b` and `eslint .`
both clean.

**Task 2** (`e718e7f`): `TEHRAN_OFFSET_MINUTES` in
`frontend/src/shared/time/tehranTime.ts` was already a single named
constant (Phase 3's own design) — this task added the comment block the
phase asked for, stating the value, why it's fixed rather than
`Intl`-resolved, and that this constant is the only place that needs to
change if Iran's offset changes again (it has before: DST was abolished
only in 2022). The identical note was added to `README.md`'s Known
pitfalls. Comment-only change to the constant; all 22
`tehranTime.test.ts` tests still pass unchanged.

## 4. Task 4 — the vitest upgrade, honestly

`npm audit fix --force` upgraded `vitest` `2.1.8` → `4.1.10` (a declared
SemVer major, exactly as `npm audit` warned it would be). `npm audit`
after: **0 vulnerabilities** (was 3 moderate, 1 high, 1 critical — vitest
critical, vite high, esbuild moderate, all transitively resolved by the
single `vitest` bump).

**The first full run after the upgrade failed: 4 of 195 tests failed**,
all sharing one pattern — a test asserting "invalid input blocks
submission, the mutation must not have been called" instead saw a *prior*
test's leftover call recorded against the same mock (confirmed by
inspecting the failure output: the "unexpected" call's exact arguments
matched a **different**, earlier test in the same file byte-for-byte,
including a Tehran-converted timestamp the failing test's own form never
touched). Root cause: `vi.spyOn` on a module export that a prior test in
the same file already replaced returns *that same mock instance* rather
than a fresh one, so its accumulated call history leaks forward unless
something clears it between tests — a latent test-isolation gap the
vitest 2→4 upgrade surfaced (a difference in `vi.spyOn` re-invocation
semantics or scheduling), not one it introduced by design.

**No file under `frontend/src/` needed to change to fix this.** The fix is
`restoreMocks: true`, one line in `vitest.config.ts` (a sibling of `src/`,
not inside it) — this stayed inside the upgrade's own stop condition, so
no revert was needed. Verified deliberately, not assumed: reverted the
config line, reproduced all 4 failures again, confirmed each was the exact
same leaked-call pattern, then restored the fix.

**Full suite run 3 times after the fix, per the phase's instruction — all
195 tests passed, all 3 runs:**

```
Run 1: Test Files  48 passed (48) | Tests  195 passed (195)
Run 2: Test Files  48 passed (48) | Tests  195 passed (195)
Run 3: Test Files  48 passed (48) | Tests  195 passed (195)
```

**`npm run build` output size: unaffected**, byte-for-byte identical
chunk sizes (`vendor-react` 103.80 kB, `vendor-mui` 318.78 kB,
`vendor-data` 174.65 kB, `index` 205.20/205.13 kB — the ~70-byte `index`
delta traces to Task 1's own code change, not the upgrade) — expected,
since `vitest`/`vite`/`esbuild` are dev-only dependencies with zero
presence in the production bundle.

## 5. Task 3 — a Development seed-data failure no longer crashes the host

`DevelopmentSeeder.SeedAsync` ran unguarded before `app.Run()`
(Phase 2.5's own finding: a wrong database password crashed the whole
process before it ever started listening). `Program.cs` now wraps the
call in a `try`/`catch` that logs the exception loudly (via
`app.Logger.LogError`) and lets the host start anyway.

Two failure modes are deliberately excluded from that catch, per the
task's own two named exceptions, and remain fatal:
- **A missing `Seed:AdminPassword`** — now `DevelopmentSeederConfigurationException`,
  thrown before any write, an explicit misconfiguration per Phase 2's own
  requirement to fail loudly.
- **A failure after the seeder's first `SaveChangesAsync` already
  committed** (Admin/Tutor/Student/Parent-Guardian) but before its second
  (Relationship/Availability Slot) — now
  `DevelopmentSeederPartialFailureException`. This case genuinely *was*
  distinguishable (contrary to the task's own anticipated fallback of
  "say so and keep current behaviour"): the seeder's idempotency check
  only looks for the Admin account's presence, so a retry after a partial
  failure like this would see it already there and silently skip
  re-seeding the missing Relationship/Availability Slot forever. The
  second phase of `SeedAsync` (from `Relationship.Invite(...)` through its
  own `SaveChangesAsync`) is now wrapped and re-thrown as this type
  specifically so `Program.cs` can tell the two failure classes apart.

**Verified as a real regression test, not just written and trusted**: the
new `DevelopmentSeederFailureTests.Host_starts_and_health_is_reachable_when_the_seed_database_is_unreachable`
was run against the code *before* this fix (via `git stash`) and
confirmed to fail — the real failure mode is more interesting than
initially assumed: a first attempt using `IWebHostBuilder.ConfigureAppConfiguration`
to point at an unreachable database was silently overridden by this
machine's real `ConnectionStrings:TutorFlow` user-secret (`WebApplicationBuilder.CreateBuilder`
materializes its configuration, including user secrets, before the test
factory's hook runs), so the "unreachable" database was actually the real
one and the test passed for the wrong reason regardless of the fix. Fixed
by setting `ConnectionStrings__TutorFlow`/`Seed__AdminPassword` as
environment variables instead (environment variables outrank user secrets
in ASP.NET Core's own configuration precedence), pointed at `127.0.0.1:1`
(nothing listens there, so Npgsql fails fast rather than timing out). With
that fix, the un-patched code failed exactly as expected — an unhandled
`Npgsql.NpgsqlException` propagating out of `Program.cs`'s top-level
statements and crashing `WebApplicationFactory`'s host startup — and the
patched code passes.

## 6. `scripts/verify.ps1`, real output, unmodified for this run

Run against `TUTORFLOW_TEST_CONNECTION` pointed at the real local
`tutorflow_test` PostgreSQL 17 database already configured on this
machine (discovered via the same `dotnet user-secrets` mechanism
README's "Database setup" describes):

```
=================== VERIFY SUMMARY ===================
dotnet restore                                PASS     0.7s
dotnet build (0 warnings)                     PASS     7.8s
Backend: dotnet test TutorFlow.sln (run 1/2)  PASS    22.9s
Backend: dotnet test TutorFlow.sln (run 2/2)  PASS    20.0s
Backend: Postgres integration tests           PASS     4.0s
npm ci                                        PASS    26.3s
Frontend: npm run lint                        PASS    10.1s
Frontend: npm run build                       PASS    16.5s
Frontend: npm test -- --run                   PASS    53.0s
-------------------------------------------------------
RESULT: PASS (9/9 steps green)
```

Backend totals across both determinism-guard runs: Domain 53 + Application
218 + Infrastructure 61 + Web 176 (up one from Phase 3's 175 —
`DevelopmentSeederFailureTests`) = **508 tests**, both runs, plus the 7
Postgres-backed integration tests, all passing against the real database
this time. Frontend: 195 tests, confirmed 3x in Section 4 alone (5x total
this session).

## 7. Merge recommendation

**Safe to merge to `main`.** Every absolute rule held: storage and wire
format stayed UTC (no Domain entity, migration, or API contract shape
changed — Tasks 1–3 touched only the Web/frontend presentation layer and
a dev-only seeding path); no i18n introduced; no test weakened, skipped,
or deleted (two intentionally-inverted assertions, each documented in its
own commit); no task expanded beyond its stated scope (Task 4's one
config-only fix stayed inside its own stop condition and didn't trigger a
revert). Unlike every prior phase report in this repo, this one has a
genuine, current, all-green `verify.ps1` run against real PostgreSQL to
point to — no caveat needed.
