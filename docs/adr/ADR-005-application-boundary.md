**Authoritative sources (in priority order):** `PROJECT_CONSTITUTION.md`, `PRODUCT_REQUIREMENTS.md`, `BUSINESS_MODEL.md`, `DOMAIN_MODEL.md`, `ARCHITECTURE.md`, `docs/adr/ADR-001-architecture-style.md`, `docs/adr/ADR-002-domain-boundaries.md`, `docs/adr/ADR-003-authentication-and-authorization.md`, `docs/adr/ADR-004-persistence-strategy.md` (all approved and immutable). This ADR is about responsibility and boundary only — it names no framework, library, programming language, or implementation pattern tied to any specific technology, and none is implied by anything below. Where information needed to complete this ADR is missing from the nine sources above, it is recorded under Open Questions rather than invented.

---

# ADR-005: Application Layer Boundary and Responsibilities

## Status

**Accepted.** *(Status field corrected 2026-07-21 — this document's own Status was never updated after approval; every ADR from `ADR-013` onward, dated 2026-07-20, already lists this document among its "approved and immutable" sources, and the entire shipped implementation is built on it. Gap first identified by the Final Architecture Review Board's production-readiness audit, 2026-07-21. No content in this ADR is otherwise altered by this correction.)*

## Context

`ARCHITECTURE.md` already sketches a layered structure — Domain, Application, Infrastructure, Presentation — with inward-only dependencies (Section 6–7), and lists the Application layer's responsibilities at a high level, organized by the four bounded contexts (Section 9). `ADR-002` fixed that business logic is enforced only within the Domain layer of the bounded context that owns the resource in question, and that no context may write to another's aggregate directly. `ADR-003` fixed that business-authorization decisions belong to the Domain layer, never to Infrastructure, with Application responsible only for invoking that decision. `ADR-004` fixed that a transaction boundary never spans two bounded contexts' owned aggregates, and that cross-context effects are coordinated as separate transactions by the consuming context's Application layer. This ADR consolidates and completes those threads into a single, precise statement of what the Application layer does — and does not do — relative to Domain and Infrastructure.

## Problem Statement

Given the layering and dependency rules already fixed (`ADR-001`), the bounded-context ownership already fixed (`ADR-002`), the authorization model already fixed (`ADR-003`), and the persistence/transaction rules already fixed (`ADR-004`), what exactly is the Application layer responsible for, what is strictly excluded from it, and what rules keep business logic from leaking into it or into Infrastructure?

## Decision

The Application layer orchestrates use cases: for each use case, it resolves the caller's identity, invokes the Authorization concern (`ADR-003`), invokes Domain logic — which alone holds business rules and invariants — to carry out the requested change or query, coordinates the transaction boundary for its own bounded context's aggregate(s) (`ADR-004`), reacts to any Domain Event the Domain layer raised in order to trigger the Audit concern (CONST-2), and translates the outcome for Presentation. The Application layer owns no business rule of its own — every business rule is owned exclusively by the Domain layer of the bounded context to which it belongs (`ADR-002`). Infrastructure provides technical capability only, implementing the interfaces Application defines, with no business logic of its own (`ARCHITECTURE.md` Section 10). The dependency direction already fixed in `ARCHITECTURE.md` — Presentation → Application → Domain, with Infrastructure implementing Application's interfaces — is unchanged and is preserved by every rule in this ADR.

## Application Layer Responsibilities

- Orchestrates the use cases already identified in `ARCHITECTURE.md` Section 9, organized by bounded context (`ADR-002`):
  - **Scheduling & Booking:** book, cancel, or reschedule a Session; transition Session status. (`PRODUCT_REQUIREMENTS.md` SCH-5, SCH-6, SCH-7)
  - **Identity & Relationship:** register an account; define Tutor offering/availability; invite and confirm a Relationship. (IDR-1, SCH-3, DISC-2, IDR-4)
  - **Discovery:** search Tutors. (DISC-1)
  - **Marketplace Oversight:** approve/suspend a Tutor; view all schedules; resolve a booking conflict; manage a user account. (ADM-1 to ADM-5)
