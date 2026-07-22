# Phase 4.5 Report — Whole-Toman Invariant

Branch: `develop` | Commits this phase: `0a1abc8` (Task 1), `66e06aa`
(Task 2), `b22be95` (Task 3). Task 4 was verification only — no commit
(nothing to change; see Section 5).

**Status: complete, 9/9 green.** `HourlyRate.Of` now rejects any Rial
amount not evenly divisible by 10, closing the structural hole Phase 4
left: a rate written as `45` Rial was legal Domain state with no
whole-Toman price a user could have entered, and the frontend discovered
this only as an exception three layers away, at display time. The
invariant now lives where `ADR-007` requires it — the owning aggregate's
Domain layer — and the display seam was made permanently crash-proof for
data written before this phase, rather than merely not-currently-broken.

## 1. Clean Architecture validation

`backend/src/` changes are confined to `Domain/Identity/ValueObjects/HourlyRate.cs`
(the divisibility check and the corrected `MaxAmount`) and a one-line
comment update in `Infrastructure/Persistence/Configurations/TutorConfiguration.cs`
— no migration, since `numeric(12,0)` itself is unchanged; only the
Domain-level ceiling moved. No Application-layer duplicate check was
added (`ADR-007`'s single-place-of-enforcement rule, restated in ADR-019
Addendum 1). `frontend/src/` changes are confined to `shared/money/rial.ts`
(the strict/safe split) and the one page (`TutorOfferingPage`) that had
called the strict primitive directly. `dotnet build TutorFlow.sln`:
**0 Warnings, 0 Errors**. `npm run build`/`tsc -b`/`eslint .`: all clean.

## 2. Repository convention validation

