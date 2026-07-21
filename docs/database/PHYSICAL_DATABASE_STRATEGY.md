**Authoritative sources (in priority order):** `PROJECT_CONSTITUTION.md`, `PRODUCT_REQUIREMENTS.md`, `BUSINESS_MODEL.md`, `DOMAIN_MODEL.md`, `ARCHITECTURE.md`, all approved ADRs (`docs/adr/ADR-001` through `ADR-010`), and `docs/database/DOMAIN_DATA_MODEL.md` (all approved and immutable). This document defines architectural database decisions only — it generates no SQL, no table definition, no migration, no index, no stored procedure, no ORM mapping, and names no vendor-specific feature. Where information needed to complete this document is missing from the approved sources, it is recorded under Open Questions rather than invented.

---

# 1. Purpose

This document translates the logical domain data model (`docs/database/DOMAIN_DATA_MODEL.md`) and the persistence principles already fixed in `ADR-004` into a physical database strategy — the architectural decisions that will govern how data is transacted, kept consistent, and protected once a physical technology is chosen — without choosing that technology. It exists to constrain and guide future physical schema design so that no implementation decision is made in contradiction of the invariants and ownership rules already approved (`PROJECT_CONSTITUTION.md`: Architecture Principle 6).

# 2. Scope

**In scope:** strategic, technology-neutral principles for transaction management, concurrency control, consistency, identity, referential integrity, soft-delete/terminal-state handling, audit storage, read models, archival, performance, scalability, and backup/recovery.

**Out of scope:** any specific database vendor, SQL statement, table definition, migration, index, stored procedure, ORM mapping, or vendor-specific feature. The specific persistence technology, concurrency mechanism, and audit storage mechanism remain deferred Architectural Decision Candidates (`ARCHITECTURE.md` Section 21, Items 2, 3, 5).

# 3. Physical Design Principles

- **Single source of truth, preserved physically:** each aggregate's data has exactly one physical home, owned by exactly one bounded context; no physical design may introduce a second, independently-writable copy of the same aggregate's data (CONST-3; `ADR-002`; `ADR-004`; `DOMAIN_DATA_MODEL.md` Section 12).
- **No double booking, preserved physically:** whatever physical mechanism is chosen must make it structurally impossible for two Sessions to consume the same Availability Slot concurrently (CONST-1; `ADR-004`: Concurrency Strategy).
- **Correctness before convenience, technology deferred:** physical decisions favor verifiable correctness over performance shortcuts, and no specific technology is chosen until justified (`PROJECT_CONSTITUTION.md`: Engineering Principle 1, Architecture Principle 6).
- **Auditability preserved physically:** every action requiring an audit record (CONST-2; `ADR-009`) has a durable physical representation that must never be lost.
- **Physical structure follows logical ownership:** this strategy introduces no storage structure that violates the aggregate ownership already fixed in `ADR-002` and `DOMAIN_DATA_MODEL.md` Section 4.

# 4. Aggregate-to-Persistence Strategy

- Each aggregate — Tutor, Availability Slot, Session, Relationship, and Account (specialized per role) — is persisted as a unit within its owning bounded context's physical storage; no aggregate's data is split across storage owned by different contexts (`ADR-002`; `ADR-004`; `DOMAIN_DATA_MODEL.md` Section 4).
- Discovery and Marketplace Oversight require no dedicated aggregate storage of their own, since neither owns an aggregate (`ADR-002`; `DOMAIN_DATA_MODEL.md`).
- Whether each bounded context's storage is physically separate or co-located within one physical store is not decided here; either way, the logical ownership partition must hold regardless of physical co-location, consistent with `ADR-004`'s rejection of "a single undifferentiated data store with no context-based partitioning of ownership" even under the Modular Monolith decided in `ADR-001`.

# 5. Transaction Strategy

- The booking transaction — verifying and consuming an Availability Slot and creating the resulting Session — is the system's single highest-priority atomic transaction boundary (`ADR-004`: Transaction Boundaries; `ARCHITECTURE.md` Section 15).
- A transaction never spans two bounded contexts' owned aggregates (`ADR-004`).
- A cross-context use case is expressed as separate transactions, one per owning context, coordinated by the consuming context's Application layer (`ADR-004`; `ADR-005`).
- The precise mechanism by which atomicity is physically achieved is not chosen here — it remains the Architectural Decision Candidate already listed in `ARCHITECTURE.md` Section 21, Item 3.
- Whether an audit entry is written within the same physical transaction as its triggering mutation, or through a separate mechanism, remains open (`ADR-004` Open Question 6; `ADR-009` Open Question 3).

