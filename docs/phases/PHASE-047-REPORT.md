# Phase 4.7 Report — Reschedule Targets a Slot

Branch: `develop` | Commits this phase: `e4d2905` (Task 1 — failing
test), `9bcc79b` (Task 2 — Domain), `b27b627` (Task 3 — Application +
contract), `7b1b678` (Task 4 — Postgres proof), `44df055` (Task 5 —
frontend). This report is its own commit, no further code change.

**Status: complete, 9/9 `scripts/verify.ps1` green.** Closes the
double-booking defect Phase 4.6's study found: `RescheduleSessionCommand`
took a raw `DateTime` and touched no `AvailabilitySlot` at all, so a
rescheduled Session's new time was protected by no occupancy invariant,
and its original slot stayed permanently `IsConsumed` describing a time
the Session no longer occupied. Rescheduling is now cancel-and-rebook of
the same Session: it targets an existing, open `AvailabilitySlot` by id;
the old slot reopens (`AvailabilitySlot.Reopen()`, reused unchanged from
Phase 4.6); the new slot consumes (`AvailabilitySlot.Consume()`, new this
phase). No payments code was touched.

## 1. Clean Architecture validation

Domain gained `AvailabilitySlot.Consume()` and widened
`Session.Reschedule`'s signature; both still depend on nothing outside
`Domain.Common`/`Domain.Scheduling.ValueObjects`. `Session.AvailabilitySlotId`/
`Duration`/`DeliveryMode` gained private setters (previously `{ get; }`)
— the only Domain-layer API-shape change, and it stays `private set`, so
no caller outside `Session` itself can mutate them directly.
`RescheduleSessionCommandHandler` gained a dependency on
`IAvailabilitySlotRepository` — the same Domain-defined interface
`CancelSessionCommandHandler`/`BookSessionCommandHandler` already depend
on, not a new one. No new package was added. Frontend:
`fetchTutorAvailabilitySlots` wraps an endpoint that already existed
(`GET /tutors/{id}/availability-slots`) — no new backend capability, a
missing frontend wrapper filled in. `dotnet build TutorFlow.sln`: **0
Warnings, 0 Errors** (verified below, `scripts/verify.ps1`).

## 2. Three-aggregate transaction atomicity (the question this phase's brief asked to reason through explicitly)

`RescheduleSessionCommandHandler.Handle` mutates and saves **three**
aggregates in one `SaveChangesAsync` call: `session`, `oldSlot`,
`newSlot`. This is wider than `ADR-015`'s "one aggregate per transaction"
default, but not a new departure from it — it is the union of two
transactions that default already anticipates individually:

- `BookSessionCommandHandler` already saves two aggregates in one call:
  `slot` (mutated: `IsConsumed = true`) and the brand-new `session`.
- `CancelSessionCommandHandler` already saves two aggregates in one
  call: `session` (mutated: `Status = Cancelled`) and `slot` (mutated:
  `IsConsumed = false` via `Reopen`).

