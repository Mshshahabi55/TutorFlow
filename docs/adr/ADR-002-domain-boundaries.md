**Authoritative sources (in priority order):** `PROJECT_CONSTITUTION.md`, `PRODUCT_REQUIREMENTS.md`, `BUSINESS_MODEL.md`, `DOMAIN_MODEL.md`, `ARCHITECTURE.md`, `docs/adr/ADR-001-architecture-style.md` (all approved and immutable). This ADR formalizes the Bounded Context Candidates already identified in `DOMAIN_MODEL.md` and already carried into `ARCHITECTURE.md`'s layering and folder structure, as the binding module boundaries required by `ADR-001`'s Modular Monolith decision. It introduces no new business rule, technology choice, API design, or persistence schema — only the rules governing how the already-approved bounded contexts relate to one another. Where information needed to complete this ADR is missing from the six sources above, it is recorded under Open Questions rather than invented.

---

# ADR-002: Domain-Driven Design Bounded Context Boundaries

## Status

**Accepted.** *(Status field corrected 2026-07-21 — this document's own Status was never updated after approval; every ADR from `ADR-013` onward, dated 2026-07-20, already lists this document among its "approved and immutable" sources, and the entire shipped implementation is built on it. Gap first identified by the Final Architecture Review Board's production-readiness audit, 2026-07-21. No content in this ADR is otherwise altered by this correction.)*

## Context

`DOMAIN_MODEL.md` identified four Bounded Context Candidates — Scheduling & Booking, Identity & Relationship, Discovery, Marketplace Oversight — derived directly from the functional requirement groupings in `PRODUCT_REQUIREMENTS.md` (Sections 6.1–6.4). `ARCHITECTURE.md` carried these forward "unchanged" as its Bounded Context Mapping (Section 4), used them to organize the Domain and Application layers (Sections 8–9), and reflected them in the recommended folder structure (Sections 19–20). `ADR-001` then decided that TutorFlow's deployment topology is a single Modular Monolith "partitioned along the four bounded contexts already identified in `DOMAIN_MODEL.md`" — making the precise definition of those boundaries, their ownership, and the rules governing their interaction a direct prerequisite for that decision to be implementable. This ADR supplies that definition and elevates it from "candidate" to the authoritative reference for all future module boundaries.

## Problem Statement

Given the Modular Monolith style already decided in `ADR-001`, what are TutorFlow's definitive internal bounded contexts, what does each own, how do they depend on one another, and what rules prevent business logic or data ownership from leaking across their boundaries — such that the single-source-of-truth and auditability invariants already established (`PRODUCT_REQUIREMENTS.md` CONST-2, CONST-3) hold as the system grows?

## Decision

TutorFlow adopts exactly the four bounded contexts already identified in `DOMAIN_MODEL.md` as its official, binding module boundaries: **Scheduling & Booking**, **Identity & Relationship**, **Discovery**, and **Marketplace Oversight**. No context is added, merged, or further split beyond what is already approved. These four contexts, their ownership, and their relationships (defined below) are the authoritative reference for all future Domain/Application-layer module organization within the Modular Monolith decided in `ADR-001`. Any change to this decomposition requires revisiting this ADR through the Constitution's Decision-Making Process, not an ad hoc implementation choice.

## Why These Bounded Contexts Were Chosen

- They map directly, one-to-one, onto the functional requirement groupings already approved in `PRODUCT_REQUIREMENTS.md` Section 6 (6.1 Identity & Relationships → Identity & Relationship context; 6.2 Scheduling & Booking → Scheduling & Booking context; 6.3 Discovery → Discovery context; 6.4 Admin → Marketplace Oversight context), satisfying Architecture Principle 8 (traceability from business rule to system behavior) directly by construction (`PROJECT_CONSTITUTION.md`).
- They are the same four contexts `ARCHITECTURE.md` already used to organize its Domain and Application layers (Sections 8–9) and its recommended folder structure (Sections 19–20); adopting any different decomposition now would contradict architecture already approved.
- They respect the Constitution's "Four roles, one truth" principle (Core Principle 4) by keeping each role's data owned in exactly one place rather than scattering it per role.
- No alternative decomposition is supported by the approved documents — inventing one here would violate the instruction to use only facts already approved.

## Bounded Contexts

1. **Scheduling & Booking**
2. **Identity & Relationship**
3. **Discovery**
4. **Marketplace Oversight**

