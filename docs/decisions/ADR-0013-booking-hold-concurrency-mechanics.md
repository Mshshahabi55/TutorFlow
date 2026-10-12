# ADR-0013: Booking Hold & Overlap Concurrency Mechanics

**Status:** Proposed — requires Architect sign-off before `Bookings` is scaffolded

## Context

`ADR-0003` (Accepted) already settled the *direction*: `Bookings` owns the hold lifecycle, protected by a PostgreSQL exclusion constraint, with a 10-minute default TTL. `ADR-0003` explicitly deferred the literal mechanics: *"The exact EF Core mapping... is an implementation detail decided when `Bookings` is scaffolded."* This ADR provides those mechanics so the first `Bookings` implementation slice has a concrete, reviewable design rather than rediscovering it mid-implementation.

**Correction (architecture review, 2026-10-04):** the version of this ADR originally circulated for sign-off scoped the exclusion constraint and the expiry sweep to `Held`/`Confirmed` only. That scope reopens the exact double-booking window this ADR exists to close: the moment a booking leaves `Held` for `PendingPayment` (checkout), it fell outside the constraint's predicate and a second student could acquire a conflicting hold for the same tutor/interval while the first was mid-checkout. §1 and §2 below are corrected to scope both the constraint and the sweep to `Held`, `PendingPayment`, and `Confirmed` — the full set of statuses during which a booking occupies the tutor's calendar.

## Decision

### 1. The overlap guard is a PostgreSQL exclusion constraint, not application logic

```sql
CREATE EXTENSION IF NOT EXISTS btree_gist;

ALTER TABLE bookings.bookings
    ADD COLUMN during tstzrange GENERATED ALWAYS AS (tstzrange(start_utc, end_utc, '[)')) STORED;

ALTER TABLE bookings.bookings
    ADD CONSTRAINT ex_bookings_no_overlap
    EXCLUDE USING gist (tutor_user_id WITH =, during WITH &&)
    WHERE (status IN ('Held', 'PendingPayment', 'Confirmed'));
```

- `btree_gist` is required because a GiST index natively supports the range-overlap operator (`&&`) but needs this extension's operator class to *also* support plain equality (`=`) on `tutor_user_id` within the same index.
- `during` is a `GENERATED ALWAYS ... STORED` column derived from the plain `start_utc`/`end_utc` columns (`timestamptz`, mapped from `DateTimeOffset` as in every other module) — the application never writes to `during` directly; it stays in sync automatically. `StartUtc`/`EndUtc` remain the columns the application reads and writes, consistent with `TimeOff`'s existing convention in `Availability`.
- **The constraint's `WHERE` clause scopes protection to `Held`, `PendingPayment`, and `Confirmed` rows** — every status during which the booking occupies the tutor's calendar and must not be double-allocated. This widens `ADR-0003`'s originally-stated `Held`/`Confirmed` scope (corrected above): `PendingPayment` must be included, or the slot is unprotected for the entire duration of checkout. A booking only ever leaves this protected set by becoming `Cancelled`, `Expired`, or `Rescheduled` — all of which free the interval — or `Refunded`/`Completed`/`NoShow`, which are unreachable without first passing through `Confirmed` and then `Cancelled` (see `BOOKINGS_SPEC.md` §2's authoritative transition table). Partial exclusion constraints are supported by PostgreSQL (exclusion constraints are index-backed, and partial indexes support `WHERE`).
- `[)` (half-open) interval semantics are used, matching the half-open overlap convention already used by `Availability`'s `TimeOff`/`OccupiedInterval` checks (`slotStart < t.EndUtc && slotEnd > t.StartUtc`) — a booking ending exactly when another starts is *not* a conflict.
- EF Core has no first-class mapping for PostgreSQL exclusion constraints or generated columns; both are added via `migrationBuilder.Sql(...)` in the initial migration, not through the fluent model builder. This is an accepted, narrowly-scoped use of raw SQL in an otherwise EF-managed schema.