# 6. Concurrency Strategy

- A concurrent attempt to book an already-consumed Availability Slot must resolve so that at most one attempt succeeds and the other is rejected explicitly and visibly (CONST-1; `PROJECT_CONSTITUTION.md`: Engineering Principle 4) — never partial success, never a silent overwrite.
- The specific concurrency-control technique is not chosen here; it remains the Architectural Decision Candidate already listed in `ARCHITECTURE.md` Section 21, Item 3.
- A concurrency conflict is classified as a Domain-Error-equivalent outcome, not an Infrastructure Failure, since it protects the same business invariant Domain validation protects (`ADR-007`: Relationship to Persistence; `ADR-008`: Error Classification Principles).

# 7. Consistency Strategy

- Strong consistency is required within a bounded context's own aggregate boundary — specifically for Availability Slot/Session (CONST-1) and for whichever aggregate serves as the single authoritative schedule representation (CONST-3) (`ADR-004`: Consistency Model).
- Cross-context reads occur at the time of the operation; no approved document establishes whether such a read must reflect the absolute latest state of the owning context or may tolerate bounded staleness (see Open Questions).
- No eventual-consistency mechanism is adopted for within-context aggregate state, consistent with `ADR-001`'s rejection of Microservices-style distributed consistency for enforcing CONST-1 and CONST-3.

# 8. Identity Strategy

- Every aggregate has a conceptual identity, distinct from its attributes, that persists across its lifecycle (`DOMAIN_DATA_MODEL.md` Section 6).
- A cross-context reference is to another aggregate's identity only, never an embedding of its data (`ADR-002`: Integration Rules; `DOMAIN_DATA_MODEL.md` Section 6).
- The specific identifier generation scheme (for example, sequential versus globally unique, centrally issued versus locally generated) is not chosen here; it is deferred as a physical/technology decision (`PROJECT_CONSTITUTION.md`: Architecture Principle 6). What is established is that identities must be stable and unambiguous within their owning aggregate type, since both the no-double-booking and single-source-of-truth invariants depend on unambiguously identifying exactly which Availability Slot, Session, or Tutor is being referenced.

# 9. Referential Integrity Strategy

- The logical referential integrity rules already established in `DOMAIN_DATA_MODEL.md` Section 13 must hold physically: a Session always references exactly one valid Tutor and Student; a Session's optional Parent/Guardian reference, when present, corresponds to a confirmed Relationship at the time of booking; a Relationship always references exactly one Parent/Guardian and one Student; an Availability Slot always references exactly one Tutor; a Session references a slot that was open at the moment of booking.
- Because cross-context writes are forbidden (`ADR-002`) and a transaction never spans two contexts (`ADR-004`), referential integrity across a context boundary cannot be enforced by a single physical mechanism spanning both contexts' storage. It is enforced instead by the referencing context's own logic, reading the referenced aggregate's identity and state through the owning context's own interface at the time the reference is created (`ADR-002`: Integration Rules).
- Within a single context's own owned aggregates, referential integrity may be enforced by whatever physical mechanism is chosen later; that specific mechanism is not decided here.
- Whether an entity can ever be deleted outright, as opposed to reaching a terminal or suspended state, is unresolved (`DOMAIN_DATA_MODEL.md` Open Question 11) and directly affects how referential integrity must handle a potentially-removed reference target — carried forward as an Open Question.

# 10. Soft Delete Strategy

- No approved document establishes a deletion policy for any entity. The state transitions already approved — a Tutor's suspension state, a Session's terminal statuses (Completed, Cancelled, No-Show), a Relationship's status — function as soft, in-model state changes, not deletions, and are the only deletion-adjacent mechanisms currently established.
- Because CONST-2 requires every mutating action to remain attributable and auditable, and `ADR-009`'s audit trail must remain intact regardless of an entity's current state, any future deletion policy must not be permitted to erase the historical facts the audit trail already recorded. An audit record's durability is not contingent on the referenced entity continuing to exist in its original form.
- This document does not invent a soft-delete mechanism beyond the status/state fields already established; whether a distinct "soft delete" concept is needed in addition to them is neither established nor ruled out (see Open Questions).