Explicitly **not** bounded contexts in this decomposition, per approved scope exclusions: Payments, In-Platform Communication/Content Delivery, Progress Tracking/Grading (`PROJECT_CONSTITUTION.md`: Project Scope; `PRODUCT_REQUIREMENTS.md` Section 9).

## Responsibilities of Each Context

| Context | Responsibilities | Source |
|---|---|---|
| **Scheduling & Booking** | Declaring Tutor availability; booking, cancelling, and rescheduling a Session; enforcing Session status transitions (Scheduled/Completed/Cancelled/No-Show); enforcing no-double-booking. | `PRODUCT_REQUIREMENTS.md` SCH-1 to SCH-7, CONST-1 |
| **Identity & Relationship** | Self-registration of Student, Tutor, and Parent/Guardian; Tutor approval/suspension workflow; Tutor offering attributes (hourly rate, session duration, subjects, language, location); Parent-Student Relationship invitation and confirmation. | `PRODUCT_REQUIREMENTS.md` IDR-1 to IDR-6, SCH-3, DISC-2, ADM-1, ADM-2 |
| **Discovery** | Search/filter of Tutors by Subject, Availability, Language, Location/Time Zone; composes reads over data owned by Identity & Relationship and Scheduling & Booking; owns no aggregate of its own. | `PRODUCT_REQUIREMENTS.md` DISC-1; `DOMAIN_MODEL.md` Bounded Context Candidates |
| **Marketplace Oversight** | Approving/suspending Tutors; viewing all schedules; resolving booking conflicts; managing user accounts. Acts on data owned by Identity & Relationship and Scheduling & Booking rather than owning its own domain data. | `PRODUCT_REQUIREMENTS.md` ADM-1 to ADM-5 |

## Context Ownership

Each aggregate, entity, and value object from `DOMAIN_MODEL.md` has exactly one owning context:

- **Scheduling & Booking owns:** Session, Availability Slot, and their value objects — Delivery Mode, Session Status, Session Duration.
- **Identity & Relationship owns:** Student, Tutor, Parent/Guardian, Admin/Staff (Account entities), Relationship, and their value objects — Hourly Rate, Subject, Language, Location/Time Zone, Relationship Status.
- **Discovery owns:** no aggregate, entity, or value object. It is a read-only composition over data owned elsewhere (`DOMAIN_MODEL.md`: Aggregates, Bounded Context Candidates).
- **Marketplace Oversight owns:** no aggregate, entity, or value object identified in `DOMAIN_MODEL.md`. Its actions (approve/suspend a Tutor, resolve a conflict) modify state owned by Identity & Relationship or Scheduling & Booking, through those contexts' own logic — see Integration Rules below.

## Upstream / Downstream Relationships

Directly restated from `ARCHITECTURE.md` Section 5:

- **Identity & Relationship** is upstream of **Scheduling & Booking** — a booking must know whether a Student is a minor and, if so, whether a confirmed Relationship exists (`PRODUCT_REQUIREMENTS.md` IDR-5, IDR-6).
- **Identity & Relationship** and **Scheduling & Booking** are both upstream of **Discovery**, which composes their data and has no upstream dependents of its own (`PRODUCT_REQUIREMENTS.md` DISC-1, DISC-2).
- **Identity & Relationship** and **Scheduling & Booking** are both upstream of **Marketplace Oversight**, which consumes and acts on their data and has no upstream dependents of its own (`PRODUCT_REQUIREMENTS.md` ADM-1 to ADM-5).
- Neither Discovery nor Marketplace Oversight is upstream of any other context.

## Shared Concepts

Concepts referenced across contexts but owned exactly once (per CONST-3):

