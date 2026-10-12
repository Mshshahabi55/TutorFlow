# ADR-0011: Tutor Identity Reference & Publication Event Contract

**Status:** Accepted

## Context

The architecture review conducted before `Availability` implementation (see `docs/OPEN_QUESTIONS.md`) identified two unresolved integration points between `Tutors` and every module that needs to reference "a tutor":

1. `TutorApplication` has its own primary key (`Id`, generated at draft creation) **and** a separate `UserId` (the stable Identity reference). Neither `docs/domain/DOMAIN_MODEL.md` nor any ADR specified which one external modules should key off — a real risk for `Availability`, `Bookings`, and `Search`, all of which need to reference a tutor.
2. No mechanism exists for `Availability` (or eventually `Bookings`/`Search`) to learn that a tutor is `Published`/bookable without reaching into `Tutors`' database directly, which `ADR-0001` already forbids.

## Decision

### 1. Tutor identity reference

`Availability`, `Bookings`, `Search`, and any other module referencing a tutor use the **Identity `UserId` (`Guid`)** as the stable cross-module tutor identifier.

- `TutorApplication.Id` remains **internal to `Tutors`** — it is never referenced, stored, or exposed as "the tutor ID" by any other module's schema or API.
- This is not a new rule — it is a direct instance of the stable-ID/no-cross-module-FK principle already established in `ADR-0001`. `UserId` is already the one stable identifier every module uses to reference a person; `Tutors` does not introduce a second, competing identity for the same person.

### 2. Published/bookable signal

`Tutors` owns the `TutorApplication.Status` lifecycle (see `docs/domain/STATE_MACHINES.md`) and is the sole authority on when a tutor becomes `Published`.

- When that transition happens, `Tutors` will publish an integration event carrying, at minimum, the tutor's `UserId` and publication status. The exact event name and full payload shape are **not decided by this ADR** — they are finalized when the event is actually implemented, by whichever module needs it first.
- `Availability` consumes this event into its **own, locally-owned read model** of "which `UserId`s are currently bookable." It does not query `Tutors`' database and does not hold a foreign key into `Tutors`' schema.
- This establishes the event-driven integration pattern `ADR-0001` already requires in concrete terms, and is the template future `Search`/`Bookings` integrations with `Tutors` for the same signal should follow.

## Consequences

- `Availability`'s (and later `Bookings`'/`Search`'s) schema design uses `UserId` (`Guid`) as the tutor-reference column — never `TutorApplication.Id` — avoiding a rename/migration later.
- No messaging infrastructure (Wolverine or otherwise) is added by this decision. Per `CLAUDE.md`'s existing Tech Stack guidance, Wolverine is introduced when a cross-module integration event is actually implemented — this ADR defines the *contract shape and direction*, not the *implementation timing*. Whichever module first needs to consume this event (most likely `Availability`) is responsible for introducing messaging infrastructure at that time, not before.
- No concrete integration event is added to `TutorFlow.Shared` by this decision — only the direction (`Tutors` publishes, `Availability` consumes into its own read model) is settled.
- No change to the already-implemented `Tutors` module code is required; `TutorApplication.Id` stays exactly as implemented.
- Resolves the "Tutor identity reference" and "Published/bookable signal" risks flagged in the pre-`Availability` architecture review.
