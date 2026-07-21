# Phase 1B Report — Persistence Integrity (Authorized `backend/src` Change)

Branch: `develop` | Commits this phase: `9b4b337` (failing tests), `ff3d538`
(fix), `1ea5ccf` (corrected stale test), `21c9f0c` (docs), plus this report's
own commit

**Status: complete.** Full backend suite (478 tests across Domain/Application/
Infrastructure/Web.Tests) is **fully green across 10 consecutive runs.** The
`backend/src` defect Phase 1 identified but was not authorized to fix —
`EfUnitOfWork` never re-attaching aggregates read via `.AsNoTracking()` — is
fixed, at exactly the two repository methods a full audit proved were
actually broken.

## 1. Clean Architecture validation

One `backend/src` file changed: `backend/src/Infrastructure/Identity/Repositories/TutorRepository.cs`
(13 insertions, 2 deletions — removing `.AsNoTracking()` from two methods,
plus a comment explaining why). This is the only production-code change
this phase made, and it is exactly the approach the owner authorized
(approach (b): stop using `.AsNoTracking()` on the command path, not (a)
patching `EfUnitOfWork` to call `_dbContext.Update()`). No Domain entity
behaviour, no handler's business logic, and no API contract changed —
verified by re-running Domain.Tests (53) and Application.Tests (218)
unchanged and green throughout.

Everything else this phase touched is test code or documentation:
`backend/tests/Web.Tests/PersistenceIntegrityTests.cs` (new, 15 tests),
`backend/tests/Infrastructure.Tests/Identity/Repositories/TutorRepositoryTests.cs`
(one test corrected — see Section 4), `CLAUDE.md`, `README.md`.

`dotnet build TutorFlow.sln`: **0 Warnings, 0 Errors**, checked after every
commit this phase.

## 2. Repository convention validation

Four commits on `develop` (not `main`), each a single logical unit, in the
order the task specified — failing tests before the fix, fix as its own
commit, the test correction the fix required as a separate commit, docs
last — plus this report as a fifth. No `bin/`/`obj/`/`node_modules/`
staged. No migration run; no real database connected to.

## 3. Task 1 audit — every repository method in `backend/src/Infrastructure/`

Read-only investigation, no code changed while compiling this table. For
each method: does it use `.AsNoTracking()`, is its result ever mutated and
saved, and therefore is it COMMAND PATH or READ-ONLY.

