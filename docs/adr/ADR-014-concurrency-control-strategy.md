**Authoritative sources (in priority order):** `PROJECT_CONSTITUTION.md`, `PRODUCT_REQUIREMENTS.md`, `BUSINESS_MODEL.md`, `DOMAIN_MODEL.md`, `ARCHITECTURE.md`, `docs/adr/ADR-001` through `ADR-013` (all approved and immutable, except `ADR-003`, `ADR-011`, `ADR-012`, and `ADR-013`, which are themselves Proposed), `docs/database/DOMAIN_DATA_MODEL.md`, `docs/database/PHYSICAL_DATABASE_STRATEGY.md`. This ADR chooses no specific locking primitive, no specific database feature, and no product. It exists to resolve `ARCHITECTURE.md` Section 21, Item 3 only.

---

# ADR-014: Concurrency Control Strategy

## Status

**Accepted — 2026-07-20.**

**Business decision:** unique-constraint-based enforcement. A database-level uniqueness constraint on the Session-to-Availability-Slot reference rejects a duplicate booking at the storage layer, independent of the in-memory check-then-act timing.

## Context

`ADR-004`'s Concurrency Strategy section already fixes the *requirement*: "A concurrent attempt to book an already-consumed Availability Slot must be resolved so that at most one attempt succeeds and the other is rejected explicitly and visibly... never resolved by allowing both to partially succeed or by silently overwriting one outcome." It explicitly defers the *mechanism*. The Architecture Assessment Report demonstrated, in the currently-running code, exactly the failure mode this requirement warns against: two concurrent `AvailabilitySlot.Book()` calls against the same in-memory object reference can both succeed, because `IsConsumed` is checked-then-set with no synchronization — the single highest-severity finding of that review.

## Problem Statement

Given CONST-1's zero-tolerance requirement and `ADR-004`'s already-fixed qualitative requirement, what concurrency-control mechanism should enforce it — without assuming which persistence technology (`ADR-013`) it runs against, and without assuming the aggregate/transaction boundary these operations run within (`ADR-015`)?

## Dependencies on Other Open Decisions

- **Depends on `ADR-015` (Transaction Boundary Confirmation) directly.** The mechanism's scope — exactly what is being protected against concurrent access — is defined by that boundary. This ADR does not assume `ADR-015`'s outcome.
- **Parallel decision alongside `ADR-013` (Persistence Technology), both gated by `ADR-015`.** This is not drawn as a strict blocking dependency on `ADR-013` in the batch's final dependency graph, but several candidates below (in particular, a unique-constraint-based approach) are most confidently evaluated once `ADR-013`'s outcome is known — deciding the two together, or `ADR-013` first, remains the practical recommendation even though the formal graph treats them as siblings under `ADR-015`.
- **Downstream dependent: `ADR-016` (Audit Durability Strategy).** Whether the booking transaction this ADR governs may be internally retried due to a concurrency conflict (for example, under an optimistic-concurrency mechanism) affects what "the transaction" means for `ADR-016`'s same-transaction audit-durability candidate — `ADR-016` cannot assume a single, non-retried write without knowing this ADR's outcome.

## Constraints

- Must guarantee at most one success among concurrent attempts against the same Availability Slot (CONST-1).
- The rejected attempt's outcome must be explicit and visible (`PROJECT_CONSTITUTION.md` Engineering Principle 4) — never silent, never a partial application.
- Must be classifiable as a Domain-Error-equivalent outcome, not an Infrastructure Failure, per `ADR-008`'s existing classification (already decided; this ADR does not revisit it, only selects the mechanism that produces that outcome).
- Must not span two bounded contexts' owned aggregates in one mechanism or transaction (`ADR-004`: Transaction Boundaries).

## Alternatives Considered

Unranked category-level candidates:

- **Optimistic concurrency** — a version/row-version/ETag check-and-increment at write time; the attempt proceeds without locking and is rejected at commit if the underlying record changed since it was read.
- **Pessimistic locking** — a database-level row lock held for the duration of the booking transaction; a second concurrent attempt blocks or fails immediately rather than racing to a rejected commit.
- **Unique-constraint-based** — a uniqueness constraint expressing "one active Session per Availability Slot," letting the persistence engine itself reject a duplicate at the storage layer, independent of read-then-write race timing.
- **Application-level serialization** — funneling all booking attempts for a given slot through a single in-process synchronization point (e.g., a lock keyed by Availability Slot id); does not depend on the underlying store's concurrency primitives, but only protects against races within a single process/instance.

## Trade-offs

