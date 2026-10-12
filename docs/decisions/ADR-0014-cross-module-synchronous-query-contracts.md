# ADR-0014: Cross-Module Synchronous Query Contracts via Shared Interfaces

**Status:** Proposed — requires Architect sign-off before `Bookings` is scaffolded

## Context

`Bookings` must validate a requested slot against `Availability`'s weekly rules, time off, booking settings, and bookability — logic that already exists in `Availability`'s `AvailabilityCalculator` and must not be duplicated (explicit instruction: "do not duplicate Availability's calculation logic inside Bookings"). `Bookings` must similarly know the tutor's current price to snapshot it onto the booking.

`ADR-0001` states cross-module communication happens "exclusively through integration events" and forbids "direct project references between business modules." Taken completely literally, this would force `Availability`'s rule-evaluation logic to be duplicated inside `Bookings`, or force an eventually-consistent local read-model copy of *all* of `Availability`'s rules/time-off/settings data inside `Bookings` — both worse than the alternative below. `ADR-0003`, however, already anticipated this: it explicitly allows `Availability` to learn about `Bookings`' holds "via **a query** or consumed integration event" — i.e. the project's own already-accepted architecture treats synchronous queries as a legitimate alternative to events for *this class* of need, not just an event-only regime.

## Decision

Introduce a narrow, explicitly-scoped exception to "no direct project references between business modules": a **read-only query contract**, defined as an interface in `TutorFlow.Shared`, implemented by the *owning* module, and consumed via dependency injection by any module that needs the answer — never via a direct project reference to the owning module's assembly, and never for anything other than an idempotent read.

```csharp
// TutorFlow.Shared
public interface ITutorAvailabilityQuery
{
    Task<SlotValidationResult> ValidateSlotAsync(
        Guid tutorUserId, DateTimeOffset startUtc, DateTimeOffset endUtc, CancellationToken ct);
}

public sealed record SlotValidationResult(bool IsValid, string? Reason);

public interface ITutorPricingQuery
{
    Task<TutorPrice?> GetCurrentPriceAsync(Guid tutorUserId, int durationMinutes, CancellationToken ct);
}

public sealed record TutorPrice(int AmountMinorUnits, string Currency);
```