- **Tutor identity and approval state** — owned by Identity & Relationship; read by Scheduling & Booking (to associate a Session with a Tutor), Discovery (search), and acted upon by Marketplace Oversight (through Identity & Relationship's own logic, not directly).
- **Student / Parent-Guardian identity and Relationship confirmation state** — owned by Identity & Relationship; read by Scheduling & Booking to enforce IDR-5/IDR-6.
- **Role vocabulary** (Student, Tutor, Parent/Guardian, Admin/Staff) — a shared term across all four contexts' Ubiquitous Language (`DOMAIN_MODEL.md`: Ubiquitous Language), but the authoritative Account record for each role is owned exactly once, by Identity & Relationship.

## Integration Rules Between Contexts

- A context may read data owned by another context only through that owning context's own interface; no context reaches directly into another context's persisted data.
- A context must never write to another context's aggregate directly. State changes happen only through the owning context's own logic (e.g., Marketplace Oversight's "approve a Tutor" action invokes Identity & Relationship's own approval logic; it does not set the Tutor's approval flag itself).
- Discovery, owning no aggregate, performs reads only — it issues no writes to any other context.
- The specific in-process mechanism used for this integration (direct calls, an internal mediator, or another mechanism) is not decided by this ADR — it remains Architectural Decision Candidate #6 in `ARCHITECTURE.md` Section 21.

## Dependency Rules

- Scheduling & Booking depends (read-only) on Identity & Relationship for minor/Relationship-confirmation facts.
- Discovery depends (read-only) on both Identity & Relationship and Scheduling & Booking.
- Marketplace Oversight depends on both Identity & Relationship and Scheduling & Booking, invoking their own use cases rather than bypassing them.
- No context depends on Discovery or on Marketplace Oversight; both are the leaves of this dependency graph.
- Within each context, the layer dependency rule already established in `ARCHITECTURE.md` Section 7 (Presentation → Application → Domain; Infrastructure implements Application's interfaces) continues to apply independently.

## Data Ownership Rules

Directly derived from CONST-3 (single source of truth) applied at the context level:

- Every aggregate, entity, and value object has exactly one owning context; no other context may persist its own copy of it.
- Session and Availability Slot data is owned exclusively by Scheduling & Booking.
- Student, Tutor, Parent/Guardian, Admin/Staff, and Relationship data is owned exclusively by Identity & Relationship.
- Discovery holds no authoritative data of its own. Whether it requires any derived, non-authoritative read projection (e.g., a search index) is not established by any approved document — see Open Questions.
- Whether Marketplace Oversight requires any record of its own (e.g., a conflict-resolution record) beyond the data it acts upon is not established by any approved document, since the exact mechanics of "resolve booking conflicts" remain open (`DOMAIN_MODEL.md` Open Question 13) — see Open Questions.

## Rules That Prevent Business Logic Leakage

- Every business rule and invariant (e.g., CONST-1: no double-booking) is enforced only within its owning context's Domain layer — never duplicated or re-implemented by a consuming context.
- Discovery, owning no aggregate, enforces no business rule; it performs read composition only.
- Marketplace Oversight must not re-implement Tutor-approval or Session-status logic; it must invoke the owning context's existing use cases so each rule is enforced in exactly one place, consistent with the Constitution's warning against "scattered special cases in logic" (Architecture Principle 4).
- A context reading a fact owned by another context (e.g., Scheduling & Booking checking whether a Student's Relationship is confirmed) must read that fact from the owning context at the time it is needed rather than re-deriving or independently maintaining the rule that produces it.

## Why Other Decompositions Were Rejected

- **Technical-layer-only decomposition** (no bounded contexts, only Domain/Application/Infrastructure folders) was rejected because `ADR-001` explicitly requires the Modular Monolith to be "partitioned along the four bounded contexts already identified" — a purely technical decomposition would contradict that already-approved decision.
- **One module per Role** (separate Student, Tutor, Parent/Guardian, Admin modules) was rejected because it would require Session and Availability Slot data to be duplicated across multiple role-specific modules, directly violating CONST-3 (single source of truth) and the single-aggregate-ownership model already established in `DOMAIN_MODEL.md`.
- **Merging Discovery into Scheduling & Booking** was rejected because Discovery depends equally on Identity & Relationship data (Tutor attributes) and Scheduling & Booking data (Availability); folding it into only one of its two upstream contexts would misrepresent that dependency, and `DOMAIN_MODEL.md` already lists Discovery as a distinct candidate.
- **Merging Marketplace Oversight into Identity & Relationship and Scheduling & Booking directly** was rejected because Admin/Staff is a distinct actor with cross-cutting oversight responsibility (`PROJECT_CONSTITUTION.md`: Product Goal 5; Governance Principle 1, single point of accountability), and `DOMAIN_MODEL.md` already models it as a distinct context.
- **Any decomposition finer than these four, mapped to independent services**, is rejected for the same reasons Microservices was rejected in `ADR-001` — no approved document establishes a need for independent deployability at a finer grain than these four contexts.

## Consequences

- The folder/module organization already sketched in `ARCHITECTURE.md` Sections 19–20 is now bound by this ADR; it is no longer merely a recommendation but the required structure.
- Any feature must be implemented inside its owning context and must integrate with other contexts only through that context's own interface, never by reaching into another context's data directly.
- Because Discovery and Marketplace Oversight own no data, their implementation depends entirely on stable read/invoke access to Identity & Relationship and Scheduling & Booking; the shape of that access is a future concern (not designed here, per instruction not to design APIs) but its existence is now a formal architectural dependency.

## Risks

- The unresolved question of whether Availability Slot is a separate aggregate from Session or a sub-component of the Tutor aggregate (`DOMAIN_MODEL.md` Open Question 17) could cause Scheduling & Booking's internal boundary to be implemented inconsistently until resolved.
- Marketplace Oversight owning no data of its own creates a temptation to bypass the owning context's use cases and mutate another context's aggregate directly "for convenience" (e.g., flipping a Tutor's suspension flag from Oversight code) — this would violate the Integration Rules above and must be actively guarded against.
- If Admin/Staff permission tiering (`DOMAIN_MODEL.md` Open Question 12) is later resolved to require sub-roles, Marketplace Oversight's internal authorization logic may need revision, though its context boundary is not expected to change.
- The unresolved mechanics of "resolve booking conflicts" (`DOMAIN_MODEL.md` Open Question 13) leave Marketplace Oversight's precise responsibilities under-specified, risking scope creep into Scheduling & Booking's territory if implemented carelessly.

## Future Evolution

If `ADR-001`'s Future Evolution path is later exercised — extracting a context into an independently deployed service on the basis of a specific, approved need — the ownership, upstream/downstream, and integration rules defined in this ADR are the contract that extraction must preserve. Any new bounded context (for example, if Payments or Communication is later brought into scope through a Constitution amendment) would be introduced through a new ADR that extends or supersedes this one, not by informally attaching it to an existing context. Discovery's currently data-less nature may warrant revisiting if search/performance requirements are documented in the future, but no such requirement exists today.

## Traceability Back to Approved Documents

| Element of this ADR | Source |
|---|---|
| The four bounded contexts themselves | `DOMAIN_MODEL.md`: Bounded Context Candidates; `ARCHITECTURE.md` Section 4 |
| Modular Monolith requiring context-based partitioning | `docs/adr/ADR-001-architecture-style.md`: Decision |
| Responsibilities per context | `PRODUCT_REQUIREMENTS.md` Sections 6.1–6.4 |
| Aggregate/entity/value-object ownership | `DOMAIN_MODEL.md`: Aggregates, Entities, Value Objects |
| Upstream/downstream relationships | `ARCHITECTURE.md` Section 5 |
| Single-source-of-truth data ownership rule | `PRODUCT_REQUIREMENTS.md` CONST-3 |
| No-double-booking invariant | `PRODUCT_REQUIREMENTS.md` CONST-1 |
| Auditability of cross-context actions | `PRODUCT_REQUIREMENTS.md` CONST-2 |
| Role-aware, not role-hardcoded logic placement | `PROJECT_CONSTITUTION.md`: Architecture Principle 4 |
| Rejection of finer-grained/service-level decomposition | `docs/adr/ADR-001-architecture-style.md`: Alternatives Considered (Microservices) |

## Open Questions

1. Does Discovery require any derived, non-authoritative read projection (e.g., a search index) of data owned by Identity & Relationship and Scheduling & Booking, or is a direct read sufficient? Not addressed in any approved document.
2. Does Marketplace Oversight require any record of its own (e.g., a conflict-resolution record), or does it act purely on data owned by other contexts? Depends on the unresolved mechanics of "resolve booking conflicts" (`DOMAIN_MODEL.md` Open Question 13).
3. Is Availability Slot a separate aggregate from Session, or a sub-component of the Tutor aggregate? This affects the precise internal boundary within Scheduling & Booking. (`DOMAIN_MODEL.md` Open Question 17)
4. How is an Admin/Staff account created, and does Identity & Relationship own that process the same way it owns Student/Tutor/Parent/Guardian self-registration? (`DOMAIN_MODEL.md` Open Question 15)
5. Do Admin/Staff accounts have permission tiers, and if so, does that affect Marketplace Oversight's internal structure? (`DOMAIN_MODEL.md` Open Question 12)
6. What information or credentials does a Tutor submit for Admin approval — does this introduce any data (e.g., documents/credentials) that Identity & Relationship must own but which is not yet modeled? (`DOMAIN_MODEL.md` Open Question 4)

---

*Status: Accepted. Status field corrected 2026-07-21 to match how every later ADR and the shipped implementation already treat this document — see the Status section above.*