**Invariant preserved:** a tutor cannot have overlapping active (`Held`/`PendingPayment`/`Confirmed`) rows for the same time interval, for the entire lifetime of a booking attempt — including the checkout window, not merely before and after it. The database rejects the second conflicting `INSERT` with PostgreSQL error `23P01` (`exclusion_violation`) — this is the actual double-booking guarantee, not an application-level "check then write."

### 1a. Required invariant: `HoldExpiresAtUtc` must be set for every row inside the protected set that isn't yet `Confirmed`

The sweep in §2 and the constraint above are only correct together if this invariant holds without exception: **`HoldExpiresAtUtc` is non-null if and only if `Status` is `Held` or `PendingPayment`, and is cleared (set to `null`) the instant a row becomes `Confirmed`, `Cancelled`, `Expired`, or `Rescheduled`.** If a `Held` or `PendingPayment` row were ever persisted with a `null` `HoldExpiresAtUtc` (e.g. a bug in the checkout handler that forgets to carry it forward), the sweep's `hold_expires_at_utc < now()` predicate can never match that row, and it would block its tutor/interval indefinitely with no mechanism to recover it short of manual intervention. This is restated as a hard precondition, not merely a modeling nicety — every handler that writes `Status` must also write `HoldExpiresAtUtc` in the same statement. See `BOOKINGS_SPEC.md` §1.5 for the full invariant list.

### 2. Expired holds cease blocking availability via a lazy, transactional sweep — not a background job

An index predicate cannot reference `now()` (volatile, not IMMUTABLE), so the exclusion constraint can only ever filter on `status`, never on `hold_expires_at_utc` directly. Consequently, a `Held` **or `PendingPayment`** row whose TTL has passed must have its `status` actually flipped to `Expired` before the constraint will stop treating it as active — this now matters for both statuses, not only `Held`, since both are inside the widened protected set in §1.

**Required mechanism:** every hold-creation attempt first executes, in the same transaction as the subsequent insert:

```sql
UPDATE bookings.bookings
SET status = 'Expired'
WHERE tutor_user_id = @tutorUserId AND status IN ('Held', 'PendingPayment') AND hold_expires_at_utc < now();
```

scoped to the specific `tutor_user_id` being requested. This is **correctness-critical**, not a hygiene task — without it, a stale `Held`/`PendingPayment` row would continue blocking new holds for that tutor/interval indefinitely. This also now realizes the `PendingPayment → Expired` transition itself (`BOOKINGS_SPEC.md` §2): a booking stuck in `PendingPayment` past the hold TTL (e.g. the student abandoned checkout, or a payment webhook never arrives) is expired by this same sweep, the same clock, the same statement — no second TTL concept. Default PostgreSQL `READ COMMITTED` isolation is sufficient for this statement plus the following insert: the exclusion constraint itself is what provides atomicity against *other concurrent requests*, not the isolation level.

A periodic background sweep (for general row hygiene / observability, cleaning up `Expired` rows that are never superseded) is explicitly **out of scope** for this ADR and for the first `Bookings` implementation slice — it is not required for correctness, and adding a `BackgroundService` for it now would be speculative infrastructure ahead of need.

### 3. Hold creation uses `READ COMMITTED`; state transitions use conditional updates, not read-then-write

- **Hold creation** (`Held` row insert): default `READ COMMITTED`. No `SERIALIZABLE` transaction is needed here — unlike `Availability`'s `ReplaceWeeklyRules` (which has no database constraint to lean on and must detect write-skew itself via serializable isolation, see `ADR-0012`'s sibling concern), hold creation has the exclusion constraint as its safety net. The application still wraps the expiry-sweep `UPDATE` and the `INSERT` in one transaction so they're atomic together, but `SERIALIZABLE` is unnecessary overhead here.
- **State transitions** (`Confirm`, `Cancel`, `Expire`): a single conditional `UPDATE ... WHERE id = @id AND status = @expectedStatus`, inspecting the affected-row count (EF Core: `ExecuteUpdateAsync`) rather than reading the entity, checking its status in C#, then calling `SaveChangesAsync`. A single `UPDATE` statement is already atomic in PostgreSQL; this avoids needing a separate optimistic-concurrency token (and avoids repeating the `xmin`-mapping unavailability already hit in this Npgsql version during `Identity`/`Availability` work). If the affected-row count is `0`, the handler re-reads the row's current status to distinguish "already in the target state" (idempotent success — e.g. a duplicate payment-success notification) from "in an unexpected state" (a real conflict, surfaced as an error).

