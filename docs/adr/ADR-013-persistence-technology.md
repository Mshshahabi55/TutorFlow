**Authoritative sources (in priority order):** `PROJECT_CONSTITUTION.md`, `PRODUCT_REQUIREMENTS.md`, `BUSINESS_MODEL.md`, `DOMAIN_MODEL.md`, `ARCHITECTURE.md`, `docs/adr/ADR-001` through `ADR-012` (all approved and immutable, except `ADR-003`, `ADR-011`, and `ADR-012`, which are themselves Proposed), `docs/database/DOMAIN_DATA_MODEL.md`, `docs/database/PHYSICAL_DATABASE_STRATEGY.md`. This ADR chooses no vendor, product, edition, ORM, or data-access library. It exists to resolve `ARCHITECTURE.md` Section 21, Item 2 only. Where a candidate technology is named below, it is named as an illustrative example of a category, not a selection — per this phase's explicit instruction, no technology is assumed chosen by virtue of appearing as an example.

---

# ADR-013: Persistence Technology

## Status

**Accepted — 2026-07-20.**

**Business decision:** Relational/SQL, ACID — PostgreSQL. Single-writer with optional read replicas, consistent with the Constraints section below.

## Context

`ARCHITECTURE.md` Section 10 already fixes the qualitative category: "a strongly consistent, transactional (ACID) datastore is recommended as a category... the specific product is not chosen here." `ADR-004` formalizes this further: persistence must be "a strongly consistent, transactional mechanism sufficient to enforce CONST-1 and CONST-3 as hard invariants." The system currently runs on temporary, explicitly-flagged in-memory repositories. The Architecture Assessment Report (an earlier phase of this engagement) verified a reproducible concurrency defect directly traceable to the absence of a real transactional store — the single highest-severity finding of that review.

## Problem Statement

Given the qualitative category already fixed by `ARCHITECTURE.md` and `ADR-004` (strongly consistent, ACID-transactional), which specific persistence technology should TutorFlow adopt to durably store Tutor, Availability Slot, Session, Relationship, and Account data and enforce CONST-1 and CONST-3 as hard invariants — without deciding the concurrency-control technique (`ADR-014`) or any ORM/data-access-library choice?

## Dependencies on Other Open Decisions

- **Depends on `ADR-015` (Transaction Boundary Confirmation).** `ADR-015` must be resolved first: the aggregate boundary it ratifies or revises directly shapes how naturally a document-oriented technology's "one aggregate ≈ one document" trade-off applies, and confirms the exact scope of the atomic transaction this technology must support. This ADR is **not** independent of `ADR-015` — an earlier draft of this document claimed otherwise; that claim is withdrawn here per Enterprise Architecture Review Board finding.
- **Downstream dependent: `ADR-016` (Audit Durability Strategy)**, which depends on this ADR for what "same transaction" is physically capable of meaning.
- **Parallel decision: `ADR-014` (Concurrency Control Strategy)** is resolved alongside this ADR, both gated by `ADR-015` rather than sequenced strictly after this one — but `ADR-014`'s specific candidate menu (in particular, a unique-constraint-based mechanism) is most confidently evaluated once this ADR's outcome is known, so deciding them in the same sitting is still recommended.

## Constraints

- Must be strongly consistent / ACID-transactional (`ARCHITECTURE.md` Section 10; `ADR-004`).
- Must support atomic, all-or-nothing multi-write transactions within a single bounded context's owned aggregates (`ADR-004`: Transaction Boundaries) — specifically the check-and-consume-slot-and-create-session sequence.
- Must not require or imply a specific concurrency-control mechanism — that is `ADR-014`'s decision, not this one's.
- Must fit the Modular Monolith / single-deployable topology already fixed by `ADR-001` — specifically, must not require a **distributed, multi-writer, eventually-consistent** persistence model to satisfy CONST-1/CONST-3 (the class of trade-off `ADR-001` already rejected when it rejected Microservices). This does **not** prohibit a single-writer topology with read replicas, or any other topology in which exactly one node is authoritative for a given write at a time — such topologies remain single-writer-consistent and are explicitly listed as candidates below.
- Must not preclude future international expansion without re-architecture (`PRODUCT_REQUIREMENTS.md` DATA-3).

## Alternatives Considered

Unranked category-level candidates, not an exhaustive product list:

