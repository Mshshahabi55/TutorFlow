# Phase 3 Report — Tehran Time UX

Branch: `develop` | Commits this phase: `ef7cfeb` (Task 1), `a1f1f18`
(Task 2), `21c1e0b` (Task 3), `b3a834c` (Task 4), `7fc1668` (Task 5).

**Status: complete.** Storage stays UTC end to end — no Domain entity,
database column, migration, or wire-format shape changed. Every screen that
shows or accepts a time now does so in Asia/Tehran (UTC+03:30, no DST)
through one conversion module. No package was added (Section 3). The API
boundary now rejects any incoming timestamp without an explicit UTC
designator or offset, and a second, independently-dangerous bug in
System.Text.Json's default `DateTime` handling was found and fixed along
the way (Section 6). `scripts/verify.ps1`: **8 of 9 steps PASS**; the ninth
(Postgres integration tests) fails only because no PostgreSQL instance and
`TUTORFLOW_TEST_CONNECTION` exist in this environment — the same
pre-existing, environment-only gap Phase 2.5 reported, unrelated to this
phase's changes (Section 7).

## 1. Clean Architecture validation

`frontend/src/` changes are confined to: one new module
(`frontend/src/shared/time/tehranTime.ts` + its test file), one new
validation schema (`frontend/src/shared/validation/tehranDateTime.ts`), the
two form schemas that referenced the old raw-ISO validator
(`declareAvailabilitySchema.ts`, `rescheduleSessionSchema.ts`), and eight
page components swapped from raw-string inputs/displays to the new module's
functions. No component does timezone arithmetic inline — every one imports
`toTehranDisplay`/`fromTehranInput` from `shared/time/tehranTime.ts`, which
was written and fully tested (Task 1) before any page was touched (Task 2),
per the phase's own ordering requirement.

`backend/src/` changes are confined to the Web layer only: one new file
(`backend/src/Web/Json/RequireUtcDateTimeJsonConverter.cs`) and a
9-line addition to `Program.cs` registering it via `ConfigureHttpJsonOptions`.
No Domain entity, Application command/query, Infrastructure repository, or
EF Core configuration changed — `DeclareAvailabilityCommand`,
`RescheduleSessionCommand`, and every DTO keep the exact `DateTime`
properties they had before. The Domain ← Application ← Infrastructure/Web
dependency rule is untouched: the new converter is `internal`, referenced
only from `Program.cs` inside the same assembly.

`dotnet build TutorFlow.sln`: **0 Warnings, 0 Errors**. `npm run build`:
clean, `tsc -b` reports no type errors. `npm run lint` (`eslint .`): clean.

## 2. Repository convention validation

Five commits on `develop`, one per task, Conventional Commits, in task
order. No `bin/`/`obj/`/`node_modules`/`dist` staged. No secret,
connection string, or credential touched — this phase never needed a
database connection (SQLite in-memory covers every backend test that
exercises the new converter; see Section 6). No `TODO`/`FIXME`/
`NotImplementedException` introduced. No package added (Section 3).

## 3. Task 1 — the time module: no package needed

**Packages added: none.** Evaluated `date-fns`, `date-fns-tz`, and
`@mui/x-date-pickers` (all pre-authorized) against the actual problem and
concluded none earn their weight:

- **Display** (`toTehranDisplay`) doesn't need `Intl`'s `timeZone:
  "Asia/Tehran"` resolution at all, let alone a library: the UTC instant is
  shifted by a **fixed** `+03:30` in plain millisecond arithmetic first,
  then formatted with `Intl.DateTimeFormat({ timeZone: "UTC", ... })` purely
  for locale-aware month names — Intl never resolves an actual IANA zone,
  so correctness never depends on the runtime's tz-database being current
  (a real risk: Iran abolished DST in 2022, and a stale ICU build could
  still apply an old rule if the code asked Intl to resolve `Asia/Tehran`
  directly).
- **Input → UTC** (`fromTehranInput`) is the "harder direction" the phase
  asked to assess honestly. Because Tehran's offset never changes, it's
  `Date.UTC(...) - 210 minutes` — no DST table, no ambiguous/skipped local
  times to resolve, which is the entire reason `date-fns-tz` or similar
  libraries exist in the first place. There is no genuine problem here for
  a library to solve.
- **The picker itself**: a plain MUI `TextField` with `type="datetime-local"`
  (already in the dependency tree) gives an accessible, real browser-native
  date+time picker bound to `FormTextField`'s existing react-hook-form
  wiring, with zero new code and zero new dependency. `@mui/x-date-pickers`
  would have added a full component library to render exactly this.

**Bundle-size impact: effectively none**, confirmed via `npm run build`
before/after — no `vendor-*` chunk changed size (no new dependency was
added to `package.json`), and the only new output is the module's own two
small chunks: `tehranTime-*.js` (1.86 kB, 0.71 kB gzip) and
`tehranDateTime-*.js` (0.20 kB, 0.17 kB gzip).

