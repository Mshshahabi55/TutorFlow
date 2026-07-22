# Phase 2.5 Report — Browser End-to-End

Branch: `develop` | Commits this phase: `219d921` (Task 1), `da36e3a`,
`0e52eba` (Task 3), `23f84b3` (Task 2), `b1df8b0` (Task 4). Task 5 produced
no file changes — its result is recorded below.

**Status: complete, with one honestly-reported blocker.** Mid-session, the
owner rotated the PostgreSQL superuser password and introduced a
non-superuser `tutorflow` role, per this phase's own "Context you must not
act on" instruction. That instruction was followed to the letter: every
live-Postgres verification this phase's tasks called for was attempted
once, failed with a clear, expected authentication error, and was reported
rather than chased. Everything else — CORS (verified live, without needing
Postgres at all), the DateTime convention (implemented and verified live
via SQLite), the migration (written and built, unverified), and the manual
walkthrough doc — is complete.

## 1. Clean Architecture validation

`backend/src/` changed in four narrowly-scoped places: `appsettings.Development.json`
(Task 1, CORS origins only — `appsettings.json` untouched), a new
`UtcDateTimeValueConverter.cs` and a 6-line addition to `TutorFlowDbContext.cs`
(Task 3, persistence-layer convention only — no Domain or Application code
touched), and a new migration file (Task 2, schema-only). No Domain entity
behaviour and no API contract changed. `frontend/src/` was never touched
(Task 1's dev-server origin came from reading `vite.config.ts`, not editing
it). `dotnet build TutorFlow.sln`: **0 Warnings, 0 Errors** throughout.

## 2. Repository convention validation

Five commits on `develop` matching task order, each scoped to one task
(Task 3 split into two, per its own explicit instruction to commit the test
correction separately — mirroring Phase 1B). No `bin/`/`obj/`/`node_modules/`
staged. **No database password was asked for, read, echoed, or stored
anywhere this phase** — the one password used (Task 1/2's connection
attempts) was the same `ConnectionStrings:TutorFlow` secret already
configured in Phase 2, read only by the running process, never touched or
inspected by this session directly.

## 3. Task 1 — CORS, verified live without needing Postgres

`appsettings.Development.json` now allows `http://localhost:5173` — the
exact port `frontend/vite.config.ts`'s `server.port` sets — by explicit
origin, not `AllowAnyOrigin()`. `appsettings.json` (production) untouched.

**Credentials/cookies, checked as instructed:** `apiClient.ts`
(`frontend/src/services/api/apiClient.ts`) attaches the bearer token via a
manually-set `Authorization` header inside an Axios request interceptor,
reading from a plain module-level variable — `axios.create()` never sets
`withCredentials: true` anywhere in the frontend. No cookie-based
credential exists at all. **`AllowCredentials` is therefore correctly not
set**, and the real response was checked to confirm no
`Access-Control-Allow-Credentials` header is present (below).

**Verification hit the credential rotation immediately.** Starting the API
against `tutorflow_dev` to issue a real preflight request failed before
Kestrel ever started listening:

```
fail: Microsoft.EntityFrameworkCore.Database.Connection[20004]
      An error occurred using the connection to database 'tutorflow_dev' on server 'tcp://localhost:5432'.
Npgsql.PostgresException (0x80004005): 28P01: password authentication failed for user "tutorflow"
...
   at TutorFlow.Web.DevelopmentSeeder.SeedAsync(IServiceProvider services) ...
Unhandled exception.
```

