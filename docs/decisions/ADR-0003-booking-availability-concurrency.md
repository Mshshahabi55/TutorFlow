# ADR-0003: Booking/Availability Concurrency Mechanism

**Status:** Accepted

## Context

`docs/product/TRIAL_BOOKING_WORKFLOW.md` requires that two students cannot both acquire a hold on the same (tutor, slot). This requires a real concurrency guarantee, decided before `Bookings`/`Availability` are scaffolded.

## Decision

### Hold ownership (resolves A2)

`Bookings` owns the slot-hold lifecycle. A hold is modeled as a `Booking` row in `Held` state — it is part of the booking attempt/lifecycle, not a separate concept.

`Availability` calculates/queries available time slots but does **not** own booking holds. To compute open slots, `Availability` must treat active holds owned by `Bookings` as unavailable — via a query or consumed integration event from `Bookings`, never a shared table.

```
Availability
    |
    | calculates available slots
    v
Booking request
    |
    v
Bookings creates/owns Hold
    |
    v
Payment
    |
    v
Confirmed Booking
```

### Concurrency mechanism (resolves A3)

Booking concurrency is protected at the PostgreSQL database level — never solely by frontend checks or application-level "check then write" locks.

- Use a database transaction plus a PostgreSQL constraint/locking mechanism in `Bookings`' own schema.
- **Preferred mechanism:** a PostgreSQL range/exclusion constraint (`EXCLUDE USING gist`, via the `btree_gist` extension) over `(tutor_id, during tsrange)` scoped to active statuses (`Held`, `Confirmed`) — this makes the database itself reject a second overlapping insert for the same tutor/time interval.
- The exact EF Core mapping (e.g. raw SQL migration for the exclusion constraint, since EF Core has no first-class mapping for PostgreSQL exclusion constraints) is an implementation detail decided when `Bookings` is scaffolded — it must preserve the invariant below, but this ADR does not prescribe the literal migration SQL.

**Invariant:** a tutor cannot have overlapping active holds/confirmed bookings for the same time interval.

Expired holds must cease blocking availability — the exclusion constraint and any availability query must scope to non-expired holds (e.g. `status = 'Held' AND expires_at > now()`); the exact expiry sweep mechanism (background job vs. expiry check at read-time) is an implementation detail, not decided here.

### Hold TTL

Approved initial default: **10 minutes.**

- The hold's expiration is stored explicitly on the hold/`Booking` row (an `ExpiresAtUtc` column), not derived implicitly from `CreatedAtUtc` + a hardcoded constant scattered through the codebase.
- The TTL value itself is sourced from configuration (`docs/CLAUDE.md` Options pattern), so it can change later without a redesign.

## Consequences

- `Bookings`' schema carries the exclusion constraint and the explicit hold-expiration column; this is now in scope for `Bookings`' first feature.
- `Availability` must call into `Bookings` (query or consumed event) to know about active holds rather than sharing a table — this is the first concrete example of the cross-module query/event pattern required by `ADR-0001`.
- This also resolves Open Question #14 (Hold TTL duration).

This ADR supersedes the three options previously listed as non-binding recommendations; option 1 (database constraint) is now the accepted mechanism, refined to an exclusion constraint rather than a simple unique constraint to correctly model overlapping *intervals* rather than exact-match slot starts.