Three commits on `develop`, task order, Conventional Commits (Task 4 had
no code change, so no commit — reported directly in Section 5). ADR-019
was amended via a dated Addendum, appending rather than rewriting its
original content, per Task 1's own instruction and this project's
established addendum-only convention (`ADR-003`'s Second/Third Addenda
precedent). No `bin/`/`obj/`/`node_modules`/`dist` staged. No database row
was deleted or modified this phase — Task 4 was read-only verification
against `tutorflow_dev`, and no other task touched persisted data. No
secret committed: the same `dotnet user-secrets`-derived credentials used
in Phase 4 were reused, read-only, to run `dotnet ef migrations list`/`psql`
and set `TUTORFLOW_TEST_CONNECTION` for this session. No test weakened,
skipped, or deleted — existing fixtures using non-divisible amounts (`45m`
in eight Application/Web tests, `MaxAmount + 1m` in three "exceeds
maximum" tests) were corrected to divisible-by-10 values in Task 1's own
commit, a necessary consequence of the Domain change, not a scope
expansion.

## 3. Task 1 & Task 2 — the new maximum, and why the display layer degrades rather than throws

**New maximum: `999,999,999,990` Rial** (was `999,999,999,999`). The old
value was itself illegal under the corrected divisibility rule — one
Rial short of being representable in Toman — so it had to move. `999,999,999,990`
is the largest multiple of 10 within `numeric(12,0)`'s ceiling
(`999,999,999,999`, twelve nines): still comfortably within the column's
precision (no migration needed, since the *column* type is unchanged —
only the Domain-level business ceiling moved, a pure constant edit), and
still the same roughly-four-orders-of-magnitude margin over any plausible
hourly rate that motivated the original Phase 4 figure. `HourlyRate.Of`'s
check order is `NegativeOrZero` → `amount % 10 != 0` → `amount > MaxAmount`;
the divisibility check subsumes Phase 4's old "must be a whole number"
check (anything divisible by 10 is necessarily an integer), so there are
now two checks where there were previously two — not three — despite
adding a stricter rule.

**Task 2's decision: `rialToToman` stays strict; `formatToman`/`toTomanInputValue`
degrade (round) instead.** Two options existed: make the one conversion
function permissive everywhere, or split responsibilities by caller. This
phase chose the split, for one reason — with Domain now the sole and
final authority on divisibility (Task 1), **no conforming write can ever
produce a non-divisible Rial amount again**. A caller of the low-level
primitive receiving one is therefore looking at a real bug (a
miscalculation, a bypassed invariant, a test double gone stale), and
silently rounding it there would hide that bug rather than surface it —
exactly what this codebase's "fail loudly" instinct (`UtcDateTime.EnsureUtc`,
the Phase 4 migration's own `RAISE EXCEPTION` guard) already does
elsewhere for the same class of problem. But a *page render path* is a
different contract: its job is to show a Tutor's profile, correctly, for
every row this system has ever persisted — including the handful that may
predate this phase's invariant. Crashing an Admin's or Tutor's own profile
page over a display rounding nicety would be strictly worse for every
real user than showing a value off by at most half a Toman (5 Rial),
especially since the *stored* amount is never touched by this — only the
rendered/edited Toman figure rounds, and it becomes exact again the
moment that rate is next saved through the Toman-only UI. `rialToToman`
therefore remains the strict, throwing primitive used only for round-trip
correctness (tests, `tomanToRial`'s own inverse); `formatToman` and the
new `toTomanInputValue` (which replaced `TutorOfferingPage`'s direct call
to the strict primitive for the edit form's default value — the one real
page-render path that could have crashed) both round via a shared
internal helper and are proven, by test, to never throw on a legacy
non-divisible value while still throwing on a genuinely invalid one
(negative, out-of-range).

`README.md`'s Known-pitfalls entry claiming `MaxAmount` is itself
indivisible by 10 is removed (Task 2) — it described the old, now-corrected
value and no longer applies; the adjacent entry about the divisibility gap
is rewritten to describe this phase's fix rather than the Phase 4 finding
in isolation.

## 4. Task 3 — end to end, four layers

- **Domain** (`HourlyRateTests.cs`, 9 tests, +2 this phase): explicit
  coverage for a whole-but-non-divisible amount (`45m`, message asserted
  to contain "Toman") and a divisible one (`50m`) succeeding, alongside
  the already-present maximum/maximum+10 tests (referencing `HourlyRate.MaxAmount`
  symbolically, so they track the corrected value automatically).
- **Web.Tests** (`IdentityEndpointsTests.cs`, +1): `PATCH /tutors/{id}/hourly-rate`
  with `Amount = 45` returns `400`, code `SetTutorHourlyRateCommand.Amount.Invalid`,
  message containing "Toman" — the real HTTP boundary, not a simulated one.
- **Postgres**: no test code changed. `HourlyRate_whole_Rial_amount_round_trips_exactly_at_the_maximum`
  (Phase 4's own inverted test) references `HourlyRate.MaxAmount` symbolically
  rather than a hardcoded literal, so it automatically began proving the
  *corrected* maximum (`999,999,999,990`) round-trips exactly under
  `numeric(12,0)` the moment Task 1 changed the constant — confirmed by
  running it directly against the real `tutorflow_test` database (passed).
  Its comment now records this explicitly, satisfying "update the existing
  test rather than adding a duplicate" without an assertion inversion (the
  shape of the proof didn't change, only which value it now proves).
- **Frontend** (`TutorDetailPage.test.tsx`, +1): mocks a Tutor with
  `hourlyRate: 45` (simulating a row written before this phase) and
  asserts the page renders successfully, showing the rounded `"5 Toman"`
  — proving Task 2's crash-proofing decision at the actual component level,
  not just the module level.

Full suite after this task: backend Domain 62 (+2) + Application 220
(unchanged) + Infrastructure 68 (unchanged, incl. 7 Postgres) + Web 181
(+1) = 531 tests across both determinism-guard runs, plus 7/7 Postgres.
Frontend: 229 tests (+1).

## 5. Task 4 — `tutorflow_dev` is now current

Per the owner's note, this was verification only. `dotnet ef migrations list`
against `tutorflow_dev` (with `ASPNETCORE_ENVIRONMENT=Development`, reading
the same `dotnet user-secrets` connection string as always) returned all
**8 migrations with no `(Pending)` marker** — including
`RemoveMigrationArtifactColumnDefaults` (Phase 2.5) and
`HourlyRateWholeRialPrecision` (Phase 4), both of which Phase 4's report
recorded as blocked by a table-ownership issue. `SELECT tableowner FROM pg_tables`
confirms every table is now owned by `tutorflow` (previously `postgres`),
consistent with the owner's note that ownership was granted. No migration
needed to be applied and none failed — there was nothing left pending to
run.

```
psql -U tutorflow -h localhost -d tutorflow_dev -c '\d+ "Tutors"'
 Column      | Type          | ...
 HourlyRate  | numeric(12,0) | ...
```

This matches `tutorflow_test` exactly. Phase 4's one open follow-up item
is now closed.

## 6. `scripts/verify.ps1`, real output, unmodified for this run

Run against the same real `tutorflow_test` PostgreSQL 17 database used in
Phase 4:

```
=================== VERIFY SUMMARY ===================
dotnet restore                                PASS     1.1s
dotnet build (0 warnings)                     PASS     6.0s
Backend: dotnet test TutorFlow.sln (run 1/2)  PASS    26.7s
Backend: dotnet test TutorFlow.sln (run 2/2)  PASS    22.7s
Backend: Postgres integration tests           PASS     4.9s
npm ci                                        PASS    26.0s
Frontend: npm run lint                        PASS    13.9s
Frontend: npm run build                       PASS    18.3s
Frontend: npm test -- --run                   PASS    50.6s
-------------------------------------------------------
RESULT: PASS (9/9 steps green)
```

## 7. Merge recommendation

**Safe to merge to `main`, with no open follow-ups.** Every absolute rule
held: no payment/gateway/invoice code, no multi-currency abstraction, no
database row deleted or modified without reporting first (Task 4 was
read-only; nothing needed changing), no test weakened, skipped, or
deleted. `tutorflow_dev` is fully current for the first time since Phase
2.5 — the one caveat Phase 4's report carried forward is now resolved,
and this report carries none of its own.
