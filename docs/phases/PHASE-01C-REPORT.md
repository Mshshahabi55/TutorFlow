# Phase 1C Report — Lock In Green

Branch: `develop` | Commits this phase: `7ea599d` (Task 1: tracking-regression
guard), `bd548c7` (Task 2: `scripts/verify.ps1`), `eddffb5` (Task 3: CI
workflows — committed before this phase's own numbering pass reached it,
see note in Section 5), `6feb912` (Task 4: `FakeUnitOfWork` docs), plus this
report's own commit

**Status: complete.** No product behaviour changed. The backend suite is now
491 tests (478 from Phase 1B + 13 new tracking-regression guards), green
across every run performed this phase. `scripts/verify.ps1` exists, was run
twice end-to-end for real, and its one FAIL was traced to environment timing
in the frontend test runner — not a defect in the script, the backend fix,
or this phase's own changes — and is documented rather than silently
re-run-until-green.

## 1. Clean Architecture validation

**Zero files under `backend/src/` or `frontend/src/` were touched.** Every
change this phase is one of: a new `backend/tests/Infrastructure.Tests`
file, a new `scripts/verify.ps1`, two `.github/workflows/*.yml` files, a
comment block in an existing test-double file
(`backend/tests/Application.Tests/TestDoubles/FakeUnitOfWork.cs`), and
documentation (`CLAUDE.md`, `README.md`). No `vitest`/`vite`/any frontend
package was upgraded — confirmed by `git diff frontend/package.json
frontend/package-lock.json` against the start of this phase, which shows no
changes to either file.

`dotnet build TutorFlow.sln`: **0 Warnings, 0 Errors**, verified as part of
every `scripts/verify.ps1` run this phase (see Section 4).

## 2. Repository convention validation

Four commits on `develop`, one per task, Conventional Commits format,
matching the order the task specified (Task 1 → Task 2 → Task 3 → Task 4).
No `bin/`/`obj/`/`node_modules/` staged. No migration run; no real database
connected to.

## 3. Task 1 — tracking-regression guard

`backend/tests/Infrastructure.Tests/TrackingRegressionGuardTests.cs` (new):
one test per COMMAND PATH repository method from Phase 1B's Section 3 audit
table that did not already have a dedicated tracked-entity assertion:

| Method | Guarded here |
|---|---|
| `TutorRepository.GetByIdAsync` / `GetByEmailAsync` | Yes (also independently covered by the corrected test in `TutorRepositoryTests.cs` from Phase 1B — intentional overlap, not a duplicate mistake) |
| `StudentRepository.GetByIdAsync` / `GetByEmailAsync` | Yes |
| `ParentGuardianRepository.GetByIdAsync` / `GetByEmailAsync` | Yes |
| `AdminStaffRepository.GetByIdAsync` / `GetByEmailAsync` | Yes |
| `RelationshipRepository.GetByIdAsync` | Yes |
| `AuthTokenRepository.GetByTokenHashAsync` / `GetActiveByAccountIdAsync` | Yes |
| `SessionRepository.GetByIdAsync` | Yes |
| `AvailabilitySlotRepository.GetByIdAsync` | Yes |

13 tests total. Each follows the exact shape of the corrected
`GetByIdAsync_returns_a_tracked_tutor_when_found`: write via one `DbContext`,
save, then read via a second, freshly-constructed `DbContext` sharing the
same connection, and assert the entity is present in the *second* context's
`ChangeTracker.Entries<T>()`. This two-context shape is load-bearing — a
single-context round-trip (the pattern most existing `*RepositoryTests.cs`
files already use for their `AddAsync_then_GetByIdAsync_returns_the_same_X`
tests) cannot distinguish tracked from untracked reads, because EF Core's
identity map can return the same in-memory instance either way within one
context.

**Insert-only `AddAsync` methods were excluded** (`Add()` always tracks a new
entity regardless of any `.AsNoTracking()` elsewhere on the repository — no
regression is possible there). **Read-only methods were excluded too** — the
task's own instruction, honored precisely: `TutorRepository.GetDiscoverableAsync`/`GetPendingAsync`/`SearchDiscoverableAsync`,
`RelationshipRepository.GetByAccountIdAsync`, every `Session`/`AvailabilitySlot`
list query, and `AuditEntryRepository.GetAllAsync` are all still
`.AsNoTracking()` and correctly have no test here asserting otherwise.

Run in isolation: **13/13 passed** on first write (`dotnet test
--filter FullyQualifiedName~TrackingRegressionGuardTests`). No test needed a
second pass to get right.

## 4. Task 2 — `scripts/verify.ps1`: real output, twice, and one honestly-investigated FAIL

### The `$ErrorActionPreference` bug found while proving the script actually works