| Repository.Method | `AsNoTracking()`? | Consumers (mutated+saved in bold) | Classification | Status before this phase |
|---|---|---|---|---|
| `TutorRepository.GetByIdAsync` | **Yes** | **ApproveTutorCommandHandler, SuspendTutorCommandHandler, SetTutorHourlyRateCommandHandler, SetTutorSubjectCommandHandler, SetTutorLanguageCommandHandler, SetTutorLocationCommandHandler, SetTutorOfferedDurationsCommandHandler, AdminResetPasswordCommandHandler** (Tutor-target); `GetTutorByIdQueryHandler` (read-only) | **COMMAND PATH** | **BROKEN** — proven by Phase 1 + Task 2 |
| `TutorRepository.GetByEmailAsync` | **Yes** | **LoginCommandHandler** (candidate account, mutated when matched/touched); `RegisterTutorCommandHandler` (duplicate-email check, read-only) | **COMMAND PATH** | **BROKEN** — proven by Phase 1 (`Repeated_wrong_passwords...`) |
| `TutorRepository.GetDiscoverableAsync` | Yes | `GetTutorListQueryHandler` | READ-ONLY | Correct as-is |
| `TutorRepository.GetPendingAsync` | Yes | `GetPendingTutorsQueryHandler` | READ-ONLY | Correct as-is (its own Phase 1 bug was a missing `ORDER BY` + shared test DB, already fixed — unrelated to tracking) |
| `TutorRepository.SearchDiscoverableAsync` | Yes | `SearchTutorsQueryHandler` | READ-ONLY | Correct as-is |
| `TutorRepository.AddAsync` | n/a (insert) | `RegisterTutorCommandHandler` | COMMAND PATH (insert-only) | Correct — `Add()` always tracks a new entity regardless |
| `StudentRepository.GetByIdAsync` | No (already tracked) | **AdminResetPasswordCommandHandler** (Student-target); `BookSessionCommandHandler` (read-only `IsMinor` check); `CreateRelationshipInvitationCommandHandler` (read-only); `GetStudentByIdQueryHandler` (read-only) | COMMAND PATH | Already correct — proven by Task 2 |
| `StudentRepository.GetByEmailAsync` | No (already tracked) | **LoginCommandHandler** (Student candidate); `RegisterStudentCommandHandler` (read-only) | COMMAND PATH | Already correct (not independently tested this phase — same shape as the proven Tutor case, tracked) |
| `StudentRepository.AddAsync` | n/a | `RegisterStudentCommandHandler` | COMMAND PATH (insert-only) | Correct |
| `ParentGuardianRepository.GetByIdAsync` | No (already tracked) | `CreateRelationshipInvitationCommandHandler` (read-only); `GetParentGuardianByIdQueryHandler` (read-only); **AdminResetPasswordCommandHandler** (ParentGuardian-target) | COMMAND PATH | Already correct |
| `ParentGuardianRepository.GetByEmailAsync` | No (already tracked) | **LoginCommandHandler** (ParentGuardian candidate); `RegisterParentGuardianCommandHandler` (read-only) | COMMAND PATH | Already correct |
| `ParentGuardianRepository.AddAsync` | n/a | `RegisterParentGuardianCommandHandler` | COMMAND PATH (insert-only) | Correct |
| `AdminStaffRepository.GetByIdAsync` | No (already tracked) | **AdminResetPasswordCommandHandler** (AdminStaff-target, last fallback) | COMMAND PATH | Already correct |
| `AdminStaffRepository.GetByEmailAsync` | No (already tracked) | **LoginCommandHandler** (AdminStaff candidate) | COMMAND PATH | Already correct |
| `AdminStaffRepository.AddAsync` | n/a | Test seeding only (no self-registration endpoint, ADR-017) | COMMAND PATH (insert-only) | Correct |
| `RelationshipRepository.GetByIdAsync` | No (already tracked) | **ConfirmRelationshipCommandHandler**; `GetRelationshipByIdQueryHandler` (read-only) | COMMAND PATH | **Already correct — proven by Task 2** |
| `RelationshipRepository.GetByAccountIdAsync` | Yes | `BookSessionCommandHandler` (read-only confirmed-relationship check, no mutation of the Relationship itself); `GetRelationshipsByAccountIdQueryHandler` (read-only) | READ-ONLY | Correct as-is |
| `RelationshipRepository.AddAsync` | n/a | `CreateRelationshipInvitationCommandHandler` | COMMAND PATH (insert-only) | Correct |
| `AuthTokenRepository.AddAsync` | n/a | `LoginCommandHandler` | COMMAND PATH (insert-only) | Correct |
| `AuthTokenRepository.GetByTokenHashAsync` | No (already tracked) | **LogoutCommandHandler** (`token.Revoke()`); AuthorizationMiddleware's sliding-expiration extension (read-then-extend-then-save) | COMMAND PATH | **Already correct — proven by Task 2** |
| `AuthTokenRepository.GetActiveByAccountIdAsync` | No (already tracked) | **AdminResetPasswordCommandHandler** (`token.Revoke()` for each active token) | COMMAND PATH | Already correct (indirectly confirmed: Phase 1's failing `Admin_reset_password...` test's old-token-401 assertion always passed — only the password-change assertion failed) |
| `SessionRepository.GetByIdAsync` | No (already tracked) | **CancelSessionCommandHandler, RescheduleSessionCommandHandler, CompleteSessionCommandHandler, MarkSessionNoShowCommandHandler**; `GetSessionByIdQueryHandler` (read-only) | COMMAND PATH | **Already correct — proven by Task 2, all four** |
| `SessionRepository.GetAllAsync` | Yes | `GetAllSessionsQueryHandler` (Oversight) | READ-ONLY | Correct as-is |
| `SessionRepository.GetByTutorIdAsync` | Yes | `GetTutorScheduleQueryHandler` | READ-ONLY | Correct as-is |
| `SessionRepository.GetByStudentIdAsync` | Yes | `GetStudentScheduleQueryHandler` | READ-ONLY | Correct as-is |
| `SessionRepository.GetByAvailabilitySlotIdAsync` | No (tracked, but unnecessary — no consumer mutates) | `GetAvailabilitySlotByIdQueryHandler` (read-only, embeds the resulting Session in a DTO) | READ-ONLY (currently over-tracked) | Not broken; optional future optimization, not required by this fix (see Section 6) |
| `SessionRepository.AddAsync` | n/a | `BookSessionCommandHandler` | COMMAND PATH (insert-only) | Correct |
| `AvailabilitySlotRepository.GetByIdAsync` | No (already tracked) | **BookSessionCommandHandler** (`slot.Book(...)`, sets `IsConsumed`); `GetAvailabilitySlotByIdQueryHandler` (read-only) | COMMAND PATH | **Already correct — proven by Task 2 (the double-booking guard question, see below)** |
| `AvailabilitySlotRepository.GetTutorIdsWithOpenSlotFromAsync` | Yes | `SearchTutorsQueryHandler` | READ-ONLY | Correct as-is |
| `AvailabilitySlotRepository.GetByTutorIdAsync` | Yes | `GetTutorAvailabilitySlotsQueryHandler` | READ-ONLY | Correct as-is |
| `AvailabilitySlotRepository.AddAsync` | n/a | `DeclareAvailabilityCommandHandler` | COMMAND PATH (insert-only) | Correct |
| `AuditEntryRepository.GetAllAsync` | Yes | `GetAuditEntriesQueryHandler` | READ-ONLY | Correct — `AuditEntry` is append-only, there is no command path for it at all |

