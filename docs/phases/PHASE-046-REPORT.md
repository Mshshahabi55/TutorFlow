# Phase 4.6 Report — Slot Release

Branch: `develop` | Commits this phase: `d379619` (Task 1 — failing test),
`8186740` (Task 2 — `AvailabilitySlot.Reopen()` and wiring), `b20d19e`
(Task 3 — filtered unique index), `e3c4a1c` (Task 4 — `Session.Price`).
Task 5 was report-only — no code change; its content is Section 5 below,
folded into this report's own commit.

**Status: complete, 9/9 green.** This phase fixed a live defect Phase 5A's
study surfaced, independent of any payments work: `AvailabilitySlot.IsConsumed`
was never reset by `Session.Cancel()`, and the unique index enforcing
CONST-1 was unconditional — so a cancelled Session blocked its slot
forever, with no way to rebook it and no admin tooling to recover it.
`DOMAIN_MODEL.md` Open Question 7 is now formally resolved: cancelling a
Session reopens its Availability Slot. Task 4 additionally captured the
Session's price at booking time — a Domain-modeling prerequisite for any
future payments phase, not payments work itself; no `Payment` aggregate,
gateway, or ADR-020 implementation was added.

## 1. Clean Architecture validation

Domain gained `AvailabilitySlot.Reopen()`, the `AvailabilitySlotReopened`
event, `Session.Price`, `Domain.Scheduling.ValueObjects.SessionPrice`, and
`Domain.Common.RialAmount` (a currency-level rule owned by no single
context, extracted out of `HourlyRate` — the same role
`Guard`/`AggregateRoot`/`DomainEvent` already play from `Domain.Common`).
Domain still has zero outward dependency. Application's
`CancelSessionCommandHandler` gained a dependency on
`IAvailabilitySlotRepository`; `BookSessionCommandHandler` gained a
dependency on `ITutorRepository` — both are Application depending on
Domain-defined interfaces, never the reverse, and both are cross-context
reads through the *owning* context's own repository interface
(`ADR-002` Integration Rules), the same pattern
`_studentRepository`/`_relationshipRepository` already established in
`BookSessionCommandHandler`. `SessionPrice` is deliberately its own value
object, not a reused `HourlyRate` — Session belongs to Scheduling &
Booking, `HourlyRate` to Identity & Relationship, and no existing
cross-context reference in this codebase is a full value object (always
an id, e.g. `TutorId`) — reusing `HourlyRate` directly would have been a
new, precedent-breaking kind of coupling. Infrastructure changes are
confined to `SessionConfiguration.cs`, `AuditDomainEventHandler.cs`, and
the two new migrations. No new package was added.
`dotnet build TutorFlow.sln`: **0 Warnings, 0 Errors**.

## 2. Repository convention validation

Four commits on `develop`, task order, Conventional Commits, each
documenting its own architectural reasoning (cross-context avoidance,
no-proration decision, nullable mirroring `HourlyRate`, the
`ADR-004`/`ADR-015` atomicity precedent). **No database row was modified
or deleted this phase** — Task 3's migration changes only an index
definition; Task 4's migration adds a new nullable column, which
Postgres backfills with `NULL` for every existing row, not a value this
session chose or wrote. Both migrations were applied to `tutorflow_dev`
and `tutorflow_test`. No secret committed. No test weakened, skipped, or
deleted — Task 1's test was committed failing first (Phase 1B precedent)
and made to pass by Tasks 2 and 3 without being altered itself.

## 3. Task 1 & Task 3 — the failing test, and the index before/after

**Task 1 — proving the defect before touching anything.** Commit
`d379619` added `CancelSession_reopens_the_slot_so_it_can_be_rebooked` to
`SchedulingEndpointsTests.cs`: book a slot, cancel the Session, attempt to
rebook the same slot as a different student. Run against the
pre-fix code, this failed with:

```
409 Conflict
"errorCode": "BookSessionCommand.InvalidState"
"message": "This Availability Slot has already been consumed and cannot
             produce another Session."
```