- Defines the interfaces that Infrastructure must implement: persistence, identity, and audit (`ARCHITECTURE.md` Sections 9–10).
- Invokes Domain logic to make the actual business decision or mutation; it sequences the steps needed to apply a rule, but does not decide the rule itself.
- Invokes the Authorization concern before a use case is allowed to proceed (`ADR-003`).
- Recognizes a Domain Event raised by the Domain layer and invokes the Audit concern so the event is durably recorded with identity, role, and timestamp (CONST-2; `ADR-004`: Audit Persistence Principles).
- Coordinates the transaction boundary for its own bounded context's aggregate(s); when a use case's effects span two contexts, coordinates two separate transactions rather than one cross-context transaction (`ADR-004`: Transaction Boundaries).
- Translates a Domain-level outcome (success, or an explicit business-rule rejection) into a result Presentation can act on, without adding new business meaning of its own.

## Domain Layer Responsibilities

Restated from `ARCHITECTURE.md` Section 8 and reinforced here as the exclusive home of business logic:

- Owns every Entity, Value Object, Aggregate, invariant, and Domain Event established in `DOMAIN_MODEL.md`.
- Is the exclusive place where a business rule is evaluated and enforced — including CONST-1 (no double-booking), CONST-5 (least privilege), IDR-2 through IDR-6, and SCH-4 through SCH-6 — per `ADR-002`'s rule that business logic is enforced only within its owning context's Domain layer.
- Has zero dependency on Application, Infrastructure, or Presentation (`ARCHITECTURE.md` Section 7).
- Is where any authorization decision depending on business/domain state (e.g., whether a Relationship is confirmed, whether a Tutor is approved) is actually evaluated, per `ADR-003`.

## Infrastructure Responsibilities

Restated from `ARCHITECTURE.md` Section 10 and `ADR-004`:

- Implements the interfaces Application defines: persistence for the aggregates a context owns, identity/authentication integration, and audit trail storage.
- Contains no business logic and makes no business or authorization decision of its own (`ADR-003`: Authorization Principles — business authorization is "never in Infrastructure").
- Is the only layer aware of any concrete technology, and may be replaced without changing Domain or Application (`ARCHITECTURE.md` Section 7).

## Interaction Rules

- Presentation calls Application only. Application calls Domain, and — through the interfaces it defines — Infrastructure. Domain never calls Application or Infrastructure. (`ARCHITECTURE.md` Section 7, unchanged)
- One bounded context's Application layer may call another bounded context's Application layer only through that context's own use case; it never reaches into another context's Domain or Infrastructure directly (`ADR-002`: Integration Rules).
- Application never writes to a data store directly. Persistence occurs by invoking a Domain aggregate's own behavior and then persisting the result through Infrastructure's implementation of the interfaces Application defines (`ADR-004`: Aggregate Persistence Rules).

## Use Case Boundaries