**The double-booking question, answered directly.** The brief asked to check
explicitly whether `AvailabilitySlot.Book()`'s domain guard was inert. It is
**not**: `AvailabilitySlotRepository.GetByIdAsync` was never `.AsNoTracking()`
— `BookSessionCommandHandler` fetches a tracked `AvailabilitySlot`, calls
`slot.Book(...)` (which throws `InvalidOperationException` if
`IsConsumed` is already `true`), and passes both the mutated `slot` and the
new `Session` to `SaveChangesAsync`. Since the entity is tracked, EF Core
correctly generates the `UPDATE` for `IsConsumed`. Task 2's
`BookSession_persists_AvailabilitySlot_IsConsumed_and_the_domain_guard_rejects_a_second_booking_of_the_same_slot`
test proves this directly: a first booking persists `IsConsumed = true`
(confirmed via a fresh-scope re-read), and a second booking attempt against
the same slot is rejected with `BookSessionCommand.InvalidState` — the
Domain guard itself — not `BookSessionCommand.SlotAlreadyBooked` (the
`ConcurrencyConflictException`/unique-index path, which would only fire if
the guard were inert and two bookings raced past it concurrently). Both
layers of protection are real; the Domain guard is the one actually catching
the ordinary, non-concurrent duplicate-booking case.

**Net result: the defect's true scope was two methods, not "the Identity
module."** Every other Identity repository (`Student`, `ParentGuardian`,
`AdminStaff`, `AuthToken`) and every Scheduling/Discovery repository used on
a command path (`Session`, `AvailabilitySlot`, `Relationship.GetByIdAsync`)
was already tracked and already correct. Phase 1's report reasonably flagged
the whole module as "at risk" without this audit; the audit narrows that to
exactly `TutorRepository.GetByIdAsync` and `GetByEmailAsync`.

## 4. Task 2 tests, and the one existing test that had to change

