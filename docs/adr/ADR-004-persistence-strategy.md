**Authoritative sources (in priority order):** `PROJECT_CONSTITUTION.md`, `PRODUCT_REQUIREMENTS.md`, `BUSINESS_MODEL.md`, `DOMAIN_MODEL.md`, `ARCHITECTURE.md`, `docs/adr/ADR-001-architecture-style.md`, `docs/adr/ADR-002-domain-boundaries.md`, `docs/adr/ADR-003-authentication-and-authorization.md` (all approved and immutable). This ADR defines architectural persistence principles only — it selects no database vendor, ORM, framework, cloud provider, or storage technology, and none is implied by anything below. Where information needed to complete this ADR is missing from the eight sources above, it is recorded under Open Questions rather than invented.

---

# ADR-004: Persistence Strategy

## Status

**Accepted.** *(Status field corrected 2026-07-21 — this document's own Status was never updated after approval; every ADR from `ADR-013` onward, dated 2026-07-20, already lists this document among its "approved and immutable" sources, and the entire shipped implementation is built on it. Gap first identified by the Final Architecture Review Board's production-readiness audit, 2026-07-21. No content in this ADR is otherwise altered by this correction.)*

## Context

`PRODUCT_REQUIREMENTS.md` establishes two zero-tolerance invariants that persistence must guarantee: no Availability Slot is ever booked by more than one Student/Parent concurrently (CONST-1), and availability/booking data has a single authoritative representation read by all four roles (CONST-3). It further requires that every action creating, rescheduling, or cancelling a booking, or changing availability, be attributable to an identity, role, and timestamp (CONST-2). `ARCHITECTURE.md` already recommends a strongly consistent, transactional datastore category — without naming a product — precisely because it supports CONST-1 and CONST-3 (Section 10), and lists the specific persistence technology, the specific no-double-booking enforcement mechanism, and the audit-storage mechanism as deferred Architectural Decision Candidates (Section 21, Items 2, 3, 5). `ADR-002` fixed that every aggregate has exactly one owning bounded context and that cross-context writes are forbidden. `ADR-003` fixed that identity is owned exclusively by Identity & Relationship and that audit records must capture identity, role, action, and timestamp. This ADR formalizes how persistence is architected across those decisions, without choosing any technology.

## Problem Statement

Given the exclusive aggregate ownership already fixed per bounded context (`ADR-002`), how should TutorFlow persist domain state, bound its transactions, and enforce consistency and concurrency so that no session is ever double-booked (CONST-1), one authoritative schedule representation exists (CONST-3), and every qualifying action is durably attributable (CONST-2) — without selecting a specific storage technology?

## Decision

Each bounded context persists only the aggregates it exclusively owns, per `ADR-002`: Scheduling & Booking persists Session and Availability Slot; Identity & Relationship persists Student, Tutor, Parent/Guardian, and Admin/Staff Accounts, and Relationship; Discovery and Marketplace Oversight persist no aggregate of their own. Persistence is provided by a strongly consistent, transactional mechanism sufficient to enforce CONST-1 and CONST-3 as hard invariants — no specific product is chosen; this remains the Architectural Decision Candidate already listed in `ARCHITECTURE.md` Section 21, Item 2. The booking operation — checking and consuming an Availability Slot and creating the resulting Session — is a single atomic transaction boundary. Cross-context writes are forbidden: a context that needs an effect in another context's owned data must invoke that context's own logic, never write to its persisted state directly. Every qualifying mutation is durably and completely auditable, per CONST-2.

## Persistence Principles

- **Single source of truth:** each piece of schedule and identity data is persisted in exactly one place, never duplicated per role or per context (CONST-3).
- **No double booking:** persistence must make it structurally impossible for two Sessions to consume the same Availability Slot concurrently (CONST-1).
- **Complete auditability:** persistence includes a durable, attributable record of every qualifying mutation (CONST-2).
- **Least privilege at the persistence boundary:** a context's persistence mechanism serves only that context's own Application layer; no context has ambient write access to another's store (CONST-5; `ADR-002`: Integration Rules).
- **Correctness over convenience:** persistence design favors verifiable consistency over performance shortcuts that risk inconsistency (`PROJECT_CONSTITUTION.md`: Engineering Principle 1, Architecture Principle 5).
- **Technology deferred:** no specific database, ORM, or storage product is chosen; only the qualitative properties required are specified here (`PROJECT_CONSTITUTION.md`: Architecture Principle 6).