## 4. Task 1 — test results (date-boundary explicit)

**22 of 22 tests pass** in `frontend/src/shared/time/tehranTime.test.ts`,
written and run before any page was wired to the module:

```
✓ fromTehranInput / toTehranInputValue — the +03:30 half-hour offset (3 tests)
✓ date-boundary crossing (3 tests)
✓ round-tripping (4 tests, incl. a US DST-transition instant)
✓ no inherited DST (3 tests)
✓ toTehranDisplay (2 tests)
✓ isValidTehranLocalInput (6 tests)
✓ fromTehranInput error handling (1 test)

Test Files  1 passed (1)
     Tests  22 passed (22)
```

**Date-boundary results, explicitly** (the phase's own "single most common
source of timezone bugs" check, proven both directions):

| Direction | Input | Expected | Actual |
|---|---|---|---|
| Tehran → UTC | `2026-08-02T01:00` (Tehran) | `2026-08-01T21:30:00.000Z` | `2026-08-01T21:30:00.000Z` ✓ |
| UTC → Tehran | `2026-08-01T21:30:00Z` | `2026-08-02T01:00` (Tehran) | `2026-08-02T01:00` ✓ |

Both assert the *date* changes (Aug 1 ↔ Aug 2), not just the clock time —
`fromTehranInput`/`toTehranInputValue`'s `Date.UTC`/`getUTC*` arithmetic
handles the rollover automatically since it operates on true epoch
milliseconds, never on separately-tracked date/time fields.

The "no inherited DST" tests cross real US DST transition instants
(2026-03-08 spring-forward, 2026-11-01 fall-back) and Iran's own
now-abolished former DST dates (late March/September), asserting the
Tehran offset stays exactly `+03:30` on both sides of each — proving the
module's fixed-offset design can't pick up a stray DST rule from the host,
from Intl, or from Tehran's own pre-2022 history.

## 5. Task 2 & Task 3 — inputs and displays converted

**Task 2** (`a1f1f18`): Declare Availability's `startTimeUtc` free-text
field and Reschedule Session's `newScheduledTimeUtc` field are now
`type="datetime-local"` inputs labelled "(Tehran)", bound to
`declareAvailabilitySchema`/`rescheduleSessionSchema`'s renamed
`startTimeLocal`/`newScheduledTimeLocal` fields (`tehranLocalDateTimeSchema`
validates the field shape; `fromTehranInput` converts to the wire value at
submit time, which is then exactly the same UTC-`Z` string the backend
already expected — no API contract change). The old
"rejects a malformed UTC timestamp" test is inverted to "rejects a missing
start time" in the same commit, since a native picker can no longer produce
free-typed garbage — following the Phase 1B/2.5 precedent of documenting an
intentionally-flipped assertion explicitly rather than silently deleting it.

**Task 3** (`21c1e0b`, plus the two pages' own displays folded into Task
2's commit since they share the same files): every remaining `(UTC)` label
or raw `*TimeUtc` render now goes through `toTehranDisplay` and reads
"(Tehran)":

- `frontend/src/features/scheduling/pages/DeclareAvailabilityPage.tsx` (success panel)
- `frontend/src/features/scheduling/pages/SessionDetailPage.tsx` (Scheduled/End, and the reschedule input)
- `frontend/src/features/scheduling/pages/AvailabilitySlotDetailPage.tsx`
- `frontend/src/features/scheduling/pages/BookSessionPage.tsx` (confirmation panel)
- `frontend/src/features/scheduling/pages/StudentSessionListPage.tsx`
- `frontend/src/features/scheduling/pages/TutorSessionListPage.tsx`
- `frontend/src/features/oversight/pages/GlobalSessionListPage.tsx`

`grep -rn "TimeUtc}" frontend/src --include=*.tsx | grep -v .test.tsx |
grep -v "toTehranDisplay\|fromTehranInput\|TimeUtc:"` returns nothing — no
page renders a raw UTC string to a user. (Left deliberately untouched, out
of this phase's named scope: `frontend/src/features/discovery/pages/TutorSearchPage.tsx`'s
`availableFrom` filter, which still uses the original
`isoDateTimeUtcSchema`/raw-ISO text field — Task 2 named only Declare
Availability and Reschedule Session by name, and it's a filter input, not a
display of a stored time.)

All 195 frontend tests pass (`npm test -- --run`), including the updated
`DeclareAvailabilityPage.test.tsx` and `SessionDetailPage.test.tsx`.

## 6. Task 4 — closing the API boundary (and a second bug found along the way)

Phase 2.5's `UtcDateTimeValueConverter` relabels `Kind=Unspecified` as UTC
at the EF Core boundary — correct for values this codebase produces
internally, but it meant a client-sent timestamp with no `Z`/offset would
be silently accepted and treated as UTC, a silent 3.5-hour error for a
Tehran caller. `backend/src/Web/Json/RequireUtcDateTimeJsonConverter.cs`
closes this: registered globally via `ConfigureHttpJsonOptions` in
`Program.cs`, it rejects (with a `JsonException` → 400) any JSON-bound
`DateTime`/`DateTime?` lacking a trailing `Z` or a numeric `[+-]hh:mm`
offset.

**A second, independent bug was found while investigating this**, proven
with a throwaway probe before writing the fix (kept in this report, not the
codebase, since it's not itself a regression test):
`JsonSerializer.Deserialize<DateTime>("\"2026-08-01T14:00:00+03:30\"")`
returns `Kind=Local`, converted against **the host process's own system
timezone**, not the offset actually in the string. Two servers in different
timezones would compute two different instants from an identical request
body — and combined with `UtcDateTimeValueConverter`'s "Kind=Local always
throws" rule, the request would either 500 or silently misconvert depending
where the API happens to run. `RequireUtcDateTimeJsonConverter` parses
every accepted value via `DateTimeOffset` instead, which is anchored to the
offset in the string, never the host's — deterministic regardless of
server location.

**Acceptance and rejection, proven at both the unit and end-to-end level:**

- `backend/tests/Web.Tests/Json/RequireUtcDateTimeJsonConverterTests.cs`
  (new, 7 tests): accepts trailing `Z`, accepts fractional seconds with
  `Z`, accepts `+03:30`/`+00:00`/`-05:00` and converts each to the
  *correct* UTC instant (not the test-runner's own timezone), rejects a
  bare `2026-08-01T14:00:00`, a date-only string, `null`, and garbage.
- `backend/tests/Web.Tests/SchedulingEndpointsTests.cs` (3 new tests, sent
  as hand-written raw JSON since `System.Text.Json` itself would never
  produce a `Z`-less string): `DeclareAvailability` returns `400` for a
  `Z`-less `StartTimeUtc`; `DeclareAvailability` **succeeds** for
  `2026-08-01T17:30:00+03:30` and the persisted/returned `startTimeUtc`
  equals `2026-08-01T14:00:00Z`; `RescheduleSession` returns `400` for a
  `Z`-less `NewScheduledTimeUtc`.

All 62 `Web.Tests` in the Scheduling/Json area pass; the full non-Postgres
backend suite (Domain 53 + Application 218 + Infrastructure 61 + Web 175 =
**507 tests**) passes, twice in a row (verify.ps1's determinism guard).

## 7. Task 5, verify.ps1 results, and merge recommendation

**Documentation** (`7fc1668`): `docs/MANUAL-SMOKE-TEST.md`'s Declare
Availability step now describes using the Tehran-local picker, keeping the
worked `17:30 − 03:30 = 14:00` example as an explanatory note rather than a
by-hand instruction; the closing paragraph no longer disclaims the timezone
gap as a known future-phase item. `CLAUDE.md` gained the phase's required
sentence verbatim: *"All times are stored and transmitted in UTC. All times
displayed to or entered by a user are Asia/Tehran (UTC+03:30, no DST).
Conversion happens only in `frontend/src/shared/time/` — never inline."*
`README.md`'s Known pitfalls gained the `Kind=Local` finding from Section 6.

**`scripts/verify.ps1`, real output, unmodified for this run:**

```
=================== VERIFY SUMMARY ===================
dotnet restore                                PASS     2.3s
dotnet build (0 warnings)                     PASS     4.6s
Backend: dotnet test TutorFlow.sln (run 1/2)  PASS    26.5s
Backend: dotnet test TutorFlow.sln (run 2/2)  PASS    23.4s
Backend: Postgres integration tests           FAIL     2.7s
npm ci                                        PASS    30.3s
Frontend: npm run lint                        PASS    17.3s
Frontend: npm run build                       PASS    19.1s
Frontend: npm test -- --run                   PASS    59.5s
-------------------------------------------------------
RESULT: FAIL (8/9 steps green)
```

The one failure is `PostgresTestFixture`'s own guard —
`InvalidOperationException: Environment variable 'TUTORFLOW_TEST_CONNECTION'
is not set` — firing exactly as designed (fail loudly, not skip) because no
PostgreSQL instance exists in this sandboxed environment. This is the same
environment-only gap Phase 2.5 reported (`docs/phases/PHASE-025-REPORT.md`
Section 6), not a regression: nothing in this phase touched persistence,
migrations, or the Postgres-only test suite, and every one of the 7 failing
tests fails identically at `PostgresTestFixture`'s constructor, before any
of this phase's code would even run.

**Merge recommendation: safe to merge to `main`** once run once against a
real PostgreSQL instance (`TUTORFLOW_TEST_CONNECTION` set, per README's
"Database setup") to confirm the 9th step, which this phase's own changes
have no plausible path to affecting — they touch only the Web JSON layer
and the frontend, never a migration, entity, or repository.