### 4. Required indexes

- The exclusion constraint's own GiST index (created implicitly by the `EXCLUDE` clause) serves double duty as the primary lookup for "does an active hold/booking exist for this tutor/interval."
- `CREATE INDEX ix_bookings_student_user_id ON bookings.bookings (student_user_id);` — for "my bookings as a student."
- `CREATE INDEX ix_bookings_tutor_user_id ON bookings.bookings (tutor_user_id);` — for "my bookings as a tutor" (the GiST index above is range-oriented and not efficient for a plain equality scan across all statuses).
- No index is needed on `status` alone at this scale; it is always queried in combination with `tutor_user_id` or `student_user_id`.

## Consequences

- The initial `Bookings` migration contains both EF-generated DDL (ordinary columns, plain indexes) and a block of raw SQL (`btree_gist` extension, generated column, exclusion constraint) — reviewers should expect and check this mix, not treat raw SQL in a migration as automatically suspicious.
- Application code must catch PostgreSQL's `23P01` (`exclusion_violation`) on hold-creation `INSERT`s and translate it into a clean "slot no longer available" response — this is expected, routine contention, not a bug, and must not surface as an unhandled 500 (the project has already hit and fixed exactly this class of bug for unique-violation and serialization-failure errors in `Identity`, `Tutors`, and `Availability`).
- No optimistic-concurrency column is added to `Booking`; all state transitions are expressed as conditional `UPDATE`s with affected-row-count checks.
- This ADR does not decide the hold TTL's configuration key name, the exact trial durations, or any cancellation/reschedule/no-show business rule — those remain outside its scope.

## Required concurrency tests (specification — not implemented; `Bookings` does not exist yet)

These extend `BOOKINGS_SPEC.md` §9's existing concurrency test list and exist specifically to prove the corrected (§1/§2) design, once `Bookings` is scaffolded. All use Testcontainers PostgreSQL through the real database, never a mock:

1. **`PendingPayment` is protected**: create a `Held` booking, transition it to `PendingPayment` (simulated, no real `Payments`), then attempt a second, overlapping hold-creation `INSERT` for the same tutor — must fail with `23P01`, mapped to `409`. This is the regression test for the gap this correction closes; it must fail against the original (`Held`/`Confirmed`-only) constraint scope and pass against the corrected one.
2. **Sweep expires a stale `PendingPayment` row**: create a booking, force it to `PendingPayment` with a `HoldExpiresAtUtc` in the past (direct DB manipulation, matching `Availability`'s existing precedent for forcing time-based states), then attempt a new hold for the same tutor/interval — must succeed, proving the sweep un-blocks `PendingPayment` rows, not only `Held` ones.
3. **Sweep does not touch unrelated tutors**: two tutors, one with a stale `Held`/`PendingPayment` row and one without — the sweep's `tutor_user_id` scoping must leave the second tutor's rows untouched (asserted via row count / `UpdatedAtUtc`, not just absence of error).
4. **`HoldExpiresAtUtc` invariant enforcement**: a domain-level (no DB) test asserting every status-changing method on `Booking` either sets a non-null `HoldExpiresAtUtc` (entering `Held`/`PendingPayment`) or clears it to `null` (entering any other status) — guards §1a's invariant at the unit-test level, not only via integration behavior.
5. **Exclusion constraint DDL assertion**: a focused integration test querying PostgreSQL's catalog (`pg_constraint` / `information_schema`) after migration to assert the constraint's definition literally includes `PendingPayment` in its `WHERE` clause — prevents a future migration edit from silently narrowing the scope back down without a test failing.
