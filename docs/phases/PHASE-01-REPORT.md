# Phase 1 Report — Green Pipeline (Restore Trust in the Test Suite)

Branch: `main` | Commits this phase: one `fix:` commit (Task 1 isolation fix), plus this report's own `docs:` commit

**Status: STOPPED after Task 1. Tasks 2–4 not started.** Task 1's own exit
criterion — 10 consecutive fully green runs — was proven unreachable within
this phase's allowed scope (test infrastructure only, no `backend/src`
changes). The blocking cause is a real, severe, deterministic production bug,
not flakiness. Section 3–4 below document the full investigation; Section 6
explains the stop decision.

## 1. Clean Architecture validation

**Zero files under `backend/src/` or `frontend/src/` were touched.** Every
change this phase is confined to `backend/tests/Web.Tests/` (8 files) plus
this report. Verified via `git diff --stat backend/tests/` before committing:

```
backend/tests/Web.Tests/AuditEndpointsTests.cs           |  6 +++++-
backend/tests/Web.Tests/AuthEndpointsTests.cs            |  6 +++++-
backend/tests/Web.Tests/DiscoveryEndpointsTests.cs       |  6 +++++-
backend/tests/Web.Tests/IdentityEndpointsTests.cs        |  6 +++++-
backend/tests/Web.Tests/ObservabilityTests.cs            |  8 +++++++-
backend/tests/Web.Tests/OversightEndpointsTests.cs       |  6 +++++-
backend/tests/Web.Tests/SchedulingEndpointsTests.cs      |  6 +++++-
backend/tests/Web.Tests/TutorFlowWebApplicationFactory.cs| 16 ++++++++++++++++
8 files changed, 53 insertions(+), 7 deletions(-)
```

No test assertion was loosened, deleted, retried, or made to accept a wider
range of outcomes. `dotnet build TutorFlow.sln` after the change: **0
Warnings, 0 Errors.**

## 2. Repository convention validation

One commit made on `main` for the Task 1 fix (`fix: ...`, see below), plus a
separate `docs:` commit for this report — mirroring the Phase 0.5 convention
of giving the report its own commit. No `bin/`, `obj/`, or `node_modules/`
artifacts staged. No database migration run; no real database connected to
(SQLite in-memory only, as the existing test factory already used).

## 3. Investigation — Step 1 characterization and Step 2 hypothesis

### Step 1: reproduce and characterize (before any change)

Ran `dotnet test TutorFlow.sln --no-build` 5 times consecutively, full output
captured each time. Result, **contradicting the task's own premise**:

| Run | Total (Web.Tests) | Passed | Failed | Failed test set |
|---|---|---|---|---|
| 1 | 146 | 139 | 7 | identical set, see below |
| 2 | 146 | 139 | 7 | identical set |
| 3 | 146 | 139 | 7 | identical set |
| 4 | 146 | 139 | 7 | identical set |
| 5 | 146 | 139 | 7 | identical set |

The **exact same 7 tests failed in all 5 runs**, every time, with the same
exceptions:

1. `DiscoveryEndpointsTests.SearchTutors_filters_by_subject_and_finds_a_matching_approved_tutor` — `Assert.Contains` failure, collection empty
2. `AuthEndpointsTests.Admin_reset_password_invalidates_every_existing_session_immediately` — `HttpRequestException: 401 Unauthorized` (login with the "new" password fails)
3. `AuthEndpointsTests.Repeated_wrong_passwords_lock_the_account_and_the_correct_password_then_fails_too` — expected `LoginCommand.AccountLocked`, got `LoginCommand.InvalidCredentials`
4. `IdentityEndpointsTests.GetTutorById_returns_success_for_an_unauthenticated_caller_when_discoverable` — `HttpRequestException: 401 Unauthorized`
5. `IdentityEndpointsTests.GetPendingTutors_excludes_an_approved_tutor` — `Assert.DoesNotContain` failure (the just-approved tutor is still "pending")
6. `IdentityEndpointsTests.GetPendingTutors_includes_a_freshly_registered_tutor` — `Assert.Contains` failure (freshly-registered tutor missing from list)
7. `SchedulingEndpointsTests.GetTutorAvailabilitySlots_returns_success_for_any_authenticated_caller_when_discoverable` — `HttpRequestException: 403 Forbidden`

This directly contradicts the brief's premise of "~7 failures per run,
different tests each time." In this environment (12 logical processors,
.NET 9.0.300, Windows), the outcome was **100% deterministic**, not flaky.
This does not mean the original report was fabricated — see the "different
tests each time" reconciliation at the end of this section — but it means
the evidence here required following where it led rather than assuming the
original framing was correct.