This is direct evidence, not inference, that the rejection came from
`AvailabilitySlot.Book()`'s in-memory `if (IsConsumed) throw` guard — the
**Domain-level check**, not the database unique index. Proof: a rejection
from the unique index instead raises `ConcurrencyConflictException`,
which `BookSessionCommandHandler`'s separate `catch` clause turns into a
distinct error, `"BookSessionCommand.SlotAlreadyBooked"` — a different
string than what was observed. The second booking attempt never reached
the database at all in this scenario; it was rejected in memory before
any SQL was issued. **Conclusion: the defect lived in both places** —
`IsConsumed` was never reset (the immediate cause of this specific
failure) *and* the unique index was unconditional (confirmed separately,
below, and by re-running this same test after Task 2's fix alone: the
error changed to `"BookSessionCommand.SlotAlreadyBooked"`, proving the
index was the next and final blocker once `IsConsumed` no longer was).
Both had to be fixed for the defect to be closed; this test now passes
after Tasks 2 and 3.

**Task 3 — filtering the index.** `Sessions.AvailabilitySlotId`'s unique
index changed from unconditional to `WHERE "Status" <> 2` (Cancelled).
Predicate reasoning: CONST-1 only needs to prevent two *live* Sessions
from ever referencing the same slot at once — Cancelled means "this never
actually happened," so a slot may accumulate any number of Cancelled rows
over time once it is reopened. Completed and NoShow deliberately remain
**inside** the constraint (they represent a booking that did happen), so
`WHERE "Status" <> 2` was chosen over the equivalent-looking
`WHERE "Status" = 0` specifically to keep those two statuses protected —
this also matches Section 5's recommendation that NoShow should not
reopen its slot.