- **Optimistic concurrency:** no lock contention under normal load; requires explicit retry-or-fail handling at the Application layer, and — as already flagged in the Architecture Execution Review — introduces a new user-facing behavior: a legitimate concurrent booking attempt could receive a transient rejection requiring retry, where today's (broken) implementation simply "succeeds" incorrectly.
- **Pessimistic locking:** simpler reasoning ("only one attempt proceeds at a time" for a given slot); introduces lock-hold-duration and contention considerations, and requires care to avoid deadlock if a transaction ever needs to lock more than one resource.
- **Unique constraint:** pushes enforcement to the persistence engine itself — arguably the strongest physical guarantee, since it doesn't depend on correctly implementing check-then-act logic in Application/Infrastructure code; requires the technology chosen in `ADR-013` to support the specific constraint construct needed.
- **Application-level serialization:** zero persistence-technology dependency, but only correct within a single-process deployment — would silently stop being sufficient the moment the Modular Monolith runs as more than one instance, a possibility not ruled out by any approved document.

## Risks

- Any mechanism chosen without confirming it composes correctly with `ADR-013` and `ADR-015`'s eventual outcomes risks a mismatch discovered only during implementation.
- Choosing application-level serialization alone, with no persistence-layer backstop, risks CONST-1 being violated the moment the deployment topology changes from a single instance.
- Under-specifying retry behavior for a rejected concurrent attempt leaves a caller-facing gap: `ADR-008` classifies the rejection as Domain-Error-equivalent, but no approved document decides whether the Application layer ever automatically retries versus always surfacing the rejection.

## Impact on Existing Architecture

Concentrated entirely within `AvailabilitySlot.Book(...)`'s persistence path. The Domain method's own `IsConsumed` check-then-set logic remains as the logical safeguard; this decision adds the physical enforcement backstop at the Infrastructure/persistence layer.

## Impact on Clean Architecture

None — Domain's own invariant check already exists and is logically correct. This decision concerns only the physical mechanism that makes it hold true under concurrency, properly an Infrastructure-layer concern per `ADR-004`.

## Impact on DDD

Directly informs, but does not change, the Availability Slot aggregate's role as sole owner of "never bookable twice" (`DOMAIN_MODEL.md`: Aggregates).

**Open DDD tension, not a settled detail:** depending on the mechanism chosen — most notably optimistic concurrency — the aggregate may need to expose a concurrency token (e.g., a version) as part of its persisted state. This is flagged explicitly as an unresolved tension against `ADR-004`'s own principle that "Domain has zero dependency on Infrastructure" (`ADR-004`, Sections 7–8 as restated there): a persistence-originated concurrency token sitting on a Domain aggregate root is a widely-used pattern in practice, but it *is* a boundary-crossing concern in a strict Clean Architecture reading, not a neutral implementation detail. Whichever mechanism is eventually chosen, this tension must be explicitly acknowledged at acceptance time — not silently accepted as routine — and `ADR-015` does not resolve it either, since it concerns the concurrency mechanism's shape, not the aggregate boundary itself.

## Impact on Testing

Requires a genuine concurrent-load test — already recommended, currently missing — proving at most one of N simultaneous booking attempts against the same slot succeeds. The test's design depends on the mechanism: a lock-based approach and an optimistic-retry approach are verified differently (blocking-and-serializing vs. reject-and-observe).

## Recommendation

No mechanism is recommended or ranked. Procedurally: resolve `ADR-015` first, since this mechanism's scope is defined by that boundary, then resolve this ADR together with, or immediately after, `ADR-013`, even though the two are formally parallel siblings under `ADR-015` rather than sequentially dependent on each other.

## Assumptions

The Modular Monolith (`ADR-001`) remains a single logical deployment. Whether it ever runs as more than one process instance is not established by any approved document and is treated here as an open operational question, flagged under Risks rather than assumed away.

## Non-Goals

Does not decide the persistence technology (`ADR-013`). Does not decide whether or how a rejected concurrent attempt is retried by the Application layer — a related but distinct, still-undecided question. Does not address concurrency for any resource other than Availability Slot/Session; no other aggregate in this domain carries a documented concurrent-write correctness requirement as strict as CONST-1.

**Disambiguation from `ADR-012`'s retry semantics:** this ADR's use of "retry" refers exclusively to whether and how a *rejected concurrent booking attempt* might be retried by the Application layer. This is a distinct concept from `ADR-012`'s Required Decision 3, which concerns whether a *failed Domain Event Dispatch listener invocation* is retried. The two share vocabulary but not subject matter; resolving one does not resolve, constrain, or inform the other, and neither is decided by any approved document.

## Questions Requiring Approval

1. Which category (optimistic / pessimistic / unique-constraint / application-level / a combination) should be selected?
2. Should the Modular Monolith be assumed single-instance for the purposes of this decision, or must the mechanism hold even under multiple instances?
3. Should a rejected concurrent booking attempt ever be automatically retried by the Application layer, or always surfaced to the caller as an explicit failure?

---

*Status: Accepted — 2026-07-20. Mechanism: unique constraint.*