Additional characterization:
- **Same 7 failures reproduced identically running `Web.Tests` alone** (`dotnet test tests/Web.Tests/TutorFlow.Web.Tests.csproj`), and also **running only `IdentityEndpointsTests` in isolation** (`--filter FullyQualifiedName~IdentityEndpointsTests`, 59 tests, same 3 of its tests failed). Ruled out cross-test-class interference as a factor (each class's `TutorFlowWebApplicationFactory` opens its own private `SqliteConnection`; `:memory:` without `cache=shared` is never visible to another connection, so classes cannot pollute each other's data).
- **Running a single failing test completely alone** (`--filter FullyQualifiedName=...GetTutorById_returns_success_for_an_unauthenticated_caller_when_discoverable`, 1 test in the whole run) — **still failed**, with a full, uncontaminated EF Core command log showing every SQL statement issued. This is the decisive piece of evidence in Section 3 below.
- Not investigated: `--parallel none` vs default, since the single-test-alone reproduction already ruled out any form of parallelism or shared-state interference as the cause for 6 of the 7 failures.

### Step 2: hypothesis — stated before making any fix

**The brief's hypothesis** (that `IClassFixture<TutorFlowWebApplicationFactory>`
shares one SQLite connection across every test method in a class, so
accumulating rows make count/list assertions order-dependent) was **partially
confirmed and partially refuted** by the evidence:

**Confirmed, for exactly 1 of the 7 failures** —
`GetPendingTutors_includes_a_freshly_registered_tutor`. This test registers a
tutor, then asserts it appears in `GET /tutors/pending`. `TutorRepository.GetPendingAsync`
(`backend/src/Infrastructure/Identity/Repositories/TutorRepository.cs:34-46`)
runs `Where(t => !t.IsApproved).Skip(...).Take(pageSize)` with **no `OrderBy`
clause at all**, and `PageRequest.DefaultPageSize` is 20
(`backend/src/Application/Common/PageRequest.cs:11`). Because
`IClassFixture` builds one `TutorFlowWebApplicationFactory` (one open
`SqliteConnection`) per test **class**, and every test method in
`IdentityEndpointsTests` shares that same connection/database for the whole
class's lifetime, by the time this specific test runs, dozens of pending
tutors already exist from earlier sibling tests in the same file. With no
`ORDER BY`, whether the newly-registered tutor lands within `Skip(0).Take(20)`
is not guaranteed and depends on execution order and row-accumulation volume.
**Proof:** running this one test in total isolation (nothing else in the
class executes first) made it pass reliably. This is a genuine, in-scope test
isolation bug and is what Task 1 was written to catch.

**Refuted as the cause for the other 6 of 7 failures.** Running each of those
tests in total isolation — a single test method, alone, in an assembly
otherwise producing zero rows — **still failed, identically, every time**.
Shared class-level state cannot explain a failure that reproduces with only
one test method executing in the whole process. The real cause, found by
reading the full EF Core command log for the single-test-alone run of
`GetTutorById_returns_success_for_an_unauthenticated_caller_when_discoverable`,
is a genuine, deterministic **production defect**, not a test-isolation
problem:

