# Phase 4 Report — Currency and Pricing

Branch: `develop` | Commits this phase: `7606f02` (Task 1), `1af4950` +
`82053f8` (Task 2), `ee32115` (Task 3), `182afe3` (Task 4), `a80f6df`
(Task 5), `93b43f4` (Task 6).

**Status: complete, 9/9 green.** `HourlyRate` now carries an explicit
currency (IRR) and a scale-0 (whole-Rial) invariant enforced once, in
Domain. The migration was written, built, and applied against a real
PostgreSQL 17 instance — with one honestly-reported exception: applying it
to `tutorflow_dev` specifically is blocked by a pre-existing table-
ownership condition this phase found but did not cause (Section 3).
Frontend now enters and displays every rate in Toman through a single,
fully-tested conversion module; the wire format never changed from Rial.
Along the way, the migration's own defensive guard against silently
rounding a fractional value caught real leftover data from a since-retired
test — concrete proof the "don't invent a rounding rule" absolute rule
was worth following.

## 1. Clean Architecture validation

`backend/src/` changes are confined to: `Domain/Identity/ValueObjects/HourlyRate.cs`
(the currency constant and the two new invariants), one line in
`Infrastructure/Persistence/Configurations/TutorConfiguration.cs`
(`.HasPrecision(12, 0)`), one new migration pair, and a one-line error-code
rename in `Application/Identity/Handlers/SetTutorHourlyRateCommandHandler.cs`.
No Application-layer duplicate of the Domain invariant was added — `ADR-007`
(this project's own validation-strategy ADR) explicitly rejects that
pattern as a general policy, and the existing `catch (ArgumentException)`
in the handler already turns any Domain rejection into a proper `Result`,
so Task 5's "boundary validation" is proven as an *observed behavior*
(Web.Tests hitting the real endpoint) rather than built as a second,
redundant check. `frontend/src/` changes are confined to one new module
(`shared/money/rial.ts`), one validation schema, and the four page
components/tests that touch a rate. No Domain entity's *shape* changed
beyond precision; no new endpoint, so no `AUTHORIZATION_MATRIX.md` update
was needed. `dotnet build TutorFlow.sln`: **0 Warnings, 0 Errors**.
`npm run build`/`tsc -b`/`eslint .`: all clean.

## 2. Repository convention validation

Seven commits on `develop`, task order, Conventional Commits — Task 2
split into two per its own explicit instruction to commit the inverted
test separately, mirroring the Phase 1B/2.5/3/3.5 precedent. ADR-019 was
written and committed *before* any Task 2+ code, per Task 1's own
instruction. No `bin/`/`obj/`/`node_modules`/`dist` staged. No secret
committed: the real local Postgres credentials used to apply and verify
the migration (Section 3) were read from this machine's existing
`dotnet user-secrets` store, used only to run `psql`/`dotnet ef` commands
and set `TUTORFLOW_TEST_CONNECTION` for this session, never written to
any tracked file. No test weakened, skipped, or deleted — one inverted
assertion (Task 2, `82053f8`) is in its own commit with an explanation.

## 3. Task 2 — the currency designator, the migration, and its schema before/after

**Currency designator: a constant, not a value object.** `HourlyRate.CurrencyCode`
is `public const string CurrencyCode = "IRR"`, exposed per-instance via a
`Currency` property. Chosen over a full `Currency` value object because
v1 has exactly one currency — a value object would mean every `HourlyRate`
instance carries a field whose value never varies, and this phase's own
rule forbids building a multi-currency abstraction now. A constant still
satisfies `ADR-018`'s Forward Compatibility requirement: it is a real,
typed, grep-able fact in the model (`HourlyRate.CurrencyCode`, not a
comment), and it is the single point a v2 multi-currency change would
extend (e.g. adding a `Currency` parameter to `HourlyRate.Of`), not a
scattered assumption to hunt down. Full reasoning: `docs/adr/ADR-019-currency-and-pricing-representation.md`.

**Maximum value: 999,999,999,999 Rial** (`HourlyRate.MaxAmount`), matching
`numeric(12,0)`'s ceiling exactly (twelve nines). Chosen as roughly four
orders of magnitude above any currently-plausible hourly rate, deliberately
generous given Rial's history of rapid devaluation, so this column should
not need a second precision migration for a long time.

**"No database has rows" was checked, and found false for `tutorflow_dev`**
(2 rows; one non-null `HourlyRate` of `500000.00`, already a whole number
— confirmed via `psql \d+`/`SELECT` before writing the migration). No
backfill logic was needed in practice (the one real value casts cleanly),
but the migration itself does not trust that silently — see below.

**The migration refuses to round.** A bare `ALTER COLUMN TYPE numeric(12,0)`
would work, but Postgres's numeric-to-lower-scale cast *rounds* rather than
truncating or erroring (verified: `SELECT 123456.49::numeric(12,0)` →
`123456`; `123456.50::numeric(12,0)` → `123457`) — exactly the "choose a
rounding rule for the caller" this phase's absolute rule forbids doing
silently. `20260722100927_HourlyRateWholeRialPrecision.cs`'s `Up()` runs a
`DO $$ ... RAISE EXCEPTION` guard first, refusing to proceed if any
`HourlyRate` is not already whole. **This guard fired for real**: applying
the migration to `tutorflow_test` (see below) hit six leftover rows —
`123456.78`, all created by repeated runs of the very Postgres test this
same commit range inverts (`pg-test-*@example.com` emails, the old
test's exact literal value). These were disposable integration-test
residue with no dependent `AvailabilitySlots`/`Sessions` rows (checked
before deleting) — removed, then the migration applied cleanly. This is
concrete evidence the guard is not theater: it caught a real fractional
value and refused to silently resolve it, precisely as designed.

**Applying to `tutorflow_dev`: blocked by a pre-existing condition, not by
this migration.** `tutorflow_dev`'s tables are owned by `postgres`; the
app's configured role (`tutorflow`, from `dotnet user-secrets`) is not the
owner and Postgres requires ownership (not just `GRANT`-able privileges)
for `ALTER COLUMN`. This is not new: `dotnet ef migrations list` showed
Phase 2.5's `RemoveMigrationArtifactColumnDefaults` still `(Pending)` on
`tutorflow_dev` — it has been silently stuck since that phase for the
identical reason (confirmed by reproducing the exact failure: `must be
owner of table AuthTokens`, a table this migration never touches). No
superuser credential exists in this environment (`user-secrets` has only
`tutorflow`'s), and none was sought beyond checking what was already
stored — guessing or escalating credentials was out of bounds. The failed
attempt rolled back cleanly (`tutorflow_dev` confirmed unchanged: same 6
applied migrations, `HourlyRate` still `numeric(10,2)`, before and after).
**Recommendation for the owner:** run `ALTER TABLE <each table> OWNER TO tutorflow;`
as a superuser against `tutorflow_dev` once; every pending migration
(this one and Phase 2.5's) will then apply cleanly via the normal
`dotnet ef database update`.

**Verified instead against `tutorflow_test`** — a real PostgreSQL 17
database where `tutorflow` does own the tables (and the one Postgres
connection `scripts/verify.ps1` actually depends on):

```
BEFORE — psql -U tutorflow -h localhost -d tutorflow_test -c '\d+ "Tutors"'
 Column      | Type          | ...
 HourlyRate  | numeric(10,2) | ...

AFTER  — same command, after `dotnet ef database update`
 Column      | Type          | ...
 HourlyRate  | numeric(12,0) | ...
```

**Existing tests that asserted the old precision:** exactly one —
`PostgresIntegrationTests.HourlyRate_decimal_precision_round_trips_exactly`,
which constructed `HourlyRate.Of(123456.78m)`. Inverted in its own commit
(`82053f8`) to `HourlyRate_whole_Rial_amount_round_trips_exactly_at_the_maximum`,
proving the more meaningful thing this migration needs proven against real
Postgres: `HourlyRate.MaxAmount` itself round-trips with zero precision
loss under `numeric(12,0)`. New Domain coverage: `HourlyRateTests.cs` (7
tests — positive, currency, zero/negative/fractional/exceeds-max
rejection, at-maximum success). New Application coverage: two tests added
to `SetTutorHourlyRateCommandHandlerTests.cs` (fractional, exceeds-max).
Full non-Postgres backend suite: Domain 60 (+7) + Application 220 (+2) +
Infrastructure 61 (unchanged) + Web 176 (unchanged this task) — all
passing, plus 7/7 Postgres integration tests including the inverted one.

## 4. Task 3 — the money module: 29 tests, all passing

`frontend/src/shared/money/rial.ts` mirrors `shared/time/`'s pattern
exactly: written and fully tested before any page was touched.
`tomanToRial`/`rialToToman` operate on integers only (`x10`/`÷10`, never
floating-point division), `formatToman` thousand-separates via
`Intl.NumberFormat`, and `isValidTomanAmount` backs the Zod schema. `MAX_RIAL`
mirrors `HourlyRate.MaxAmount` exactly (999,999,999,999); `MAX_TOMAN` is
its floor-divided-by-10 counterpart (99,999,999,999).

```
Test Files  1 passed (1)
     Tests  29 passed (29)
```

Covering, at minimum, what Task 3 asked for: exact round-tripping
(parameterized over 0, 1, 45,000, 500,000, 1,234,567, and `MAX_TOMAN`),
thousand-separator formatting (three magnitudes, plus the maximum), zero
(all three functions), and the largest value the schema allows.

**The absolute rule's flagged path, made concrete and tested, not
silently resolved:** `rialToToman` throws — does not round — when given a
Rial amount not evenly divisible by 10, since such a value has no exact
whole-Toman representation. Two tests prove this deliberately: one for an
arbitrary non-multiple (`45`), and one specifically for `HourlyRate.MaxAmount`
itself (`999,999,999,999`, not divisible by 10) — the true schema ceiling
is one such value, by construction, which is expected and harmless since
nothing this product writes can ever reach it through the Toman-only UI
(the nearest reachable value is `999,999,999,990`). Documented in
`README.md`'s Known pitfalls (Task 6) rather than papered over with an
invented rounding rule.

## 5. Task 4 — Toman in, Toman out, Rial on the wire throughout

`TutorOfferingPage`'s hourly-rate field is now labelled "Hourly rate
(Toman)"; its default value is `rialToToman(tutor.hourlyRate)` and its
submitted value is `tomanToRial(Number(values.hourlyRate))` — the mutation
call (`setTutorHourlyRate`) still receives Rial, matching the unchanged
API contract. Every display surface now reads `formatToman(row.hourlyRate)`
with an explicit "(Toman)" column header or inline label:
`TutorDetailPage`, `TutorDirectoryPage`, `TutorOfferingPage`, and
`TutorSearchPage` (found by grep — not explicitly named by Task 4, but a
genuine "anywhere else" hit, matching Phase 3's own precedent of following
grep past the pages named by name).

```
$ grep -rn "hourlyRate" frontend/src --include=*.tsx --include=*.ts \
    | grep -v .test. | grep -v "formatToman\|rialToToman\|tomanToRial\|isValidTomanAmount"
```
returns only field-key/dirty-field/type-declaration lines — no component
renders `row.hourlyRate`/`tutor.hourlyRate` directly.

Existing test fixtures using `hourlyRate: 40` (Rial) were updated to
divisible-by-10 values (`400`, `500_000`) in this same commit: `40` Rial
is not a multiple of 10, and would now throw when `formatToman`/`rialToToman`
tried to render it — not a bug in the fixture's *validity* (`HourlyRate.Of(40)`
is still legal), but exactly Section 4's flagged edge case surfacing in
test data written before this phase existed. A new test proves the actual
Toman→Rial conversion at submit time (`50,000` Toman entered →
`setTutorHourlyRate` called with `500,000`); `TutorDetailPage`'s test
gained an assertion on the rendered `"50,000 Toman"` text. All 225
frontend tests pass (`npm test -- --run`); production bundle size:
essentially unchanged (`money/rial.ts` chunk is small; every touched page
chunk moved by low tens of bytes from the label/import changes only).

## 6. Task 5 — boundary validation, proven at the real endpoint

No new validation code was needed at the Application/Web layer — `ADR-007`
requires a business rule live in exactly one place, and `HourlyRate.Of`
(Domain) already became the sole authority for "positive, whole,
≤ MaxAmount" in Task 2, with the existing `catch (ArgumentException)` in
`SetTutorHourlyRateCommandHandler` already turning any of the three
rejections into a `Result` the Web layer maps to `400`. Task 5's actual
work was proving that boundary behavior against the real HTTP pipeline,
which a general "duplicate the check in Application" approach would not
have added anything to. Four new `Web.Tests`, end-to-end through
`WebApplicationFactory`:

- **Rejection**, each asserting `400` and the (renamed, since one error
  code now covers three reasons) `"SetTutorHourlyRateCommand.Amount.Invalid"`
  code: a negative amount, a fractional amount (message asserted to
  contain "whole number"), and an amount exceeding `HourlyRate.MaxAmount`.
- **Acceptance**: `HourlyRate.MaxAmount` itself succeeds and round-trips
  through a subsequent `GET /tutors/{id}` — the true schema boundary is
  provably usable, not just provably rejected past it.

All 11 `SetTutorHourlyRate`-related `Web.Tests` pass; full backend suite
(510 non-Postgres tests across 4 projects + 7 Postgres) passes.

## 7. Task 6, verify.ps1 results, and merge recommendation

**Documentation** (`93b43f4`): `CLAUDE.md` gained the phase's required
sentence verbatim. `README.md`'s Known pitfalls gained two entries: the
not-divisible-by-10 finding (Section 4) and `MaxAmount` being one such
value itself, by construction and harmlessly. `docs/MANUAL-SMOKE-TEST.md`'s
offering step now names the Toman field explicitly and keeps the
underlying Rial value as an explanatory note, mirroring Phase 3's
treatment of the Tehran-time field.

**`scripts/verify.ps1`, real output, unmodified for this run:**

```
=================== VERIFY SUMMARY ===================
dotnet restore                                PASS     0.7s
dotnet build (0 warnings)                     PASS     4.7s
Backend: dotnet test TutorFlow.sln (run 1/2)  PASS    20.2s
Backend: dotnet test TutorFlow.sln (run 2/2)  PASS    16.3s
Backend: Postgres integration tests           PASS     3.5s
npm ci                                        PASS    22.8s
Frontend: npm run lint                        PASS    10.1s
Frontend: npm run build                       PASS    13.4s
Frontend: npm test -- --run                   PASS    50.8s
-------------------------------------------------------
RESULT: PASS (9/9 steps green)
```

Backend totals, both determinism-guard runs: Domain 60 + Application 220 +
Infrastructure 68 (61 + 7 Postgres) + Web 180 = **528 tests**, both runs.
Frontend: 225 tests.

**Merge recommendation: safe to merge to `main`**, with one follow-up item
for the owner, not a blocker: grant `tutorflow` ownership of `tutorflow_dev`'s
tables (Section 3) so this migration — and the one from Phase 2.5 still
silently pending behind it — can actually apply there. Nothing in this
phase's own scope depends on that database; `scripts/verify.ps1`'s
Postgres step uses `tutorflow_test`, already fully migrated and verified.
Every absolute rule held: no payment/gateway/invoice code, no multi-
currency abstraction, no rounding rule invented (the one path found was
reported and tested, not silently resolved), no test weakened or deleted.