`psql \d+ "Sessions"` **before** (reconstructed exactly from the parent
commit `23f84b3`'s `SessionConfiguration.cs`, which read
`builder.HasIndex(s => s.AvailabilitySlotId).IsUnique();` with no filter,
and predates Task 4's `Price` column):

```
                                                       Table "public.Sessions"
       Column       |           Type           | Collation | Nullable | Default | Storage | Compression | Stats target | Description
--------------------+--------------------------+-----------+----------+---------+---------+-------------+--------------+-------------
 Id                 | uuid                     |           | not null |         | plain   |             |              |
 TutorId            | uuid                     |           | not null |         | plain   |             |              |
 StudentId          | uuid                     |           | not null |         | plain   |             |              |
 ParentGuardianId   | uuid                     |           |          |         | plain   |             |              |
 AvailabilitySlotId | uuid                     |           | not null |         | plain   |             |              |
 ScheduledTimeUtc   | timestamp with time zone |           | not null |         | plain   |             |              |
 Duration           | interval                 |           | not null |         | plain   |             |              |
 DeliveryMode       | integer                  |           | not null |         | plain   |             |              |
 Status             | integer                  |           | not null |         | plain   |             |              |
Indexes:
    "PK_Sessions" PRIMARY KEY, btree ("Id")
    "IX_Sessions_AvailabilitySlotId" UNIQUE, btree ("AvailabilitySlotId")
    "IX_Sessions_StudentId" btree ("StudentId")
    "IX_Sessions_TutorId" btree ("TutorId")
Foreign-key constraints:
    "FK_Sessions_AvailabilitySlots_AvailabilitySlotId" FOREIGN KEY ("AvailabilitySlotId") REFERENCES "AvailabilitySlots"("Id") ON DELETE RESTRICT
Access method: heap
```

`psql \d+ "Sessions"` **after** (captured live against `tutorflow_test`,
post Task 3 *and* Task 4 — the `Price` column and the filtered index both
present):

```
                                                       Table "public.Sessions"
       Column       |           Type           | Collation | Nullable | Default | Storage | Compression | Stats target | Description
--------------------+--------------------------+-----------+----------+---------+---------+-------------+--------------+-------------
 Id                 | uuid                     |           | not null |         | plain   |             |              |
 TutorId            | uuid                     |           | not null |         | plain   |             |              |
 StudentId          | uuid                     |           | not null |         | plain   |             |              |
 ParentGuardianId   | uuid                     |           |          |         | plain   |             |              |
 AvailabilitySlotId | uuid                     |           | not null |         | plain   |             |              |
 ScheduledTimeUtc   | timestamp with time zone |           | not null |         | plain   |             |              |
 Duration           | interval                 |           | not null |         | plain   |             |              |
 DeliveryMode       | integer                  |           | not null |         | plain   |             |              |
 Status             | integer                  |           | not null |         | plain   |             |              |
 Price              | numeric(12,0)            |           |          |         | main    |             |              |
Indexes:
    "PK_Sessions" PRIMARY KEY, btree ("Id")
    "IX_Sessions_AvailabilitySlotId" UNIQUE, btree ("AvailabilitySlotId") WHERE "Status" <> 2
    "IX_Sessions_StudentId" btree ("StudentId")
    "IX_Sessions_TutorId" btree ("TutorId")
Foreign-key constraints:
    "FK_Sessions_AvailabilitySlots_AvailabilitySlotId" FOREIGN KEY ("AvailabilitySlotId") REFERENCES "AvailabilitySlots"("Id") ON DELETE RESTRICT
Access method: heap
```

Both migrations were applied to `tutorflow_dev` and `tutorflow_test` via
`dotnet ef database update`. A new Postgres integration test,
`Sessions_AvailabilitySlotId_filtered_unique_index_permits_rebooking_a_cancelled_slot`,
proves the physical constraint directly against the real column: one
Cancelled row and one live row for the same `AvailabilitySlotId` coexist
without violating the index, while the pre-existing double-booking
rejection test (two live rows) still fails as expected — confirming the
filter is exactly as narrow as intended, no wider.

## 4. Task 2 — `AvailabilitySlot.Reopen()`

```csharp
public void Reopen(SessionId cancelledSessionId)
{
    if (!IsConsumed)
    {
        throw new InvalidOperationException(
            "Only a consumed Availability Slot can be reopened.");
    }

    IsConsumed = false;
    RaiseDomainEvent(new AvailabilitySlotReopened(Id, cancelledSessionId));
}
```

Invariants, explicit choices, and why:

- **Called on a never-consumed slot: throws.** A real misuse worth
  surfacing (calling Reopen on a slot nothing ever booked is a caller
  bug), not a case to silently no-op.
- **Called twice in a row (idempotency): throws the second time**, for
  the same reason — `IsConsumed` is already `false`, so the guard fires.
  This is intentional, not an oversight: idempotent reopening was not
  asked for, and the only real caller
  (`CancelSessionCommandHandler`) only ever calls it once, immediately
  after a `Session.Cancel()` that itself throws if the Session is not
  currently `Scheduled` — so a double-cancel can never reach `Reopen()`
  twice for the same Session.
- **Called on a slot whose session is still active:** structurally
  impossible to observe from inside `Reopen()` itself.
  `AvailabilitySlot` holds no reference to the Session that consumed it
  — by design, aggregates reference each other by identity only, and
  only `Session` holds `AvailabilitySlotId`, never the reverse — so this
  method cannot verify "the session being cancelled is really the one
  that consumed this slot," or that it is genuinely no longer active.
  That ordering guarantee (cancel the Session first, confirm it
  succeeded, only then reopen the slot it names) is enforced entirely by
  the calling handler, not by this aggregate; `cancelledSessionId` is
  accepted only for audit attribution via `AvailabilitySlotReopened`, not
  validated here.
- **Called on a slot now in the past:** deliberately **not** guarded.
  `Book()` itself has never guarded against booking a slot whose start
  time has already passed (no minimum/maximum lead time exists —
  `DOMAIN_MODEL.md` Open Question 8, still open). Adding an asymmetric
  "cannot reopen a past slot" rule here, where none exists on the
  original booking path, would have been inventing a new business rule
  this task was not authorized to decide, not fixing a bug.

**Wiring.** `CancelSessionCommandHandler` is the only caller. After
`session.Cancel()` succeeds, it loads the `AvailabilitySlot` via
`session.AvailabilitySlotId`, calls `slot.Reopen(session.Id)`, and saves
both `session` and `slot` in one `SaveChangesAsync` call — the mirror
image of `BookSessionCommandHandler`'s own already-shipped shape, which
already mutates both aggregates together for the opposite operation
(booking). This is not a new departure from `ADR-015`'s "one aggregate
per transaction" heuristic: `ADR-004`'s Transaction Boundaries section
explicitly anticipated this exact atomicity once Question 7 resolved
("what happens to the associated Availability Slot on cancellation... the
full transactional shape of cancellation is not yet complete"), and
booking already established the precedent for the same pair of
aggregates. If the slot is structurally missing (a referential-integrity
break, not a business scenario), the handler throws
`InvalidOperationException`, treated as an Infrastructure Failure
(`ADR-008`), not silently skipped.

**Audit.** `AvailabilitySlotReopened` was added to
`AuditDomainEventHandler`'s allowlist switch, keyed by the slot's own id
— a distinct audit subject from `SessionCancelled` (keyed by the Session,
which does not itself surface which slot was freed). This satisfies
CLAUDE.md's rule that every new Domain Event mutating governance-relevant
state must be covered by the audit trail.

## 5. Task 4 (`Session.Price`) and Task 5 (Reschedule/MarkNoShow report)

**Task 4.** `Session.Price` (`SessionPrice?`) captures the Tutor's
`HourlyRate.Amount` as it stands at the moment `BookSessionCommandHandler`
books the Session — fixed forever after, immune to the Tutor later
changing their rate. Validation is shared, not duplicated: the
divisibility/range rule was extracted from `HourlyRate` into
`Domain.Common.RialAmount`, and both `HourlyRate` and the new
`SessionPrice` now delegate to it, with zero behavior change to
`HourlyRate`'s own public API. Two decisions worth flagging as
deliberate, not oversights:

- **No duration-proration.** `Price` is the Tutor's rate as-is, not
  `rate × duration`. Proration could produce a result not evenly
  divisible by 10 depending on the rate/duration combination, and
  choosing a rounding rule for that would have been a new business rule
  outside this task's authorization (the same restraint `ADR-019`
  Addendum 1 already exercised for a similar rounding question).
- **Nullable, mirroring `Tutor.HourlyRate?`.** A Tutor with no configured
  rate is still bookable today — no existing rule blocks it — so a
  Session booked against such a Tutor has no price to capture, not a
  fabricated one.

**Migration and existing rows.** `AddSessionPrice` adds a nullable
`Price numeric(12,0)` column; Postgres itself backfills `NULL` for every
existing row (`tutorflow_dev`: 0 Session rows affected; `tutorflow_test`:
34 disposable test rows). This was not a default value chosen by this
session — `NULL` is Postgres's own behavior for a new nullable column
with no explicit default, and here it reads as an honest "this historical
fact was never recorded," the identical reasoning that already makes
`HourlyRate` itself nullable. No row was modified or deleted. `Price` is
not yet exposed via `SessionDto` or any endpoint, per this task's own
scope.

**Task 5 — Reschedule and MarkNoShow, current behavior (no code change).**
Both `RescheduleSessionCommandHandler` and `MarkSessionNoShowCommandHandler`
were read in full for this report. Neither injects
`IAvailabilitySlotRepository`; neither loads, reads, or mutates an
`AvailabilitySlot` in any way. Each only loads the `Session`, authorizes,
calls a single Domain method, and saves the `Session` alone.

*Reschedule* (`Session.Reschedule(newScheduledTimeUtc)`): mutates only
`ScheduledTimeUtc`, in place, on the same `Session` row; `Status` stays
`Scheduled`; `AvailabilitySlotId` is never reassigned — there is no
concept of "moving" to a different `AvailabilitySlot`. Net effect: the
original slot's `IsConsumed` stays `true` for as long as the Session
remains live, at the *original* slot's time — which is arguably correct
for that slot (a live Session still exists, still referencing it), but
exposes a real gap: `AvailabilitySlot.StartTimeUtc`/`Duration` still
describe the **old** time, not the Session's new one. Nothing in the
system prevents a second, independent `AvailabilitySlot` from being
declared and booked at the Session's **new** time — the reschedule
target is not protected by CONST-1 or any other invariant, because
`AvailabilitySlot` was never designed to move.

*Recommendation:* this is a genuine gap, but closing it is not a small
follow-on to this phase — it requires deciding what a "rescheduled slot"
even means structurally (does the original `AvailabilitySlot` move with
the Session, via a new `AvailabilitySlot.Move()`/`Reschedule()` Domain
method? does rescheduling instead require declaring and booking a brand
new slot at the new time, reopening the old one, mirroring cancel-and-
rebook?) — a new Domain method and a new business rule neither
`DOMAIN_MODEL.md` nor any Accepted ADR currently settles. Per CLAUDE.md's
stop conditions, this needs a new ADR or an owner decision before any
code changes; it is out of this phase's scope by the owner's own
framing ("Rescheduling... are not covered by this decision").

*MarkNoShow* (`Session.MarkNoShow()`): mutates only `Status`, from
`Scheduled` to `NoShow`. Same absence of any `AvailabilitySlot` access.
Net effect: the slot stays permanently `IsConsumed = true` after a
No-Show, with no path to ever reopen it — and Task 3's index predicate
(`WHERE "Status" <> 2`) deliberately keeps NoShow **inside** the unique
constraint, reinforcing the same outcome at the storage layer.

*Recommendation:* **no change.** This is very likely the correct
behavior already, not a defect: a No-Show means the Tutor's time was
genuinely reserved and the appointment happened from the Tutor's side —
the Student simply didn't attend. Reopening that slot would let someone
else book the same Tutor for time that was, in substance, already spent.
This mirrors why Completed sessions also stay inside the constraint. No
code change is proposed.

## 6. `scripts/verify.ps1`, real output, unmodified for this run

Two unrelated environment locks were hit and cleared before the run
below — a stale `esbuild.exe` process left over from a previous `npm ci`,
and a leftover `npm run dev` (Vite) dev server — both stateless dev
processes, killed via `Stop-Process`, same precedent as this session's
earlier `TutorFlow.Web.exe` lock clears. Neither reflects a change made
in this phase; no source file needed modification to unblock the run.

```
=================== VERIFY SUMMARY ===================
dotnet restore                                PASS                              1.1s
dotnet build (0 warnings)                     PASS                              2.1s
Backend: dotnet test TutorFlow.sln (run 1 of 2) PASS                               24s
Backend: dotnet test TutorFlow.sln (run 2 of 2) PASS                               24s
Backend: Postgres integration tests           PASS                              5.3s
npm ci                                        PASS                            108.7s
Frontend: npm run lint                        PASS                             56.1s
Frontend: npm run build                       PASS                               18s
Frontend: npm test -- --run                   PASS                             87.6s
-------------------------------------------------------
Total elapsed: 327.1s

RESULT: PASS
```

Backend suite totals (both runs identical): Domain 67, Application 223,
Infrastructure 61 + 8 Postgres, Web 182 — all passed, 0 failed, 0
skipped. Frontend: 49 test files, 229 tests passed;
`tsc -b && vite build` and `eslint .` both clean.

## 7. Merge recommendation

**Ready to merge.** The defect Phase 5A's study found is fixed and proven
two ways: a Web-layer end-to-end test (cancel, then successfully rebook
the same slot) and a Postgres integration test against the real filtered
index. `DOMAIN_MODEL.md` Open Question 7 is now resolved in the
codebase, matching the owner's decision. No payments code was introduced.
No database row was modified or deleted — only an index definition
changed and a new nullable column was added, both backfill-free by
construction. `Session.Price` lays necessary Domain groundwork for a
future payments phase without anticipating one — it is inert today (no
endpoint, no frontend surface). Task 5's Reschedule finding (the
new-time window is currently unprotected by any slot-occupancy invariant)
is real and worth prioritizing, but requires a new ADR/owner decision on
what "rescheduling a slot" structurally means before any code is
written — flagged here, not fixed here, per this phase's own scope
boundary.