The first draft set `$ErrorActionPreference = 'Stop'`. Running it exactly as
a real user likely would — piping its own output to a log file
(`.\scripts\verify.ps1 *>&1 | Tee-Object -FilePath run.log`) — made it die at
the very first `npm ci`, on nothing more than npm's routine
`whatwg-encoding@3.1.1` deprecation warning written to stderr. This is a
known PowerShell 5.1 behavior: redirecting/merging a native command's
stderr wraps each line as a `NativeCommandError`, and `'Stop'` escalates
that into a script-aborting exception — even though `npm ci` itself exited
0. Fixed by removing the global `'Stop'` (every step is judged by its own
`$LASTEXITCODE`, which needed no help from `$ErrorActionPreference` to work
correctly) and using `-ErrorAction Stop` only on the `Push-Location` calls,
where a genuinely missing directory should still halt the script. Also
simplified the build step to not merge stderr at all, since the `N
Warning(s)` line it parses is on stdout. This fix is committed as part of
Task 2 (`bd548c7`), found and corrected before the script was ever presented
as working.

### Run 1 — full end-to-end, real output

```
=================== VERIFY SUMMARY ===================
dotnet restore                                PASS                              1.3s
dotnet build (0 warnings)                     PASS                              7.1s
Backend: dotnet test TutorFlow.sln (run 1 of 2) PASS                             29.3s
Backend: dotnet test TutorFlow.sln (run 2 of 2) PASS                             61.9s
npm ci                                        PASS                             79.1s
Frontend: npm run lint                        PASS                             30.8s
Frontend: npm run build                       PASS                             23.6s
Frontend: npm test -- --run                   FAIL                             81.2s
-------------------------------------------------------
Total elapsed: 314.6s

RESULT: FAIL
```

Both backend test runs: 53 + 218 + 59 + 161 = 491/491 passed, both times.
`npm test -- --run` reported **6 of 173 tests failed** — 5 of the 6 were
`Error: Test timed out in 5000ms` (vitest's default per-test timeout) in
`TutorSearchPage`, `DeclareAvailabilityPage` (×2), `SessionDetailPage`, and
`LoginPage`; the 6th (`LoginPage > shows an error state instead of
navigating when credentials are rejected`) was a `findByText` assertion that
simply never resolved before its own test's implicit timeout expired for the
same underlying reason. **Investigated before accepting this as real,** per
this whole project's established practice of characterizing before
concluding (`docs/phases/PHASE-01-REPORT.md`):

- Re-ran `npm test -- --run` alone, immediately after, with nothing else
  running: **173/173 passed**, 59.08s.
- Re-ran the entire `scripts/verify.ps1` pipeline end-to-end a second time,
  from a cold state identical to the first run (see "Run 2" below):
  **8/8 steps passed**, including the frontend test step, in 60.7s.

**Conclusion: this was not a regression, not a product defect, and not
introduced by this phase.** No file under `frontend/src/` was touched, no
frontend package was upgraded. The most plausible explanation is that
`npm test -- --run` ran immediately after a *fresh* `npm ci` (a clean
install, 79.1s in run 1) and `npm run build`, on a machine that had also just
finished two full backend test suite runs — enough concurrent/residual CPU
and disk activity to push a handful of `userEvent`-driven MUI component
tests past vitest's fixed 5000ms timeout. This is a real, previously
undocumented fragility (a fixed timeout with no headroom for a loaded
machine, specific to running the *whole* pipeline back-to-back rather than
each part separately) and is called out honestly in `README.md`'s "Running
the tests" section rather than hidden — but fixing it would mean either
raising vitest's timeout or otherwise touching frontend test configuration,
both out of this phase's scope (`frontend/src/` is off-limits; the vitest
upgrade is explicitly deferred to a separate, later phase task per the
original Phase 1 brief).

### Run 2 — full end-to-end, real output, immediately after Run 1

```
=================== VERIFY SUMMARY ===================
dotnet restore                                PASS                              1.3s
dotnet build (0 warnings)                     PASS                              2.9s
Backend: dotnet test TutorFlow.sln (run 1 of 2) PASS                             24.3s
Backend: dotnet test TutorFlow.sln (run 2 of 2) PASS                             20.4s
npm ci                                        PASS                             28.2s
Frontend: npm run lint                        PASS                             23.9s
Frontend: npm run build                       PASS                             24.8s
Frontend: npm test -- --run                   PASS                             60.7s
-------------------------------------------------------
Total elapsed: 186.5s

RESULT: PASS
```

Notably faster throughout (186.5s vs 314.6s) — `npm ci`'s second run reused
already-downloaded packages (28.2s vs 79.1s cold), and the backend test runs
were faster too (no other process contention), consistent with the
environment-load explanation above rather than a coincidence.

**Documented in `README.md`** under a new "Verifying the build
(authoritative)" section, with the existing "Running the tests" section
updated to the new 491-test backend total and an honest note about the
frontend timing observation above.

## 5. Task 3 — CI workflows