# 11. Audit Storage Strategy

- Every action that creates, reschedules, or cancels a Session, or changes Availability, plus Tutor approval and suspension, is durably recorded with identity, role, action, and timestamp (CONST-2; `ADR-004`: Audit Persistence Principles; `ADR-009`); this record must never be lost (`ADR-006`: Reliability Principles).
- Audit storage is owned per bounded context, following the same exclusive-ownership rule as any other data (`ADR-009`: Audit Principles) — it is not a single, ownerless, cross-context audit store.
- Whether the audit record's physical storage must guarantee same-transaction durability with its triggering mutation, or may be reconciled through a separate mechanism, is not decided here — it remains the open question already carried through `ADR-004`, `ADR-006`, and `ADR-009`.
- No specific audit storage mechanism is chosen; it remains the Architectural Decision Candidate already listed in `ARCHITECTURE.md` Section 21, Item 5.

# 12. Read Model Strategy

- Discovery's Tutor search view and Marketplace Oversight's all-schedules and audit-trail views are read compositions over data owned by other contexts; no independent authoritative copy is required for either (`DOMAIN_DATA_MODEL.md` Section 17; `ADR-004`: Read Model Principles).
- If a materialized, non-authoritative projection is ever introduced (for example, for Discovery's search performance), it must be explicitly derived from, and reconciled to, the owning context's data, and never treated as a second source of truth (CONST-3; `ADR-004`).
- Whether any read model requires materialization at all, versus computing reads live at query time, is not established (see Open Questions).

# 13. Archival Strategy

- No approved document establishes a data retention period, archival trigger, or archival mechanism for any entity; `PRODUCT_REQUIREMENTS.md` already leaves data retention periods open generally (Section 10.5, Item 18).
- Because personal data — including minors', handled via Parent/Guardian — must be protected to a GDPR-grade standard (CONST-4), any future archival policy must account for data-minimization and retention-limitation principles inherent to that standard; the specific retention periods and archival mechanism are not established here.
- This document does not invent an archival schedule or mechanism; it records retention/archival policy as a required future decision.

# 14. Performance Principles

- `PROJECT_CONSTITUTION.md`'s KPI section explicitly defers specific targets and instrumentation until "the domain and metrics ownership are established"; no approved document establishes a specific latency, throughput, or capacity target.
- The qualitative principle that does apply: correctness before convenience (Engineering Principle 1) and reliability over cleverness (Architecture Principle 5) — a performance optimization must never compromise CONST-1 or CONST-3. No performance shortcut may weaken the no-double-booking transaction guarantee, and none may introduce a second, potentially-stale copy of authoritative data unless it is explicitly non-authoritative, per the Read Model Strategy above.
- No specific performance target is invented here; this remains an Open Question pending KPI instrumentation.

# 15. Scalability Principles

- No numeric scale target is defined by any approved document; the qualitative constraint already established is that the architecture — and, by extension, this physical database strategy — must not preclude "more tutors, more students, more markets" (`PROJECT_CONSTITUTION.md`: Product Goal 7) or require re-architecture to support future international expansion (`PRODUCT_REQUIREMENTS.md` DATA-3), consistent with `ARCHITECTURE.md` Section 14.
- Consistent with Core Principle 5 ("small foundation, deliberate growth") and Architecture Principle 7 ("evolvability over premature generality"), this document does not prescribe a specific physical scaling mechanism not yet justified by a demonstrated need.
- Whatever physical scaling mechanism is eventually adopted must preserve exclusive aggregate ownership (`ADR-002`) and single source of truth (CONST-3); scaling is not license to duplicate authoritative data across replicas that could drift, absent a specific, approved reconciliation mechanism, which is not established here.

# 16. Backup & Recovery Principles

- No approved document establishes a specific backup schedule, recovery point objective, recovery time objective, or disaster-recovery mechanism.
- The qualitative principle that does apply: because CONST-1, CONST-2, and CONST-3 are non-negotiable, any backup/recovery mechanism must preserve them on restoration — a restored state must never reintroduce a double-booking, must never lose the attributability of a previously recorded action, and must never leave two roles with a drifted view of the same schedule fact.
- Specific backup/recovery targets and mechanisms are not established and are recorded as Open Questions rather than invented.

# 17. Future Evolution

Once the persistence technology, concurrency mechanism, and audit storage mechanism (`ARCHITECTURE.md` Section 21, Items 2, 3, 5) are chosen through the Constitution's Decision-Making Process, this document's principles constrain, but do not replace, those decisions. If a bounded context is later extracted into an independently deployed service (per `ADR-001`/`ADR-002` Future Evolution), the physical database strategy for that context must still preserve the aggregate ownership, transaction, and consistency rules established here and in `ADR-004`. Soft-delete, archival, backup/recovery, and performance/scalability targets, once defined, should be recorded as explicit amendments or a future ADR, not silently assumed during implementation.

# 18. Open Questions

1. What is the specific persistence technology? (`ARCHITECTURE.md` Section 21, Item 2)
2. What is the specific concurrency-control mechanism? (`ARCHITECTURE.md` Section 21, Item 3; `ADR-004`)
3. What is the specific audit storage mechanism? (`ARCHITECTURE.md` Section 21, Item 5)
4. Must an audit entry be persisted within the same transaction as its triggering mutation? (`ADR-004` Open Question 6; `ADR-009` Open Question 3)
5. What staleness, if any, is tolerable for a cross-context read? (`ADR-004`)
6. Can any entity be deleted outright, versus only reaching a terminal or suspended state? (`DOMAIN_DATA_MODEL.md` Open Question 11)
7. What data retention periods apply to each data category? (`PRODUCT_REQUIREMENTS.md` Section 10.5, Item 18)
8. What archival mechanism, if any, applies once retention periods are defined? Not addressed by any approved document.
9. What specific numeric performance and scalability targets apply? (`BUSINESS_MODEL.md` Open Questions, Item 10)
10. What backup schedule, recovery point objective, and recovery time objective apply? Not addressed by any approved document.
11. Is Availability Slot a separate aggregate from Session, affecting exactly how referential integrity and transaction boundaries are physically structured? (`DOMAIN_MODEL.md` Open Question 17)
12. Does Discovery require a materialized read model, or is a live query sufficient? (`ADR-002` Open Question 1)

# 19. Risks

- If a future implementer designs a physical mechanism spanning two bounded contexts' storage within a single transaction "for convenience," this would violate `ADR-002`'s and `ADR-004`'s no-cross-context-transaction rule.
- Absent defined retention, archival, and backup policies, there is a compliance risk under the GDPR-grade data protection standard (CONST-4) if personal data is retained indefinitely without a deliberate policy.
- Absent defined performance/scalability targets, there is a risk of either premature over-engineering, contradicting Architecture Principle 7, or under-provisioning once real usage is known.
- If soft-delete/terminal-state semantics are implemented inconsistently across contexts, referential integrity and audit completeness could be compromised.
- Choosing a concurrency-control mechanism that is insufficiently strict is a direct risk to CONST-1, the system's highest-priority invariant, and must be verified before release (`PROJECT_CONSTITUTION.md`: Quality Principle 2).

# 20. Traceability

| Element of this Document | Source |
|---|---|
| Single source of truth preserved physically | `PRODUCT_REQUIREMENTS.md` CONST-3; `ADR-002`; `ADR-004` |
| No double booking preserved physically | `PRODUCT_REQUIREMENTS.md` CONST-1; `ADR-004`: Concurrency Strategy |
| Auditability preserved physically | `PRODUCT_REQUIREMENTS.md` CONST-2; `ADR-004`: Audit Persistence Principles; `ADR-009` |
| Aggregate-to-context ownership mapping | `ADR-002`: Context Ownership; `DOMAIN_DATA_MODEL.md` Section 4 |
| Transaction boundary rules | `ADR-004`: Transaction Boundaries |
| Referential integrity rules (logical) | `DOMAIN_DATA_MODEL.md` Section 13 |
| Read model principles | `ADR-004`: Read Model Principles; `DOMAIN_DATA_MODEL.md` Section 17 |
| Deferred persistence, concurrency, and audit storage technology | `ARCHITECTURE.md` Section 21, Items 2, 3, 5 |
| Deferred scalability/performance targets | `PROJECT_CONSTITUTION.md`: KPI-Based Success Criteria; `ARCHITECTURE.md` Section 14 |
| GDPR-grade data protection applied to retention/archival | `PRODUCT_REQUIREMENTS.md` CONST-4 |

---

*Status: Draft — pending user approval. No further documents or code are to be created until this document is approved.*