Reschedule is structurally both of those combined into one atomic
operation on the *same* Session: release the old slot (Cancel's shape)
and consume a new one (Book's shape) without ever letting the Session
exist in a "between" state that's visible to another request. `ADR-004`'s
Transaction Boundaries section already names this exact shape as
anticipated, not new territory: it flagged that cancellation's "full
transactional shape... is not yet complete" back when only Question 7
(slot reopening) was resolved, and reschedule is the next compound
operation in the same family. Concretely, in
`RescheduleSessionCommandHandler`:

```csharp
session.Reschedule(newSlot.Id, newSlot.TutorId, newSlot.StartTimeUtc, newSlot.Duration, newSlot.DeliveryMode);
newSlot.Consume();
// ... (only reached if both succeeded)
oldSlot.Reopen(session.Id);
await _unitOfWork.SaveChangesAsync(new IAggregateRoot[] { session, oldSlot, newSlot }, cancellationToken);
```

No partial persistence is possible: all three in-memory mutations happen
before the single `SaveChangesAsync` call, and if either
`session.Reschedule(...)` or `newSlot.Consume()` throws (wrong Tutor,
already-consumed slot, wrong Session status), the method returns a
`Result.Failure` before `oldSlot.Reopen(...)` is even called and before
any `SaveChangesAsync` — so a rejected reschedule leaves every aggregate,
in memory and in the database, exactly as it was. A concurrent race (two
requests both trying to consume the same `newSlot`) is caught the same
way `BookSessionCommandHandler` already catches one:
`ConcurrencyConflictException` from the storage-layer unique index,
translated to `RescheduleSessionCommand.SlotAlreadyBooked` — proven for
real in Task 4 below, not just asserted.

**This did not need to be stopped and escalated**, because it is not a
new invariant or a new cross-aggregate rule — it is the same two
already-shipped two-aggregate transactions, applied to the same Session
in sequence, inside one `SaveChangesAsync`. No new `IUnitOfWork` capability
was needed; `SaveChangesAsync(IEnumerable<IAggregateRoot>)` already
accepted an arbitrary set.

## 3. Task 1 — the failing test, before and after

Mirrors Phase 4.6's `CancelSession_reopens_the_slot_so_it_can_be_rebooked`
exactly, but for Reschedule: `RescheduleSession_reopens_the_old_slot_so_
it_can_be_rebooked` books a Session, reschedules it (via the *old*,
still-unchanged raw-timestamp endpoint at the time this test was
written), then attempts to rebook the same original slot as a different
student. Run against pre-fix code, this failed:

```
System.Net.Http.HttpRequestException : Response status code does not
indicate success: 409 (Conflict).
```

— the exact rejection Phase 4.6 observed for Cancel pre-fix:
`AvailabilitySlot.Book()`'s own in-memory `if (IsConsumed) throw` guard,
proving the original slot never released. Task 3 later updated this same
test's request body to the new `NewAvailabilitySlotId` contract (the
raw-timestamp endpoint no longer exists) — its assertions (rebooking the
vacated slot must succeed) are otherwise unchanged, and it now passes.

## 4. Task 2 — Domain (`Session.Reschedule`, `AvailabilitySlot.Consume`)

`Session.Reschedule` now takes the target slot's own fields
(`AvailabilitySlotId`, `TutorId`, `DateTime`, `SessionDuration`,
`DeliveryMode`) rather than a raw timestamp — the same pattern
`Session.Book(...)` already uses (fields, never a reference to
`AvailabilitySlot` itself; Session never holds a reference to another
aggregate, DOMAIN_MODEL.md: Relationships). Guards, each stated
explicitly per the brief's request:

- **Only a Scheduled Session can be rescheduled** — mirrors
  Cancel/Complete/MarkNoShow's own single-transition guard.
- **The new slot must belong to the same Tutor** — a Session belongs to
  one Tutor for its whole lifetime; enforced inside `Session.Reschedule`
  itself (not left to the Application layer alone) by comparing the
  passed-in `TutorId` against `this.TutorId`, so the invariant holds even
  if a future caller forgets to check it.
- **The new slot must differ from the Session's current slot** —
  otherwise the operation is a no-op dressed up as a state change.
- **The new slot must not already be consumed** — enforced by the new
  `AvailabilitySlot.Consume()`, the "book the new slot" half of the
  operation split out from `Book()` (which both validates and constructs
  a brand-new Session — wrong here, since the Session already exists).
- **Deliberately not guarded**: `newScheduledTimeUtc` already in the
  past. `Book()`/`Reopen()` never guard this either — no minimum/maximum
  lead time is established (`DOMAIN_MODEL.md` Open Question 8, still
  open) — so adding an asymmetric guard here would be inventing a new
  business rule this phase is not authorized to decide.

**`Session.Price` is deliberately left untouched.** Recommendation and
reasoning, as the brief asked: Phase 4.6 fixed `Session.Price` at the
Tutor's `HourlyRate` **as it stood at original booking time**, explicitly
"immune to the Tutor later changing their rate." Rescheduling is not a
new booking — it is the same commercial commitment, moved to a different
time/slot. Re-deriving the price from the new slot (which itself carries
no price — only Tutors have a rate, per `AvailabilitySlot.Book()`'s own
comment) would either require a second Tutor-rate lookup at reschedule
time (undoing Phase 4.6's "fixed forever after" invariant — a Tutor could
raise their rate between booking and reschedule and have it silently
retroactively apply) or require inventing a proration/rounding rule
neither `DOMAIN_MODEL.md` nor an Accepted ADR settles. Preserving `Price`
unchanged is the only option that invents nothing.

**Duration/DeliveryMode update to the new slot's own values.** Not
explicitly named in the brief's guard list, but a necessary consequence
of "rescheduling is structurally identical to booking": a fresh booking
on the new slot would inherit exactly these two fields from it
(`AvailabilitySlot.Book()`'s own call to `Session.Book(...)`), and leaving
them stale would silently reintroduce the same category of drift bug
this phase closes (a Session whose `Duration`/`DeliveryMode` describe a
slot it no longer occupies). This required adding private setters for
both, previously `{ get; }`.

**Events and audit.** `SessionRescheduled` gained
`OldAvailabilitySlotId`/`NewAvailabilitySlotId` alongside its existing
`NewScheduledTimeUtc` — both slot ids in one audit-relevant event, so the
audit record captures which slot was vacated and which was newly
occupied, not only when. `AuditDomainEventHandler`'s allowlist switch
needed **no change**: it already matches `SessionRescheduled e =>
((Guid?)e.SessionId.Value, ...)`, keyed by `SessionId`, unaffected by the
record gaining fields. `AvailabilitySlotReopened` (already audited) is
reused unchanged for the old slot. `AvailabilitySlot.Consume()` raises no
new event, mirroring `Book()` itself — `SessionRescheduled` already
carries `NewAvailabilitySlotId`, so a separate `AvailabilitySlotConsumed`
event would duplicate the same audit fact.

## 5. Task 3 (Application/contract) and Task 4 (Postgres proof)

**Contract.** `RescheduleSessionCommand(Guid SessionId, Guid
NewAvailabilitySlotId)` replaces `NewScheduledTimeUtc`; the Web request
DTO (`RescheduleSessionRequest`) mirrors it. `AUTHORIZATION_MATRIX.md`
needed **no change** — its `/sessions/{id}/reschedule` row documents *who*
may call the endpoint (any party to the Session, or Admin), which this
phase does not touch; only the request body's target field changed.
`RescheduleSessionCommandHandler` now loads both the old slot (to
reopen) and the new slot (to consume) via `IAvailabilitySlotRepository`,
the same interface `CancelSessionCommandHandler`/`BookSessionCommandHandler`
already use. `RescheduleSessionCommandHandlerTests` was rewritten for the
two-repository shape: reopens old + consumes new; rejects a
different-Tutor's slot (`InvalidState`); rejects an already-consumed slot
(`InvalidState`); rejects an unknown target slot (`AvailabilitySlotNotFound`).

**Postgres proof (Task 4).**
`RescheduleSession_reopens_the_old_slot_and_consumes_the_new_slot_against_real_Postgres`
proves all three aggregates persist correctly when re-read from a fresh
`DbContext` scope (per CLAUDE.md's rule that `SaveChangesAsync` being
called is not evidence anything was saved), and proves the old slot is
genuinely rebookable end to end, not merely flagged
`IsConsumed == false`.
`Sessions_AvailabilitySlotId_unique_index_rejects_a_second_session_on_the_rescheduled_slot`
proves the storage-layer unique index still rejects a concurrent
double-booking of the exact slot a reschedule just consumed, via the same
raw-SQL-bypassing-the-Domain-guard technique
`Sessions_AvailabilitySlotId_unique_index_rejects_a_genuine_concurrent_double_booking`
already established. Both pass against `tutorflow_test` (real
PostgreSQL, not SQLite).

## 6. Task 5 (frontend) — and a real authorization gap it surfaced

`RescheduleSessionForm` (`SessionDetailPage.tsx`) now fetches the target
Tutor's open Availability Slots (`fetchTutorAvailabilitySlots`, a new
frontend wrapper around the already-existing `GET
/tutors/{id}/availability-slots`) and renders a `FormSelect` of open
slots — excluding the Session's current slot and any already-consumed
slot client-side — instead of a raw Tehran `datetime-local` field. No
design-system styling; plain `FormSelect`, per this task's own scope.
`rescheduleSessionSchema`/`useRescheduleSession`/`rescheduleSession`
updated to carry a slot id. Tests updated, not deleted, across
`schedulingService.test.ts` and `SessionDetailPage.test.tsx` (now
exercises selecting an option and confirms the current slot is excluded
from it).

**Verified against the real backend**, not just unit tests
(`dotnet run` + real Postgres + curl, since no browser-automation tool is
available in this environment): registered a Tutor/Student, booked a
Session on slot A, called `POST /sessions/{id}/reschedule` with
`{"NewAvailabilitySlotId": "<slotB>"}` as the Student, and confirmed via
fresh `GET`s that the Session moved to slot B's own time, slot A became
`isConsumed: false` (rebookable), and slot B became `isConsumed: true` —
the real HTTP path, not a mock.

That same live check surfaced a genuine, pre-existing authorization gap
this task does **not** fix: `GET /tutors/{id}/availability-slots` (the
picker's own data source) is restricted by `AUTHORIZATION_MATRIX.md`'s
Third Addendum Decision 13 to "any authenticated caller when the Tutor is
discoverable; the named Tutor or Admin only when not discoverable." A
newly-registered Tutor defaults to `IsDiscoverable: false`, and nothing
requires a Tutor be discoverable to have a Session booked against them
(a Session is reachable by direct slot-id sharing, per this codebase's
existing "no browse" design). So: **a Student or Parent/Guardian
rescheduling their own Session — a caller `RescheduleSession` itself
equally authorizes — gets `403 Forbidden` loading the picker whenever the
Tutor isn't discoverable.** Confirmed directly: the same GET with the
Student's own token returned `403
GetTutorAvailabilitySlotsQuery.Forbidden`; the identical GET with the
Tutor's own token returned `200` with the slot list. The frontend
degrades honestly (`ErrorState` shows the backend's own message plus a
Retry button — no crash, no blank picker), but the picker itself cannot
load for that combination. This is a new-business-rule question (should
a Session's existing parties bypass discoverability for this one read?)
squarely outside this task's authorization — per CLAUDE.md's stop
conditions, flagged here for the owner, not decided or fixed by this
phase.

## 7. `scripts/verify.ps1`, real output, and merge recommendation

Two unrelated environment locks were hit and cleared before the green
run below — a leftover `TutorFlow.Web.exe` and two leftover Vite dev-
server processes (`node.exe`, one bound to port 5173) from this phase's
own live-HTTP verification step in Section 6, killed via
`Stop-Process` — the same category of stateless dev-process lock Phase
4.6 already hit and cleared, not a defect in any code this phase
changed.

```
=================== VERIFY SUMMARY ===================
dotnet restore                                PASS                              1.2s
dotnet build (0 warnings)                     PASS                              2.6s
Backend: dotnet test TutorFlow.sln (run 1 of 2) PASS                            26.2s
Backend: dotnet test TutorFlow.sln (run 2 of 2) PASS                            24.3s
Backend: Postgres integration tests           PASS                              7.3s
npm ci                                        PASS                             38.2s
Frontend: npm run lint                        PASS                               18s
Frontend: npm run build                       PASS                             19.8s
Frontend: npm test -- --run                   PASS                             79.4s
-------------------------------------------------------
Total elapsed: 217.4s

RESULT: PASS
```

Backend totals: Domain 76, Application 227, Infrastructure 61 + 10
Postgres, Web 183 — all passed, both determinism-guard runs identical, 0
failed, 0 skipped. Frontend: 50 test files, 233 tests passed; `tsc -b &&
vite build` and `eslint .` both clean.

**Ready to merge**, with one flagged, unfixed gap: the reschedule
picker's own data source (`GET /tutors/{id}/availability-slots`) 403s for
a Student/Parent caller when the Tutor is not `IsDiscoverable` — a real,
ordinary-case authorization gap this phase's live testing found and
documents (§6), not a Domain/Application/Web defect in what this phase
built. `DOMAIN_MODEL.md` Open Question 7's mechanism (cancel-and-rebook)
is now applied symmetrically to both Cancel and Reschedule. No payments
code was introduced. No database row was modified or deleted outside the
normal reschedule flow itself — no migration was needed this phase (no
new column or index; `AvailabilitySlot.Consume()` and `Session`'s new
private setters are in-memory-only changes to already-existing columns).