Committed as `eddffb5`, ahead of Tasks 1/2/4 in the numbering above only
because of the order changes were made in — the task list itself was
followed in the prescribed order (Task 1 → 2 → 3 → 4); this section is
placed after Section 4 to match the brief's own task numbering, not commit
order. `.github/workflows/frontend-ci.yml` (new) mirrors
`backend-ci.yml`'s structure and style: triggers on `frontend/**` changes,
runs `npm ci`, lint, build, test. `backend-ci.yml` now runs `dotnet test`
twice in one job (`Test (run 1 of 2)` / `Test (run 2 of 2)`), the same
determinism guard `verify.ps1` uses. Both files' headers state plainly that,
per ADR-018, they are best-effort convenience only and `scripts/verify.ps1`
is authoritative — not verified against an actual GitHub Actions run in this
phase (no reachable GitHub required or used), consistent with ADR-018's own
constraint.

## 6. Task 4 — `FakeUnitOfWork`'s persistence role, retired in documentation only

`FakeUnitOfWork.cs` (`backend/tests/Application.Tests/TestDoubles/`) gained
a comment block stating plainly, at the top of the file, what it proves
(handler orchestration: validation, authorization, the right aggregate
mutated, `SaveChangesAsync` called the expected number of times) and what it
cannot (persistence — it has no concept of EF Core change tracking).
`CLAUDE.md` gained the exact rule specified: *"`Application.Tests` proves
orchestration, authorization, and validation only. Any claim that a write
persisted must be proven in `Web.Tests` by re-reading from a fresh
`DbContext` scope."*

**Existing tests that currently claim, by name, to prove persistence** —
found via `grep -rn "public async Task.*_and_saves"` across
`backend/tests/Application.Tests`, none changed, exactly as instructed:

```
Identity/AdminResetPasswordCommandHandlerTests.cs   — Handle_resets_the_password_and_saves
Identity/ApproveTutorCommandHandlerTests.cs         — Handle_approves_existing_tutor_and_saves
Identity/ConfirmRelationshipCommandHandlerTests.cs  — Handle_confirms_existing_relationship_and_saves
Identity/RegisterParentGuardianCommandHandlerTests.cs — Handle_adds_parent_guardian_to_repository_and_saves
Identity/RegisterStudentCommandHandlerTests.cs      — Handle_adds_student_to_repository_and_saves
Identity/RegisterTutorCommandHandlerTests.cs        — Handle_adds_tutor_to_repository_and_saves
Identity/SetTutorHourlyRateCommandHandlerTests.cs   — Handle_sets_the_rate_and_saves
Identity/SetTutorLanguageCommandHandlerTests.cs     — Handle_sets_the_language_and_saves
Identity/SetTutorLocationCommandHandlerTests.cs     — Handle_sets_the_location_and_saves
Identity/SetTutorOfferedDurationsCommandHandlerTests.cs — Handle_sets_the_offered_durations_and_saves
Identity/SetTutorSubjectCommandHandlerTests.cs      — Handle_sets_the_subject_and_saves
Identity/SuspendTutorCommandHandlerTests.cs         — Handle_suspends_existing_tutor_and_saves
Scheduling/BookSessionCommandHandlerTests.cs        — Handle_books_session_against_slot_and_saves
Scheduling/CancelSessionCommandHandlerTests.cs      — Handle_cancels_existing_session_and_saves
Scheduling/CompleteSessionCommandHandlerTests.cs    — Handle_completes_existing_session_and_saves
Scheduling/DeclareAvailabilityCommandHandlerTests.cs — Handle_adds_slot_to_repository_and_saves
Scheduling/MarkSessionNoShowCommandHandlerTests.cs  — Handle_marks_existing_session_no_show_and_saves
Scheduling/RescheduleSessionCommandHandlerTests.cs  — Handle_reschedules_existing_session_and_saves
```

18 tests across 12 files. Every one of these asserts
`unitOfWork.SaveChangesCallCount == 1` and, by its own name, reads as if that
proves the write reached the database — which, per Section 6 of Phase 1B's
report and the `FakeUnitOfWork` comment added this phase, it does not and
cannot. None were changed. This is exactly the class of defect this whole
phase-pair (1B/1C) fixed for `Tutor` specifically; these 18 names are the
list a future phase would use to either rename them to reflect what they
actually verify (e.g. `Handle_approves_existing_tutor_and_calls_SaveChanges`)
or to decide `Application.Tests` shouldn't make persistence-shaped claims at
all — an owner decision, not resolved here.

## 7. Merge recommendation

**Ready to merge to `main`.** Every task's absolute rules were honored: no
`backend/src/`/`frontend/src/` change, no vitest/vite/frontend package
upgrade, no test weakened/skipped/deleted, and the backend suite — now 491
tests — was green on every run performed this phase (13/13 new guard tests
in isolation; 491/491 embedded in both full `verify.ps1` runs, four times
total). The one FAIL this phase produced (`verify.ps1` Run 1's frontend
timeout) was investigated rather than dismissed or silently re-run away, and
is documented in both this report and `README.md` so it doesn't get
mistaken for a regression in a future session. The `FakeUnitOfWork`
retirement and the 18-test list are explicitly flagged, not resolved, per
the task's own "owner decision" framing — nothing here was quietly decided
on the owner's behalf.