## Aggregate Persistence Rules

- Every aggregate has exactly one owning bounded context and exactly one persisted representation (`ADR-002`: Context Ownership; CONST-3).
- **Session** and **Availability Slot** are persisted exclusively by Scheduling & Booking.
- **Student, Tutor, Parent/Guardian, Admin/Staff Accounts, and Relationship** are persisted exclusively by Identity & Relationship.
- **Discovery** persists no aggregate (`ADR-002`: Context Ownership) — see Read Model Principles for how it may still access data it does not own.
- **Marketplace Oversight** persists no aggregate of its own; whether it persists any record of its own actions (e.g., a conflict-resolution record) is unresolved (`DOMAIN_MODEL.md` Open Question 13; `ADR-002` Open Question 2) and carried forward as an Open Question here.
- An aggregate's persisted state is mutated only through its owning context's own Application/Domain logic — never written directly by another context or an external process (`ADR-002`: Integration Rules).

## Transaction Boundaries

- The transaction boundary for booking a Session must atomically encompass verifying the Availability Slot is still open, marking it consumed, and creating the Session in Scheduled status — this is the system's single highest-priority transaction boundary, since it is what enforces CONST-1 (`ARCHITECTURE.md` Section 15: Reliability Strategy).
- Cancellation and rescheduling of a Session must be similarly atomic with respect to the Session's own status transition; what happens to the associated Availability Slot on cancellation is unresolved (`DOMAIN_MODEL.md` Open Question 7) and therefore the full transactional shape of cancellation is not yet complete.
- A transaction boundary never spans the owned aggregates of two different bounded contexts at once, since cross-context writes are forbidden (see Cross-Context Persistence Rules). A use case needing effects in two contexts (e.g., Marketplace Oversight resolving a conflict that also changes a Session) is expressed as separate transactions, one per owning context, coordinated by the consuming context's Application layer — never as a single cross-context transaction.
- The specific mechanism that implements this atomicity is not chosen here — see the Architectural Decision Candidate already listed in `ARCHITECTURE.md` Section 21, Item 3.

## Consistency Model