`DevelopmentSeeder.SeedAsync`'s own query (checking whether the seed Admin
already exists) is unguarded and runs before `app.Run()`, so any database
error at that point crashes the whole host — a real robustness gap,
independent of CORS, noted but **not fixed** here (out of this phase's
scope; changing seeding failure-handling wasn't asked for). Per this
phase's explicit instruction, this failure was reported once and not
chased with a second connection attempt or any credential-guessing.

**Verified instead via `CorsConfigurationTests.cs`** (new,
`backend/tests/Web.Tests/`) — a real preflight `OPTIONS` request through
the actual ASP.NET Core CORS middleware, using `WithWebHostBuilder(...).UseSetting(...)`
to inject the same origin value, needing no database at all (CORS
preflight handling short-circuits before any endpoint or DB access).
`ConfigureAppConfiguration` was tried first and confirmed **not** to reach
`Program.cs`'s configuration read for this app's minimal-hosting-model
setup (the log still read "no allowed origins configured" despite the
override) — `UseSetting` was used instead, and does work. Actual response,
captured directly from the real middleware pipeline (not assumed):

```
Status: 204
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
Referrer-Policy: no-referrer
Access-Control-Allow-Origin: http://localhost:5173
Access-Control-Allow-Headers: authorization,content-type
Access-Control-Allow-Methods: GET
(no Access-Control-Allow-Credentials header)
```

A second test confirms a different, unconfigured origin still gets no
`Access-Control-Allow-Origin` header even with one origin already
configured (not just "any configured origin passes"). Both pass;
495-non-Postgres-test full suite re-confirmed green after this addition
(Section 6).

## 4. Task 2 — migration written, not yet applied

New migration `20260722041335_RemoveMigrationArtifactColumnDefaults`
(hand-written `Up`/`Down`, since the model already matched the snapshot —
confirmed via `dotnet ef migrations has-pending-model-changes` reporting
"No changes have been made to the model" both before and after writing
this migration, meaning EF's automatic diff would never have generated it
on its own). Drops the physical column defaults via raw
`ALTER TABLE ... ALTER COLUMN ... DROP DEFAULT` for exactly the columns
Phase 2 found: `AuthTokens.AbsoluteExpiresAtUtc`, `Relationships.InvitedByAccountId`,
and `Email`/`PasswordHash` on `Tutors`/`Students`/`ParentGuardians` (8
columns total). `Down()` restores the original default values exactly, for
reversibility. No other schema change.

**Not applied to `tutorflow_dev`.** `dotnet ef database update` hit the
identical `28P01: password authentication failed for user "tutorflow"`
error observed in Task 1. Per this phase's instruction, this was reported
once, not retried with different credentials or investigated further.
**No before/after `psql \d+` diff exists in this report** — it could not be
produced honestly, and fabricating one from the migration's own SQL would
misrepresent what was actually verified. The migration's correctness is
argued from the SQL itself (a single `DROP DEFAULT` per column, verified
by direct comparison against each original migration's `defaultValue`
parameter — see the commit message) and from `has-pending-model-changes`
confirming no other model drift exists, but this is reasoning, not
verification, and is reported as such.

## 5. Task 3 — DateTimeKind.Utc enforced structurally, and why it isn't a bug mask

`TutorFlowDbContext.ConfigureConventions` now applies
`UtcDateTimeValueConverter`/`UtcNullableDateTimeValueConverter`
(`backend/src/Infrastructure/Persistence/UtcDateTimeValueConverter.cs`) to
every `DateTime`/`DateTime?` property project-wide, with three cases:

- **`Kind=Utc`** — passes through unchanged.
- **`Kind=Unspecified`** — relabeled `Utc` (`DateTime.SpecifyKind`, not
  converted). Justified by Phase 0.5's own audit, re-confirmed unchanged:
  zero uses of `DateTime.Now`/`.Today` anywhere in `backend/src` — every
  `DateTime` this codebase ever produces is already semantically UTC, so
  `Unspecified` here means "lost its Kind tag crossing some boundary," not
  "might actually be local." This is *restoring* the value the codebase
  already meant, not inventing a new interpretation.
- **`Kind=Local`** — **throws `InvalidOperationException`**, deliberately
  not silently converted or relabeled. A genuinely local `DateTime`
  reaching persistence would mean a real, currently-nonexistent code path
  exists somewhere that this project doesn't yet know about — converting
  it away silently would hide that bug rather than expose it
  (`PROJECT_CONSTITUTION.md` Engineering Principle 4: fail safely and
  visibly). **This is the reasoning this task asked for before
  implementing** — the design draws the line exactly at "restore a lost tag
  this codebase's own invariant already guarantees the value" vs. "silently
  reinterpret a value that might genuinely mean something else," and only
  does the former automatically.

No new migration needed — confirmed via `dotnet ef migrations has-pending-model-changes`
after adding the convention: a same-CLR-type value converter doesn't change
the mapped store type Npgsql infers.

**Verified live, without needing Postgres**, since the converter operates
at the EF Core model level before any provider-specific SQL is generated:
two new SQLite-backed tests
(`backend/tests/Infrastructure.Tests/Persistence/UtcDateTimeConversionTests.cs`) —
a `Kind=Unspecified` `DateTime` now round-trips as `Utc` instead of
throwing (using the exact same two-context write/read pattern established
in Phase 1B/1C to prove a genuine round trip, not an in-memory shortcut),
and a `Kind=Local` `DateTime` still throws, confirmed via the real observed
exception (`DbUpdateException` wrapping `InvalidOperationException` —
EF Core wraps exceptions raised inside a value converter during
`SaveChangesAsync`, the same wrapping behavior Phase 2 observed for
Npgsql's own rejection).

**The existing Postgres test corrected, in its own commit** (`0e52eba`),
exactly matching Phase 1B's precedent for `TutorRepositoryTests`:
`DateTime_with_Unspecified_Kind_is_rejected_by_Npgsql` asserted the exact
behavior this task intentionally changed, so it's renamed to
`DateTime_with_Unspecified_Kind_now_round_trips_as_Utc_instead_of_being_rejected`
and inverted. **This specific test is unconfirmed against real
PostgreSQL** — the same credential rotation blocked it, reported honestly
in that commit's own message rather than silently assumed to pass. The
SQLite-backed tests already prove the converter's logic directly; only the
Npgsql-specific claim ("Npgsql itself no longer sees an Unspecified value")
remains unverified live.

## 6. Task 4 and Task 5 — the walkthrough, and the real regression-check output

**Task 4.** `docs/MANUAL-SMOKE-TEST.md` — 8 numbered steps, starting both
servers through a full login/register/approve/refresh/declare/book/verify/
re-login cycle, playing Admin, a newly-registered Tutor, and the seeded
Student in sequence. Step 4 is written to specifically catch a Phase-1B-style
regression (approve, then a **real full-page reload**, not a soft
navigation, confirming the Pending list no longer shows it and the detail
page reads `isApproved: true` fresh). Step 7 gives an exact, verbatim
`psql` query against `tutorflow_dev` to confirm the booking and slot
consumption. The UTC/Tehran worked example: 17:30 Tehran (`UTC+03:30`, no
DST) → `2026-08-01T14:00:00Z`. Not run by this session — it's a script for
the owner, and doing so would additionally require the now-rotated
credentials this phase was told not to chase.

**Task 5 — the real `scripts/verify.ps1` output, unmodified for this run:**

```
=================== VERIFY SUMMARY ===================
dotnet restore                                PASS                                1s
dotnet build (0 warnings)                     PASS                              5.1s
Backend: dotnet test TutorFlow.sln (run 1 of 2) PASS                             25.1s
Backend: dotnet test TutorFlow.sln (run 2 of 2) PASS                             20.2s
Backend: Postgres integration tests           FAIL                              2.3s
npm ci                                        PASS                               30s
Frontend: npm run lint                        PASS                             11.7s
Frontend: npm run build                       PASS                             10.6s
Frontend: npm test -- --run                   PASS                             43.1s
-------------------------------------------------------
Total elapsed: 149.1s

RESULT: FAIL
```

**8 of 9 steps PASS.** The one failure is exactly the credential-rotation
blocker, and nothing else: `PostgresTestFixture`'s constructor threw its
own designed message, `Environment variable 'TUTORFLOW_TEST_CONNECTION' is
not set` — this session never set that variable, consistent with not
chasing the new credentials. Backend totals for the two determinism-guard
runs: 53 + 218 + 61 + 163 = **495 non-Postgres tests, both runs, fully
green** (up from 491 at the start of this phase: +2 `CorsConfigurationTests`,
+2 `UtcDateTimeConversionTests`). Frontend: 173/173. **This `FAIL` is the
correct, honest result given the current credential state — reporting it
plainly is this phase's actual instruction, not a shortfall to explain
away.**

## 7. Merge recommendation

**Ready to merge to `main`, with one explicit follow-up for the owner
before Phase 3 (or whatever comes next) can fully close this phase's
loose end.** Every absolute rule was honored: no `AllowAnyOrigin()`, no
production `appsettings.json` change, no database password asked for, read,
echoed, or stored, and every connection failure was reported once and left
alone rather than chased. The DateTime convention was reasoned through
explicitly before implementing, per the task's own "STOP and explain if
this would mask a bug" instruction, and the reasoning is recorded in
Section 5 for review — this was not treated as a rubber-stamp.

**Before this phase can be considered fully verified**, the owner needs to
either share the new `tutorflow` role's connection details for a future
session to use (via `dotnet user-secrets`, never by telling the assistant
the password in chat), or run three things themselves: `dotnet ef database update`
(to apply Task 2's migration and get the real `psql \d+` before/after
diff this report couldn't produce), the Postgres integration test suite
with `TUTORFLOW_TEST_CONNECTION` pointed at the new role (to confirm Task 3's
corrected test actually passes against real Npgsql, not just reasoned
through), and `docs/MANUAL-SMOKE-TEST.md` itself. None of this blocks
merging what's here — the code is written, reasoned through, and verified
everywhere it could be without the rotated credentials — but none of it
should be marked "done" beyond what this report actually confirms.