> **`EfUnitOfWork.SaveChangesAsync` never persists mutations made to
> aggregates fetched via a no-tracking repository read.**
>
> Every Identity repository (`TutorRepository`, `StudentRepository`,
> `ParentGuardianRepository`, `AdminStaffRepository`, `RelationshipRepository`
> — confirmed by grep across `backend/src/Infrastructure/Identity/Repositories/`)
> fetches single entities via `.AsNoTracking()`. Command handlers such as
> `ApproveTutorCommandHandler` (`backend/src/Application/Identity/Handlers/ApproveTutorCommandHandler.cs:29-49`)
> and `LoginCommandHandler` (`backend/src/Application/Identity/Handlers/LoginCommandHandler.cs:115`,
> `:144`) fetch the aggregate this way, mutate it in memory (`tutor.Approve()`,
> `account.RecordFailedLoginAttempt(now)`), and pass it to
> `IUnitOfWork.SaveChangesAsync(touchedAggregates)`. But
> `EfUnitOfWork.SaveChangesAsync` (`backend/src/Infrastructure/Persistence/EfUnitOfWork.cs:30-50`)
> never calls `_dbContext.Update(aggregate)` or otherwise re-attaches the
> touched aggregates — it only dispatches domain events, clears them, and
> calls the bare `_dbContext.SaveChangesAsync()`. Since the entity was never
> tracked in the first place, EF Core's change tracker has nothing to persist,
> and **no `UPDATE` statement is ever generated.** The mutation exists only in
> the handler's local variable and vanishes when the request ends.
>
> **Direct proof:** the full SQL command trace for the isolated
> `GetTutorById_...discoverable` test shows, for the `POST /tutors/{id}/approve`
> call: a `SELECT` of the tutor by id, then an `INSERT INTO "AuditEntries"`
> (the approval's audit record) — and **no `UPDATE "Tutors" SET "IsApproved"`
> anywhere in the log.** The subsequent `GET /tutors/{id}` re-reads the tutor
> fresh from the database, finds `IsApproved` still `false`, and correctly
> returns `401 Unauthenticated` per `GetTutorByIdQueryHandler`'s own
> discoverability check — the *test* is correct; the *product* silently
> failed to approve the tutor.
>
> This single mechanism explains all 6 non-isolation failures:
> - **Approve never persists** → `GetTutorById_...discoverable` (401),
>   `GetPendingTutors_excludes_an_approved_tutor` (tutor still "pending"),
>   `SearchTutors_filters_by_subject...` (search filters on `IsApproved`,
>   never matches), `GetTutorAvailabilitySlots_...discoverable` (403, same
>   discoverability check)
> - **`RecordFailedLoginAttempt` never persists** →
>   `Repeated_wrong_passwords_lock_the_account...` (the failed-attempt
>   counter never increments in the database, so the account can never
>   reach the lockout threshold; the 6th attempt is evaluated as an ordinary
>   wrong password, not a locked account)
> - **Password reset never persists** →
>   `Admin_reset_password_invalidates_every_existing_session_immediately`
>   (the new password hash never reaches the database, so logging in with it
>   afterward fails with 401 — the old hash is still active)
>
> **Why this was invisible everywhere except `Web.Tests`:** `Application.Tests`
> exercises the same handlers, but against `FakeUnitOfWork`
> (`backend/tests/Application.Tests/TestDoubles/FakeUnitOfWork.cs`) — a
> hand-rolled double that only increments a call counter and clears domain
> events; it does not simulate EF Core's change-tracking semantics at all.
> Those tests correctly verify the handler *orchestration* (SaveChanges was
> called once, the in-memory aggregate's own state flipped) but cannot
> detect that a real persistence layer would silently drop the write.
> `Web.Tests` is the **only** test project in this solution that exercises
> the real `EfUnitOfWork` against a real EF Core provider — and it has
> apparently been catching this defect all along, dismissed as "flaky."

**Reconciling "different tests each run" with what we observed:** xUnit's
default test-case discovery order for a compiled assembly is stable
(deterministic per build), which is consistent with this environment
reproducing the identical 7 every time. A CI environment with a different
.NET/xUnit build, different assembly layout, or genuine cross-class
parallelism timing variance could plausibly see a *different* subset of
tests affected by the row-accumulation issue (finding #6 above) on different
runs — but the same underlying non-persistence defect (findings #1–5) would
still fail deterministically regardless of environment, since it does not
depend on shared state at all. It is plausible the original audit's "~7
failures, different set" observation was itself an amalgam of these two
distinct problems observed across different machines/runs, not one single
flaky phenomenon.

## 4. Fix applied and before/after runtime

**Only the one confirmed, in-scope test-isolation defect was fixed.** The
6 failures caused by the `backend/src` persistence defect are untouched —
per this phase's absolute rule, a production-code change was not made
without owner authorization (see Section 6).

**Fix:** `TutorFlowWebApplicationFactory` gained a `ResetDatabaseAsync()`
method (`EnsureDeletedAsync` + `EnsureCreatedAsync` against the already-open
shared connection). Each of the 7 test classes now implements `IAsyncLifetime`
and calls `_factory.ResetDatabaseAsync()` in `InitializeAsync()`. Because
xUnit constructs a fresh instance of the test class (and calls
`IAsyncLifetime.InitializeAsync()`) before every `[Fact]`, every test method
now starts against a genuinely empty database, while the expensive part —
building the `WebApplicationFactory`/host once — is still paid only once per
class, not once per test.

This was **preferred over per-test connections/hosts** (the brief's first
option) because rebuilding the entire ASP.NET Core host per test method
(146 times) would have meaningfully increased runtime for comparatively
little benefit, given the *fix* needed is "no leftover rows," not "no
leftover host" — dropping and recreating schema on the same open connection
achieves that at a fraction of the cost. No xUnit collection/parallelism
change was needed since cross-class sharing was already ruled out (Section 3).

**Proof — 10 consecutive full-suite runs after the fix:**

| Run | Total (Web.Tests) | Passed | Failed |
|---|---|---|---|
| 1–10 | 146 | 140 | 6 (identical set every run) |

The isolation-caused failure (`GetPendingTutors_includes_a_freshly_registered_tutor`)
is now gone in all 10 runs — confirming the fix works for the problem it
targets. The **same 6 tests from the persistence defect fail in all 10
runs**, identically — confirming, as expected, that a test-isolation fix
cannot and does not mask or fix a real product defect. **10/10 fully green
was not achieved and is not achievable without a `backend/src` change.**

**Runtime, `Web.Tests` project only** (the only project this phase's change
could affect):
- Before (5 runs): 32.11s, 17.99s, 17.90s, 17.76s, 21.78s → mean **21.51s**
- After (10 runs): 18.29s, 22.12s, 19.28s, 22.33s, 18.53s, 18.86s, 22.33s, 19.42s, 19.64s, 18.55s → mean **19.94s**

No material regression — the per-test `EnsureDeleted`/`EnsureCreated` pair
costs single-digit milliseconds against an already-open in-memory connection
with 10 tables, and is well within normal run-to-run variance (the "before"
mean is actually higher, skewed by a 32s first-run cold-start outlier).
Full 4-project suite (`dotnet test TutorFlow.sln`, no-build): before ~19–32s
per run, after 20–24s per run — consistent with the above, no material change.

## 5. Task 2–4: not started

Per this phase's own rule ("Tasks run in order. Do not start Task 3 until
Task 1 is proven fixed") and Task 1's own exit bar ("Do not declare success
on fewer than 10 green runs"), Task 1 is not provably fixed — 6 of 7 original
failures remain, deterministically, for a reason outside this phase's
permitted scope. Building `scripts/verify.ps1` (Task 2) or CI workflows
(Task 3) on top of a suite known to fail 6 tests every time would either (a)
require quietly excluding those 6 tests from the new verification gate,
which is exactly the kind of scope-creep-via-omission this phase exists to
prevent, or (b) ship a verification script whose very first real run reports
FAIL, with no phase-appropriate way to explain why. Task 4 (`vitest` upgrade)
is unrelated to backend work and was never reached.

## 6. Open decision for the owner — a production defect, not a test problem

This phase's absolute rules require stopping here: *"DO NOT change any file
under `backend/src/`... If you become convinced a production-code change is
required, STOP and report why."* Section 3 documents the evidence. To
restate plainly: **`ApproveTutor`, `SuspendTutor` (untested end-to-end, but
same code shape), `Admin reset password`, and the account-lockout mechanism
all currently fail to persist their state changes in the real (Postgres, in
production) EF Core path** — they succeed at the HTTP layer (`Result.Success()`
is returned, since the in-memory mutation and the `Result` construction both
happen before the no-op save), but the database is never actually updated.
This is a correctness defect in the shipped product, not a test artifact —
`Web.Tests` happened to be the only test project positioned to catch it,
and its failures were being dismissed as "flaky" instead of investigated.

**Proposed fix** (for the owner to authorize as its own change, likely its
own ADR addendum since it affects `EfUnitOfWork`, a component named directly
in `docs/adr/ADR-013-persistence-technology.md` and
`docs/adr/ADR-015-transaction-boundary-confirmation.md`): either (a) have
`EfUnitOfWork.SaveChangesAsync` call `_dbContext.Update(aggregate)` for each
touched aggregate before calling `_dbContext.SaveChangesAsync()`, or (b) stop
using `.AsNoTracking()` for repository reads that feed into a
fetch-mutate-save command handler (keeping `.AsNoTracking()` only for
pure read-side queries that never round-trip through `SaveChangesAsync`).
Recommend (a): it fixes the defect at the single shared choke point
(`EfUnitOfWork`) rather than auditing and changing every repository method,
and preserves `.AsNoTracking()`'s read-performance benefit for genuine
read-only queries. This is squarely a `backend/src` change and is out of this
phase's permitted scope — flagging for owner decision, not implementing.

**Severity/impact estimate:** High. Every Identity-module command handler
that follows the fetch-mutate-save pattern is affected (Approve, Suspend,
Admin reset-password, login lockout tracking confirmed by direct evidence;
`SetHourlyRate`/`SetSubject`/`SetLanguage`/`SetLocation`/`SetOfferedDurations`
share the identical `AsNoTracking()` + `EfUnitOfWork` shape and were not
individually proven broken in this phase, but should be assumed at risk
until re-verified end-to-end after the fix). Scheduling & Audit repositories
(`SessionRepository`, `AvailabilitySlotRepository`,
`RelationshipRepository`) show the same `.AsNoTracking()` pattern and were
not audited for the same defect in this phase — recommend a full audit
alongside the fix.

## 7. Merge recommendation

**Merge the Task 1 isolation fix — it is correct, proven, in-scope, and a
genuine improvement** (removes the one real cross-test contamination bug,
with no material runtime cost). **Do not merge this as "Phase 1 complete"**
and do not proceed to Phase 2 or to Tasks 2–4 of this phase until the owner
decides how to handle the persistence defect in Section 6 — either by
authorizing the `backend/src` fix (as its own reviewed, tested change, per
this repository's normal Definition of Done) or by explicitly descoping
Task 1's exit bar. Whichever the owner chooses, this report's evidence
(single-test-isolated reproduction with full SQL command trace) should be
sufficient to act on without further investigation.