- Strong consistency is required within a bounded context's own aggregate boundary — specifically for Availability Slot/Session, to satisfy CONST-1, and for whichever aggregate is read as the single authoritative schedule representation, to satisfy CONST-3.
- Cross-context reads (e.g., Scheduling & Booking reading whether a Student's Relationship is confirmed, per `ADR-003`) occur at the time of the operation; no approved document specifies whether such a read must reflect the absolute latest Identity & Relationship state or may tolerate bounded staleness — this is an Open Question, not assumed.
- No eventual-consistency mechanism is adopted for within-context aggregate state; this is consistent with `ADR-001`'s rejection of Microservices-style distributed consistency for enforcing CONST-1 and CONST-3.

## Concurrency Strategy

- A concurrent attempt to book an already-consumed Availability Slot must be resolved so that at most one attempt succeeds and the other is rejected explicitly and visibly (CONST-1; `PROJECT_CONSTITUTION.md`: Engineering Principle 4, fail safely and visibly) — never resolved by allowing both to partially succeed or by silently overwriting one outcome.
- The specific concurrency-control technique is not chosen here; it remains the Architectural Decision Candidate already listed in `ARCHITECTURE.md` Section 21, Item 3.
- A concurrency conflict is surfaced as an explicit, handled outcome to the Application layer (`ARCHITECTURE.md` Section 18: Error Handling Strategy), never as silent data corruption.

## Data Ownership

Restated from `ADR-002` and applied specifically to the persistence layer:

- Session and Availability Slot data: persisted exclusively by Scheduling & Booking.
- Student, Tutor, Parent/Guardian, Admin/Staff Account, and Relationship data: persisted exclusively by Identity & Relationship.
- No persisted data is owned by Discovery or Marketplace Oversight; anything either needs is either read from an owning context at the time of use, or — for Discovery only, if ever introduced — held as an explicitly non-authoritative derived projection (see Read Model Principles).

## Cross-Context Persistence Rules

- Cross-context writes are forbidden: no bounded context's persisted data may be written to by another context, directly or indirectly (`ADR-002`: Integration Rules, Data Ownership Rules).
- A context needing to cause a change in another context's aggregate must invoke that context's own Application-layer use case, which alone performs the write to its own persisted state — the same pattern already established for cross-context authorization in `ADR-003`.
- A context may read another context's persisted data, through that context's own interface, to inform its own logic (e.g., Scheduling & Booking reading Relationship confirmation state). Such a read never becomes a second, independently maintained persisted copy of that data (CONST-3).
- The specific mechanism for inter-context calls (e.g., a direct call versus another integration mechanism) is not decided here — it remains the Architectural Decision Candidate already listed in `ARCHITECTURE.md` Section 21, Item 6.

## Read Model Principles

- Discovery, owning no aggregate, is a read-only composition over data owned by Identity & Relationship and Scheduling & Booking (`ADR-002`: Context Ownership).
- Whether Discovery requires its own derived, non-authoritative read projection is not established by any approved document (`ADR-002` Open Question 1). This ADR does not decide that question, but establishes the governing principle: if such a projection is ever introduced, it must be explicitly non-authoritative, clearly derived from and reconciled to the owning context's persisted data, and never treated as a second source of truth (CONST-3).
- Marketplace Oversight's "view all schedules" (`PRODUCT_REQUIREMENTS.md` ADM-3) is a read across Scheduling & Booking's own persisted Session/Availability Slot data; it requires no separate persisted copy, only read access.
- Any read model or projection introduced in the future must be justified against CONST-3 before adoption, per the Constitution's Architecture Governance.

## Audit Persistence Principles

- Every action that creates, reschedules, or cancels a Session, or that changes Availability, must be durably persisted with the acting identity, its role, the action, and a timestamp (CONST-2; `ARCHITECTURE.md` Section 16).
- The same standard extends to Tutor approval and suspension, per `ARCHITECTURE.md` Section 16 and `ADR-003`'s Audit Requirements.
- The audit record must be durable and independently readable by an Admin/Staff member without requiring code-level or direct-database access (`PROJECT_CONSTITUTION.md`: Success Criteria).
- Whether an audit entry must be persisted within the same transaction as the aggregate mutation it records, or may be persisted through a separate mechanism, is not decided here — this is a facet of the Architectural Decision Candidate already listed in `ARCHITECTURE.md` Section 21, Item 5 (audit trail storage mechanism). What this ADR does establish is that the audit entry for a qualifying action must never be lost, whatever mechanism is chosen, since CONST-2 treats attributability as non-negotiable, not best-effort.
- Whether denied authorization attempts must also be persisted in the audit trail remains open (`ADR-003` Open Question 6).

## Alternatives Considered

- **Per-role duplicated schedule copies**, each role storing its own view of availability/bookings — rejected. Directly contradicts CONST-3 (single source of truth) and the Domain Model's explicit rule against per-role data that can drift.
- **Shared persistence where any context can write to any aggregate directly** — rejected. Violates `ADR-002`'s exclusive-ownership and no-cross-context-write rules, and would make CONST-1 and CONST-3 unenforceable if more than one code path could mutate the same data.
- **Eventually-consistent cross-context replication**, where each context keeps its own asynchronously reconciled copy of another's data — rejected. CONST-1 is a zero-tolerance invariant requiring the Availability Slot/Session check to be evaluated against current state, not eventually-consistent state; this mirrors `ADR-001`'s rejection of Microservices-style eventual consistency for the same reason.
- **Strong, transactional consistency within each context's own aggregate boundary, with reads (not copies) across context boundaries (chosen)** — the only option consistent with CONST-1, CONST-3, and `ADR-002`'s ownership model without inventing a tolerance the approved documents do not establish.
- **A single undifferentiated data store with no context-based partitioning of ownership** — rejected. Even though `ADR-001`'s Modular Monolith may co-locate all contexts' data physically, the logical ownership partition fixed by `ADR-002` must still hold; an undifferentiated store would reopen the cross-context write path the Integration Rules forbid.

## Consequences

- Booking, cancellation, and rescheduling logic must be implemented so the Availability Slot/Session consistency check and its corresponding update occur within one atomic transaction, with no window in which two bookings could both succeed.
- Any feature needing data owned by another context must call that context's own read interface at the time of use rather than caching or copying the data into its own persisted store.
- The audit trail becomes a mandatory, first-class part of every qualifying mutating use case's persistence behavior, not an optional addition.
- Because no product is chosen, further technical design (schema shape, specific concurrency mechanism, audit storage shape) remains blocked on the Architectural Decision Candidates already listed in `ARCHITECTURE.md` Section 21 and cannot proceed until those are resolved.

## Risks

- If a future implementer introduces a per-context cache or projection without treating it as explicitly non-authoritative, CONST-3 could be silently violated — guarded against by the Read Model Principles above, but requires active discipline during implementation.
- The unresolved Availability Slot/Session aggregate boundary (`DOMAIN_MODEL.md` Open Question 17) means the exact transaction boundary for booking cannot yet be finalized precisely.
- The unresolved question of whether cancellation reopens the Availability Slot (`DOMAIN_MODEL.md` Open Question 7) leaves the transactional shape of cancellation incomplete.
- Without a chosen concurrency-control technique, there is a risk the no-double-booking invariant is implemented inconsistently across different use cases (book vs. reschedule) if not centralized in one place.
- The undefined staleness tolerance for cross-context reads could, if implemented naively, either over-constrain performance or under-constrain correctness.

## Future Evolution

If a bounded context is later extracted into an independently deployed service (per `ADR-001` and `ADR-002`'s Future Evolution sections), the exclusive aggregate-ownership and no-cross-context-write rules established here are exactly what must be preserved across that extraction; this strategy is designed so extraction changes only the physical location of a store, not the logical ownership rules. If Discovery's read needs later grow to require a genuine derived projection, that must be proposed and justified against CONST-3 through a future ADR, not assumed here. The specific persistence technology, concurrency-control mechanism, and audit-storage mechanism remain open Architectural Decision Candidates (`ARCHITECTURE.md` Section 21, Items 2, 3, 5) to be resolved in future, separate decisions.

## Open Questions

1. Is Availability Slot a separate aggregate from Session, or a sub-component of the Tutor aggregate? Determines the precise transaction boundary for booking. (`DOMAIN_MODEL.md` Open Question 17)
2. Does cancelling a Session automatically reopen its Availability Slot? Determines the transactional shape of cancellation. (`DOMAIN_MODEL.md` Open Question 7)
3. Does Marketplace Oversight require any persisted record of its own (e.g., a conflict-resolution record)? (`DOMAIN_MODEL.md` Open Question 13; `ADR-002` Open Question 2)
4. Does Discovery require a derived, non-authoritative read projection, or is a direct read at query time sufficient? (`ADR-002` Open Question 1)
5. What staleness, if any, is tolerable for a cross-context read (e.g., Scheduling & Booking reading Relationship confirmation state)? Not addressed by any approved document.
6. Must an audit entry be persisted within the same transaction as the aggregate mutation it records, or may it be persisted through a separate mechanism? Not addressed by any approved document.
7. Must denied authorization attempts be persisted in the audit trail? (`ADR-003` Open Question 6)
8. What is the specific persistence technology, concurrency-control mechanism, and audit-storage mechanism? Remain open Architectural Decision Candidates. (`ARCHITECTURE.md` Section 21, Items 2, 3, 5)

## Traceability

| Element of this ADR | Source |
|---|---|
| No double booking as a hard persistence invariant | `PRODUCT_REQUIREMENTS.md` CONST-1 |
| Single source of truth as a hard persistence invariant | `PRODUCT_REQUIREMENTS.md` CONST-3 |
| Complete auditability of qualifying mutations | `PRODUCT_REQUIREMENTS.md` CONST-2; `ARCHITECTURE.md` Section 16 |
| Recommended datastore category (transactional, not a product) | `ARCHITECTURE.md` Section 10 |
| Exclusive aggregate ownership per bounded context | `ADR-002`: Context Ownership |
| No-cross-context-write rule | `ADR-002`: Integration Rules, Data Ownership Rules |
| Cross-context read pattern (read, not copy) | `ADR-002`: Integration Rules; `ADR-003`: Cross-Context Authorization Rules |
| Rejection of eventual consistency / distributed replication | `docs/adr/ADR-001-architecture-style.md`: Alternatives Considered (Microservices) |
| Fail safely and visibly on concurrency conflict | `PROJECT_CONSTITUTION.md`: Engineering Principle 4 |
| Deferred persistence technology, concurrency mechanism, audit storage | `ARCHITECTURE.md` Section 21, Items 2, 3, 5 |

---

*Status: Accepted. Status field corrected 2026-07-21 to match how every later ADR and the shipped implementation already treat this document — see the Status section above.*