- **Relational/SQL, ACID, single-node or primary-replica** (illustrative examples of the category only: PostgreSQL, SQL Server, MySQL).
- **Document-oriented store with multi-document transaction support** (illustrative example: MongoDB's multi-document ACID transaction feature).
- **Embedded/lightweight relational store** (illustrative example: SQLite) — fits the Modular Monolith's single-deployable nature but carries different operational trade-offs (backup, concurrent-writer scaling) than a client-server RDBMS.
- **An enhanced in-memory store with durability (e.g., write-ahead logging/snapshotting)** — technically conceivable but does not clearly satisfy "durable, does not lose data on process restart"; flagged as a weak candidate rather than eliminated outright, since ruling it out is itself part of what needs explicit approval, not an assumption this ADR makes for you.

## Trade-offs

- **Relational/SQL:** mature ACID guarantees, broad tooling support, well-understood operational practices; requires schema migrations as the Domain model evolves; the most direct fit for `ADR-004`'s existing "strongly consistent, transactional" requirement.
- **Document store with transactions:** can align naturally with aggregate-oriented persistence (one aggregate ≈ one document), potentially simplifying the aggregate/repository mapping; multi-document ACID transaction support is comparatively newer in this category, with different maturity and operational-tooling trade-offs than relational engines.
- **Embedded relational:** minimal operational overhead, a very natural fit for a single-deployable Modular Monolith; may have different concurrent-write-throughput characteristics than a client-server RDBMS — though no approved document sets a numeric scale target (`ARCHITECTURE.md` Section 14) against which to judge this.

## Risks

- A technology whose transaction model cannot cleanly express the AvailabilitySlot+Session atomic transaction boundary (`ADR-004`) would directly undermine CONST-1, the system's highest-priority invariant.
- `ADR-013` and `ADR-014` are parallel sibling decisions informed by `ADR-015`, not sequential prerequisites of one another — deciding either in isolation, without regard for the other's likely direction, risks a mismatch discovered only during implementation (see Dependencies above), even though neither formally blocks the other.
- Continued delay leaves CONST-1 physically unenforced in production-representative conditions.

## Impact on Existing Architecture

Replaces every `Infrastructure/*/Repositories/*.cs` implementation and `InMemoryUnitOfWork`. Requires no change to Application-layer repository interfaces (already technology-neutral by design, per `ADR-005`'s dependency inversion) or to any Domain class. `IUnitOfWork`'s contract — extended during the Application Pipeline phase to accept touched aggregates — remains unchanged in shape regardless of which technology fills it in.

## Impact on Clean Architecture

None expected. The entire purpose of Infrastructure's isolation (`ARCHITECTURE.md` Section 7: Infrastructure "may be replaced without touching Domain or Application") is that this decision is confined to Infrastructure by construction. This ADR's own existence — a technology decision with no Domain or Application-layer consequence — is direct evidence the boundary is holding.

## Impact on DDD

None to aggregate boundaries or tactical patterns directly. May influence *how* an aggregate is physically mapped to storage (one-table-per-aggregate vs. embedded-document-per-aggregate), but must not be permitted to reshape the aggregate boundaries themselves (`ADR-002`, `DOMAIN_MODEL.md`) — persistence concerns are explicitly forbidden from leaking backward into Domain design (`ADR-004`: "Domain has zero dependency on Infrastructure").

## Impact on Testing

`Infrastructure.Tests` (currently 5 tests) will need real integration-style tests against whichever technology is chosen. `Application.Tests`'s hand-rolled in-memory test doubles are unaffected, since they already exist independently of the real repository implementations.

## Recommendation

No technology is recommended or ranked. Procedurally: resolve `ADR-015` first, then this ADR — ideally in the same sitting as `ADR-014`, a parallel decision also gated by `ADR-015` — since `ADR-016` is constrained by this ADR's outcome and should not be attempted before it. This is a Structural decision under `PROJECT_CONSTITUTION.md`'s Decision-Making Process and requires explicit business-owner approval.

## Assumptions

`ADR-001`'s Modular Monolith and `ADR-002`'s four-bounded-context ownership model remain fixed and unchallenged. The chosen technology is assumed to serve both Domain-owning contexts' storage needs (Scheduling & Booking; Identity & Relationship) without requiring a second, different persistence technology for either — no approved document suggests a need for heterogeneous per-context storage, and this ADR does not invent one.

## Non-Goals

Does not choose an ORM or data-access library. Does not choose a specific vendor, edition, or hosting arrangement. Does not decide backup/recovery policy (`PHYSICAL_DATABASE_STRATEGY.md` Section 16, still open). Does not decide data retention/archival policy (`PHYSICAL_DATABASE_STRATEGY.md` Section 13, still open).

## Questions Requiring Approval

1. Which category (relational / document-with-transactions / embedded-relational / other) should be selected?
2. Within the selected category, is a specific product being mandated now, or deferred to a follow-on, narrower ADR?
3. Does the business have an existing operational or hosting constraint (e.g., an already-contracted cloud provider) that should narrow the candidate set? No approved document currently states one.

---

*Status: Accepted — 2026-07-20. Technology: PostgreSQL.*