- A use case's transactional/write portion is scoped to exactly one bounded context's owned aggregate(s) (`ADR-004`: Transaction Boundaries). For example, "book a session" is a Scheduling & Booking use case operating on Session and Availability Slot; it may read — but never write — Identity & Relationship data as part of authorizing the booking (IDR-5, IDR-6).
- A use case that appears to span two contexts (for example, Marketplace Oversight's "resolve a booking conflict," which may need to change a Session) is decomposed into an orchestration step in Marketplace Oversight's own Application layer that invokes Scheduling & Booking's own use case to perform the actual Session change — preserving both `ADR-002`'s no-cross-context-write rule and `ADR-004`'s separate-transactions rule.
- Discovery's only use case, searching Tutors, is a read-only composition with no write boundary at all, since Discovery owns no aggregate (`ADR-002`: Context Ownership).

## Transaction Coordination Principles

- The Application layer demarcates the transaction boundary for its own context's use case, per `ADR-004` — for example, wrapping the check-and-consume-slot-and-create-session sequence in one atomic unit.
- The Application layer never spans a single transaction across two bounded contexts (`ADR-004`: Transaction Boundaries). Cross-context effects are two separate transactions, sequenced by the consuming context's Application layer.
- How the consuming context's Application layer should handle a partial failure — one context's transaction succeeding while the second, dependent transaction fails — is not established by any approved document (see Open Questions).
- The transaction boundary is an Application-layer (orchestration) concern; Domain objects are not aware of it, consistent with Domain having zero dependency on persistence mechanics (`ARCHITECTURE.md` Sections 7–8).

## Validation Responsibilities

- Structural/input validation (for example, confirming a required value is present) is distinct from business-rule validation (for example, whether a Relationship is confirmed). Application may perform the former before invoking Domain, but the latter is always ultimately enforced by Domain — never solely by Application (`PROJECT_CONSTITUTION.md`: Engineering Principle 1, correctness before convenience).
- Application must not implement a shortcut duplicate of a Domain invariant "for efficiency." Any such duplication risks the two falling out of sync and violates `ADR-002`'s rule that a business rule is enforced in exactly one place.

## Authorization Responsibilities

- Application is responsible for invoking the authorization check before a use case proceeds, but the authorization decision itself — for any fact dependent on business/domain state — is made in the Domain layer of the resource's owning context, never by Application itself (`ADR-003`: Authorization Principles).
- A purely role-based, coarse-grained check (for example, "is this identity a Tutor at all") may be evaluated by Application using identity/role facts read from Identity & Relationship; any check requiring evaluation of Relationship confirmation, Tutor approval state, or other aggregate state is delegated to that aggregate's own Domain logic (`ADR-003`: Authorization Principles, Cross-Context Authorization Rules).

## Domain Event Coordination

- Domain Events — TutorRegistered, TutorApproved, TutorSuspended, RelationshipInvited, RelationshipConfirmed, AvailabilityDeclared, SessionBooked, SessionRescheduled, SessionCancelled, SessionCompleted, SessionMarkedNoShow, BookingConflictResolved — are raised by the Domain layer as part of enforcing a business rule (`DOMAIN_MODEL.md`: Domain Events).
- The Application layer recognizes that a Domain Event occurred while completing a use case and coordinates the resulting cross-cutting concern — most importantly, invoking the Audit concern so the event is durably recorded with identity, role, and timestamp (CONST-2; `ADR-004`: Audit Persistence Principles).
- Whether a Domain Event must also trigger a response in a different bounded context (for example, whether SessionCancelled requires any action from Marketplace Oversight) is not established by any approved document — see Open Questions.
- The Application layer does not alter or reinterpret a Domain Event's meaning; it only reacts to what the Domain layer already determined occurred.

## Error Handling Responsibilities

- Per `ARCHITECTURE.md` Section 18 and Engineering Principle 4 (fail safely and visibly), the Application layer translates a Domain-level rule rejection (for example, attempting to book an already-consumed slot) into an explicit, handled outcome that Presentation can communicate to the acting Student, Parent/Guardian, or Tutor — it never suppresses or silently swallows such an outcome.
- The Application layer is also responsible for surfacing an unexpected Infrastructure-level failure (for example, a persistence fault during a transaction) in a way that supports an Admin/Staff member detecting and resolving it without reading code or querying a database directly (`PROJECT_CONSTITUTION.md`: Success Criteria).
- The Application layer distinguishes an expected, Domain-rule-driven rejection (for example, IDR-6: a minor without a confirmed Relationship) from an unexpected system failure; both are handled explicitly, but they are different categories of outcome.

## Alternatives Considered

- **Business rules implemented directly in Application, without a separate Domain authority** — rejected. Contradicts the Clean/layered rationale already decided in `ADR-001` and `ADR-002`'s explicit rule that business logic belongs only to the owning context's Domain layer; it would also make rules harder to verify independent of orchestration concerns (`PROJECT_CONSTITUTION.md`: Engineering Principle 2).
- **Application accessing persistence directly, without going through Infrastructure-implemented interfaces** — rejected. Violates the dependency-inversion rule already fixed in `ARCHITECTURE.md` Section 7 and would re-couple Application to a specific technology, contradicting Architecture Principle 6.
- **One bounded context's Application layer invoking another context's Domain layer directly, bypassing that context's own Application layer** — rejected. `ADR-002` requires all cross-context interaction to go through the owning context's own use case, preserving that context's own transaction, authorization, and audit coordination.
- **A single transaction spanning multiple bounded contexts for a cross-context use case** — rejected. `ADR-004`'s Transaction Boundaries explicitly forbid a transaction boundary spanning two contexts' owned aggregates.
- **Domain layer performing audit logging or authorization I/O itself** — rejected. Domain must have zero dependency on Infrastructure or cross-cutting mechanics (`ARCHITECTURE.md` Sections 7–8); invoking these cross-cutting concerns, using Domain's own decisions and events as the trigger, is Application's responsibility.

## Consequences

- Every use case is expressed as an Application-layer orchestration that authorizes (`ADR-003`), invokes Domain logic to enforce business rules, coordinates a single-context transaction (`ADR-004`), reacts to the resulting Domain Event for audit purposes, and returns a translated outcome.
- A business rule, once established, lives in exactly one place — the owning context's Domain layer — so any future change to it (for example, cancellation notice periods, once decided) is made there, not scattered across Application use cases.
- Cross-context use cases (for example, resolving a conflict) are necessarily expressed as an orchestration in the consuming context's Application layer that calls into another context's own use case, adding a small amount of orchestration complexity in exchange for preserving the boundary guarantees already fixed in `ADR-002` and `ADR-004`.

## Risks

- If an implementer places a convenience business-rule check in Application "to fail fast" without also enforcing it in Domain, the rule could end up enforced inconsistently, undermining `ADR-002`'s single-place-of-enforcement rule — this must be actively guarded against.
- How a partial failure across two coordinated context transactions should be handled is a real, unresolved gap (see Transaction Coordination Principles and Open Questions); left unaddressed, it risks one context's state advancing without a promised effect in another, threatening reliability and auditability.
- Domain Event coordination across bounded contexts is not fully specified; if implemented ad hoc, this risks quietly reintroducing the "scattered special cases in logic" the Constitution warns against (`PROJECT_CONSTITUTION.md`: Architecture Principle 4).

## Open Questions

1. How should the Application layer handle a partial failure when a use case's effects are coordinated across two separate context transactions? Not addressed by any approved document.
2. Do any Domain Events need to trigger a response in a bounded context other than the one that raised them (for example, does SessionCancelled require any action from Marketplace Oversight)? Not addressed by any approved document.
3. Which role(s) may transition a Session to Completed or No-Show, affecting which use case's authorization rule applies at that step? (`DOMAIN_MODEL.md` Open Question 6)
4. What are the cancellation/rescheduling notice-period rules that the cancel/reschedule use case must enforce? (`DOMAIN_MODEL.md` Open Question 5)
5. What specific mechanics does the "resolve booking conflicts" use case include? (`DOMAIN_MODEL.md` Open Question 13)
6. Does cancelling a Session automatically reopen its Availability Slot, affecting the cancellation use case's Domain-layer outcome? (`DOMAIN_MODEL.md` Open Question 7)
7. Must a denied authorization attempt be treated as an auditable outcome by the Application layer's error handling? (`ADR-003` Open Question 6)

## Traceability

| Element of this ADR | Source |
|---|---|
| Layered structure and inward dependency direction | `ARCHITECTURE.md` Sections 6–7 |
| Application layer use cases per bounded context | `ARCHITECTURE.md` Section 9; `PRODUCT_REQUIREMENTS.md` Section 6 |
| Business logic confined to owning context's Domain layer | `ADR-002`: Rules That Prevent Business Logic Leakage |
| No cross-context writes; cross-context calls via owning context's own use case | `ADR-002`: Integration Rules |
| Business-authorization decisions made in Domain, not Infrastructure | `ADR-003`: Authorization Principles |
| Transaction boundary never spans two contexts | `ADR-004`: Transaction Boundaries |
| Audit concern triggered by Domain Events | `PRODUCT_REQUIREMENTS.md` CONST-2; `ADR-004`: Audit Persistence Principles |
| Fail safely and visibly | `PROJECT_CONSTITUTION.md`: Engineering Principle 4; `ARCHITECTURE.md` Section 18 |
| Domain Events enumerated | `DOMAIN_MODEL.md`: Domain Events |
| Correctness before convenience (validation) | `PROJECT_CONSTITUTION.md`: Engineering Principle 1 |

---

*Status: Accepted. Status field corrected 2026-07-21 to match how every later ADR and the shipped implementation already treat this document — see the Status section above.*