- `Availability` implements and registers `ITutorAvailabilityQuery` inside `AddAvailabilityModule`. `Tutors` implements and registers `ITutorPricingQuery` inside `AddTutorsModule`.
- `Bookings` references only `TutorFlow.Shared` for these interfaces — never `TutorFlow.Modules.Availability` or `TutorFlow.Modules.Tutors` directly. No project reference is added between business modules by this decision.
- The Host composition root (already the place where every module's services are registered) resolves the interface to whichever module's implementation is registered — an ordinary DI call, in-process, same transaction/request scope if needed, no network hop and no serialization overhead.

### Scope and limits — this pattern is **not** a general escape hatch

- **Read-only, idempotent queries only.** Never used for commands, writes, or anything with a side effect in the called module — those remain strictly event-driven (publish from the owning module, consume where needed), per `ADR-0001`.
- **The owning module always implements; the calling module never reaches into the owning module's internals, EF model, or database.** The contract (interface + DTOs) is the entire surface; nothing about `Availability`'s or `Tutors`' schema leaks across it.
- **`TutorFlow.Shared` still contains no business logic** — these interfaces are pure contracts (method signatures and DTOs), consistent with `CLAUDE.md`'s existing rule for `Shared`.
- This does not replace or weaken the event-driven pattern already accepted for `Tutors` → `Availability` bookability (`ADR-0011`) or for `Bookings` → `Lessons`/`Payments` (`ADR-0015`, `ADR-0016`) — those remain eventual-consistency, event-based integrations. This ADR is specifically for the subset of cross-module needs that are synchronous reads required *before* a write can even be attempted (e.g. "is this slot valid" must be answered before a hold can be inserted; it cannot wait for eventual consistency).

## Correction (architecture review, 2026-10-04): a reciprocal contract is required for `Availability`'s browse-slots endpoint

`ADR-0003` (Accepted) already requires: *"`Availability` must treat active holds owned by `Bookings` as unavailable... via a query or consumed integration event from `Bookings`."* The query contract decided above only wires one direction — `Bookings` asking `Availability` "is this slot valid." It does not, by itself, give `Availability` any way to exclude `Bookings`' active holds when a student *browses* a tutor's open slots (the new public endpoint required by `BOOKINGS_SPEC.md` §5). Without this, that endpoint could show a slot as open that `Bookings` already holds — not a safety violation (the exclusion constraint, `ADR-0013`, still rejects the resulting hold attempt), but a direct shortfall against `ADR-0003`'s own requirement, and worse UX than necessary.

The smallest fix is the same pattern in the opposite direction — a second, symmetric query contract, not a new abstraction, not an event, not Wolverine:

```csharp
// TutorFlow.Shared
public interface IActiveBookingIntervalsQuery
{
    Task<IReadOnlyList<BookingInterval>> GetActiveIntervalsAsync(
        Guid tutorUserId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken ct);
}

public sealed record BookingInterval(DateTimeOffset StartUtc, DateTimeOffset EndUtc);
```

- **Owned and implemented by `Bookings`** (it owns this data), registered inside `AddBookingsModule`, consumed via DI by `Availability`'s new public slot-browsing endpoint only — `Availability`'s existing self-service `/me/slots` endpoint is unaffected by this ADR (out of scope; it is pre-existing, shipped behavior).
- The implementation queries `Bookings`' own table for rows with `TutorUserId = tutorUserId`, `Status IN ('Held', 'PendingPayment', 'Confirmed')` (the same protected set as `ADR-0013` §1, kept in sync by construction since both live in the same module), and `during && tstzrange(fromUtc, toUtc)` — returning plain `(StartUtc, EndUtc)` pairs, nothing else. No `BookingId`, no `StudentUserId`, no status — `Availability` only needs "this window is occupied," never who occupies it.
- `Availability`'s new browse-slots endpoint passes the result straight into `AvailabilityCalculator.ComputeOpenSlotStarts`'s existing `occupiedIntervals` parameter (`AvailabilityCalculator.cs`'s `OccupiedInterval` record — already present and already documented as waiting for exactly this integration). No change to the calculator itself is required beyond what `ADR-0014`'s original generalized-duration requirement (`BOOKINGS_SPEC.md` §5) already calls for.
- This is a read-only, idempotent query, subject to every scope limit already stated above (no commands, no side effects, no schema leakage) — it is the same pattern applied in the reverse direction, not a new kind of coupling.
- This remains advisory for the *browse* case exactly as `ITutorAvailabilityQuery` is advisory for the *hold-creation* case (`## Decision` above): it can be microseconds stale by the time a student actually attempts a hold. The exclusion constraint remains the only authoritative, final concurrency guarantee. This contract exists to make the browse experience accurate, not to replace the constraint.

### Fail-closed default for missing `TutorBookability` records

`Availability`'s `ITutorAvailabilityQuery` implementation checks `TutorBookabilityRecords` (`ADR-0011`) to answer "is this tutor currently bookable." That table is populated only by consuming `Tutors`' publish-on-`Published` event — a pipeline that does not exist yet (no consumer is wired; `TutorBookabilityProjector` exists but nothing calls it). Until it does, **no row exists for any tutor, including genuinely published ones.**

**Decision: absence of a `TutorBookability` record is treated as "not bookable."** `ValidateSlotAsync` must return `SlotValidationResult(IsValid: false, Reason: "tutor not bookable")` when no record is found for the requested `tutorUserId` — it must never default to `true` (treating "unknown" as "bookable") just because a row happens to be missing. This fails closed: real bookings cannot be created for any tutor until `ADR-0011`'s event pipeline is actually implemented and has run at least once for that tutor, which is the correct, safe behavior for a module that cannot yet distinguish "this tutor isn't published" from "the pipeline hasn't told me yet." See `BOOKINGS_SPEC.md` §5 and `docs/domain/DOMAIN_MODEL.md`'s `Availability` row, both updated to name `ADR-0011`'s pipeline as a required `Bookings` prerequisite, not merely the two already-named `AvailabilityCalculator`/public-endpoint prerequisites.

This decision does not implement Wolverine or any event infrastructure — it only documents the required contract and its safe default, exactly as the rest of this ADR documents contracts for modules (`Payments`, `Lessons`) that don't exist yet either.

## Consequences

- `Bookings.csproj` gains no new `ProjectReference` beyond `TutorFlow.Shared` (already the universal baseline for every module). `Availability.csproj` and `Tutors.csproj` likewise gain no new cross-module references — they only add an interface implementation registered against a `TutorFlow.Shared` contract. This now also holds for `IActiveBookingIntervalsQuery` in the opposite direction: `Bookings` implements it, `Availability` consumes it, still zero new project references.
- `Availability`'s `ITutorAvailabilityQuery` implementation requires generalizing `AvailabilityCalculator` to validate an arbitrary requested `(startUtc, durationMinutes)` pair, not only to enumerate slots at the tutor's own configured `DefaultSlotDurationMinutes`. This is a required extension to `Availability`, not a `Bookings`-side duplication — see `BOOKINGS_SPEC.md` §5 for the exact shape required.
- This establishes a reusable precedent: any future module facing the same "I need a real-time answer from another module's current data, not an eventually-consistent copy" need should use this same pattern rather than inventing a bespoke one.
- Resolves how `Bookings` satisfies "do not duplicate Availability's calculation logic" and "where the authoritative final availability check occurs" (see `BOOKINGS_SPEC.md` §5): the query contract answers "is this slot valid for this tutor's rules" (business-rule authority, delegated to `Availability`); the PostgreSQL exclusion constraint (`ADR-0013`) answers "is this specific interval currently free of other holds" (concurrency authority, owned by `Bookings`). Neither alone is sufficient; together they are.