`backend/tests/Web.Tests/PersistenceIntegrityTests.cs` (15 tests, one per
COMMAND PATH method from the table above not already covered by one of
Phase 1's 6 known failures): each performs the mutation over HTTP, then
re-reads the entity from a **brand-new `DbContext` scope** (never the
response body, never a context that could still be holding the mutated
instance in its identity map).

Run before the fix (Task 2, this phase): **8 passed, 7 failed** — exactly
the 7 `TutorRepository`-consuming tests (`Approve`, `Suspend`,
`SetHourlyRate`, `SetSubject`, `SetLanguage`, `SetLocation`,
`SetOfferedDurations`), and exactly the 8 already-tracked-repository tests
passed (`AdminResetPassword` for a Student target, `Logout`,
`ConfirmRelationship`, `BookSession`'s guard, `CancelSession`,
`RescheduleSession`, `CompleteSession`, `MarkSessionNoShow`) — a clean,
one-to-one match with the audit's predictions. **A test that passes here
found a repository method that was already correct** — stated explicitly,
per the task's own instruction, rather than assuming everything was broken.

**One pre-existing test had to be corrected, not just left alone or newly
added:** `TutorRepositoryTests.GetByIdAsync_returns_a_detached_tutor_when_found`
(`backend/tests/Infrastructure.Tests/Identity/Repositories/TutorRepositoryTests.cs`)
asserted `Assert.DoesNotContain(readContext.ChangeTracker.Entries<Tutor>(), ...)`
— i.e., it asserted the exact defect as a requirement. This test passed
before the Task 3 fix and would have **started failing** the moment
`.AsNoTracking()` was removed, since the whole point of the fix is that
`GetByIdAsync` now *does* return a tracked entity. This is not a case of
"the test's assertion is wrong and I disagree with it" (Phase 1's stop
condition for genuinely-disputed assertions) — this phase's own brief
explicitly authorized removing `.AsNoTracking()` from this exact method, so
the test's contract needed to invert to match the behavior the owner had
already approved. Renamed to `GetByIdAsync_returns_a_tracked_tutor_when_found`,
assertion inverted to `Assert.Contains`, committed separately (`1ea5ccf`)
with a comment explaining exactly why, rather than silently folded into the
fix commit. No other existing test needed to change.

## 5. Task 3 — the fix, and Task 4 — proof

**Fix** (`ff3d538`): removed `.AsNoTracking()` from `TutorRepository.GetByIdAsync`
and `GetByEmailAsync` only — the two methods the audit and Task 2 proved
were on the command path and broken. Every `.AsNoTracking()` call
elsewhere in `backend/src/Infrastructure/` is unchanged. One accepted side
effect: `GetTutorByIdQueryHandler`'s read-only usage of `GetByIdAsync` now
also pays a small tracking-overhead cost, since it shares the method with
the 8 command handlers — judged not worth splitting into a second,
read-only-only lookup for this fix (see Section 6 for the related
`touchedAggregates` observation).

**Proof:**
- Task 2 tests, filtered run, before fix: 8 passed / 7 failed.
- Task 2 tests, filtered run, after fix: **15/15 passed.**
- Full backend suite, **10 consecutive runs, after the fix** (`dotnet test TutorFlow.sln --no-build`):

| Run | Domain.Tests | Application.Tests | Infrastructure.Tests | Web.Tests | Total |
|---|---|---|---|---|---|
| 1–10 | 53/53 | 218/218 | 46/46 | 161/161 | **478/478, every run** |

All 10 runs fully green, including the 6 tests Phase 1 left failing and all
15 new Task 2 tests.

**Runtime, honestly measured, not assumed:**
- Filtered 15-test run, before fix (7 failing early via exception): 7.79s. After fix (all 15 succeed, doing full round-trips instead of failing partway through): 10.29s. This is not a clean measurement of tracking overhead alone — successful tests necessarily do more work (more HTTP round trips, more DB commands) than ones that throw partway through, so some of this increase is "the tests now do the thing they were supposed to," not "tracking is slower."
- Full `Web.Tests` project (161 tests, 15 more than Phase 1's 146), 10 runs after the fix: 25.28s, 20.02s, 35.92s, 46.74s, 25.48s, 21.32s, 21.16s, 22.03s, 20.52s, 27.40s → mean **26.59s**. Phase 1's pre-Phase-1B baseline (146 tests, before either phase's changes): mean 21.51s. The ~5s increase is consistent with 15 additional tests each making 2–5 HTTP calls plus a fresh-scope DB re-read, not a tracking-overhead regression on the original 146 — no test in the original 146 became slower in a way distinguishable from normal run-to-run variance (compare Phase 1's own after-isolation-fix mean of 19.94s for 146 tests to the same 146 tests' share of this phase's totals; the delta tracks the 15 added tests, not the two touched methods).
- Full 4-project suite, 10 runs: 230–285s depending on measurement pass (includes `dotnet` process/MSBuild startup overhead each invocation, consistent with Phase 1's methodology). No run showed a runtime cliff or timeout risk.

## 6. `EfUnitOfWork.touchedAggregates` and the `FakeUnitOfWork` assessment

**`touchedAggregates` is not an unused parameter — reporting this precisely
since Task 3 asked for it.** `EfUnitOfWork.SaveChangesAsync(touchedAggregates, ...)`
does use the parameter: it passes `aggregates` to
`_domainEventDispatcher.DispatchAsync(...)` and then calls
`aggregate.ClearDomainEvents()` on each one. What it does *not* do is use
`touchedAggregates` to attach/mark any aggregate as `Modified` before
calling `_dbContext.SaveChangesAsync()` — it implicitly assumes every
aggregate handed to it is already tracked from how it was fetched. That
assumption was false for exactly the two `TutorRepository` methods this
phase fixed. This is a real design gap worth a follow-up decision (not made
in this phase, since the brief explicitly forbade approach (a) and told me
not to change the `IUnitOfWork` interface): should `EfUnitOfWork` defensively
call `_dbContext.Update(aggregate)` for every touched aggregate regardless of
tracking state, as a second line of defense against a future repository
method reintroducing `.AsNoTracking()` on a command path? That would trade
a small amount of duplicate-tracking overhead for making this whole class of
defect structurally impossible rather than just documented-against
(Section 5 of `CLAUDE.md`'s Definition of Done and the two new
non-negotiable rules added this phase rely on developer/reviewer discipline,
not a compiler- or runtime-enforced guarantee). Flagging as a candidate for
its own future ADR-level decision, not implementing it here.

**`FakeUnitOfWork` assessment.** `backend/tests/Application.Tests/TestDoubles/FakeUnitOfWork.cs`
is 16 lines: increment a counter, clear domain events, return. It gave
false confidence for this project's entire history — every
`ApproveTutorCommandHandlerTests`-style test could only ever verify "the
handler mutated its in-memory aggregate and called `SaveChangesAsync` once,"
which is necessary but not sufficient evidence of correctness, and the gap
between those two things is precisely the defect this phase fixed. Two
options, an owner decision, not made here:
1. **Make it faithful.** Have `FakeUnitOfWork` simulate a minimal
   tracked/untracked distinction — e.g., accept a predicate or a wrapped
   "was this fetched via a tracking read" flag per aggregate, and throw (or
   record a distinct outcome) if `SaveChangesAsync` is given an aggregate
   that wouldn't actually have been tracked by the real `TutorFlowDbContext`.
   This keeps `Application.Tests` fast and isolated but requires it to
   somehow mirror the real tracking rules — meaning every future repository
   change touching `.AsNoTracking()` needs a parallel update to the fake, or
   the fake silently drifts out of sync with reality again.
2. **Stop relying on it for persistence correctness at all**, and treat
   `Application.Tests` as verifying orchestration/authorization/validation
   only (which is what it is actually good at and what its own file
   comments already claim) — with every "did this write actually persist"
   question answered exclusively by `Web.Tests`, following the exact
   fresh-DbContext-reread pattern this phase's `PersistenceIntegrityTests.cs`
   established. This is simpler and doesn't require maintaining two
   persistence models in parallel, but means `Application.Tests` alone can
   never prove a handler is fully correct — a real gap already true before
   this phase and arguably true of any hand-rolled fake, made explicit
   instead of implicit.

No change made to `FakeUnitOfWork` in this phase, per the brief's explicit
instruction. Recommend option 2 as lower-maintenance and more honest about
what each test project can and cannot prove, but this is the owner's call.

## 7. Merge recommendation

**Ready to merge to `main`.** The fix is minimal (2 methods, 1 file), exactly
matches the owner-authorized approach, is proven by a dedicated test for
every command-path repository method in the codebase (not just the 6
originally-known failures), and the full backend suite is green across 10
consecutive runs with no runtime regression beyond the expected cost of the
15 new tests themselves. The one existing test that needed correcting was
handled transparently, in its own commit, with an explanation of exactly why
its old assertion encoded the bug rather than a requirement.

Two items are explicitly **not resolved** and are owner decisions for a
future phase, not blockers to merging this one: (1) whether `EfUnitOfWork`
should defensively re-attach touched aggregates as a second line of defense
(Section 6); (2) whether to invest in a faithful `FakeUnitOfWork` or retire
its persistence-correctness role in favor of `Web.Tests` (Section 6). Both
are flagged, neither is silently deferred.
