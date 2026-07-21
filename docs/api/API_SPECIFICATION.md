**Authoritative sources (in priority order):** `PROJECT_CONSTITUTION.md`, `PRODUCT_REQUIREMENTS.md`, `BUSINESS_MODEL.md`, `DOMAIN_MODEL.md`, `ARCHITECTURE.md`, `docs/database/DOMAIN_DATA_MODEL.md`, `docs/database/PHYSICAL_DATABASE_STRATEGY.md`, and all approved ADRs (`docs/adr/ADR-001` through `ADR-010`). This is a logical API specification, not an OpenAPI document — it defines no endpoint, route, HTTP method, JSON schema, or request/response payload, and names no transport protocol or implementation technology. Where information needed to complete this document is missing from the approved sources, it is recorded under Open Questions rather than invented.

---

# 1. Purpose

This document catalogs the capabilities exposed at the boundary already established in `ADR-010` — what is exposed, to whom, under what input/output/error/authorization/audit principles — as a logical specification independent of any transport, protocol, or contract-description technology. It exists to give anyone later designing a concrete contract a complete, technology-neutral reference of what must be exposed and what rules already govern that exposure.

# 2. Scope

**In scope:** the catalog of Application use cases as logical capabilities, their consumers, the bounded-context boundaries they belong to, and the input, output, error, authorization, audit, versioning, stability, and compatibility principles that already govern them per the approved ADRs.

**In scope, restated, not redefined:** validation (`ADR-007`), authorization (`ADR-003`), error handling (`ADR-008`), audit (`ADR-009`), and the boundary rules already fixed in `ADR-010` — this document applies them to the specific catalog of capabilities, it does not change them.

**Out of scope:** any endpoint, route, HTTP method, transport protocol, request/response schema, payload shape, or contract-description format (for example, an OpenAPI document). These remain deferred Architectural Decision Candidates (`ARCHITECTURE.md` Section 21, Items 6–7).

# 3. API Consumers

The only consumer established by any approved document is Presentation, serving the four Domain Actors — Student, Tutor, Parent/Guardian, and Admin/Staff — through the single web-based surface required for v1 (`PRODUCT_REQUIREMENTS.md` PLAT-1; `ARCHITECTURE.md` Section 11). Whether any consumer beyond Presentation (for example, a future external integration) is ever in scope is not established (`ADR-010` Open Question 1) and is carried forward here rather than assumed.

# 4. Application Use Cases

The complete set of use cases already identified at the Application layer (`ARCHITECTURE.md` Section 9; `ADR-005`), organized by bounded context:

| Bounded Context | Use Case | Source |
|---|---|---|
| Scheduling & Booking | Book a Session | `PRODUCT_REQUIREMENTS.md` SCH-5 |
| Scheduling & Booking | Cancel a Session | SCH-7 |
| Scheduling & Booking | Reschedule a Session | SCH-7 |
| Scheduling & Booking | Transition Session status | SCH-6 |
| Identity & Relationship | Register an account | IDR-1 |
| Identity & Relationship | Define Tutor offering (duration, rate) and availability | SCH-3, DISC-2 |
| Identity & Relationship | Invite a Parent-Student Relationship | IDR-4 |
| Identity & Relationship | Confirm a Parent-Student Relationship | IDR-4 |
| Discovery | Search Tutors | DISC-1 |
| Marketplace Oversight | Approve a Tutor | ADM-1 |
| Marketplace Oversight | Suspend a Tutor | ADM-2 |
| Marketplace Oversight | View all schedules | ADM-3 |
| Marketplace Oversight | Resolve a booking conflict | ADM-4 |
| Marketplace Oversight | Manage a user account | ADM-5 |

No use case beyond this list is established by any approved document; none is invented here.

# 5. Service Boundaries

Each use case belongs to exactly one of the four bounded contexts already fixed in `ADR-002` and `ADR-010`, and is exposed only by that context's own Application layer:

- **Scheduling & Booking** exposes its own Session and Availability use cases only.
- **Identity & Relationship** exposes its own registration, Tutor-offering, and Relationship use cases only.
- **Discovery** exposes only its read-only search use case, since it owns no aggregate (`ADR-002`: Context Ownership).
- **Marketplace Oversight** exposes only its own oversight use cases; it never exposes a shortcut directly into Scheduling & Booking's or Identity & Relationship's own aggregates (`ADR-005`: Use Case Boundaries; `ADR-010`: Public vs Internal Interface Principles).

No context exposes a capability on behalf of another context (`ADR-002`: Context Ownership; `ADR-010`: Application Boundary Exposure Rules).

# 6. Public Capabilities

The capabilities in Section 4 are exactly what is exposed at the boundary (`ADR-010`: Decision — "the boundary exposes exactly the use cases already defined at the Application layer"). Framed by consumer:

| Domain Actor | Capabilities Available |
|---|---|
| Student | Register; search Tutors; book, cancel, or reschedule a Session (subject to IDR-5/IDR-6) |
| Tutor | Register; define offering and availability; cancel or reschedule a Session |
| Parent/Guardian | Register; invite/confirm a Relationship; search Tutors on a linked Student's behalf; book, cancel, or reschedule a Session on a linked Student's behalf |
| Admin/Staff | Approve/suspend a Tutor; view all schedules; resolve a booking conflict; manage a user account; cancel or reschedule a Session |

No capability is available to a Domain Actor beyond what `PRODUCT_REQUIREMENTS.md` Section 6 already establishes for their role (`ADR-003`: Permission Model).

# 7. Internal Capabilities

The following are never exposed at the boundary; they exist only for internal coordination:

- Interfaces Application defines for Infrastructure to implement — persistence, identity, and audit (`ARCHITECTURE.md` Sections 9–10; `ADR-005`).
- Interfaces one context's Application layer uses to call another context's own use case — for example, Scheduling & Booking reading Identity & Relationship's Relationship-confirmation fact when authorizing a booking, or Marketplace Oversight invoking Scheduling & Booking's own use case when resolving a conflict (`ADR-002`: Integration Rules; `ADR-005`: Use Case Boundaries; `ADR-010`: Public vs Internal Interface Principles).
- Domain-layer operations and Domain objects themselves, which never cross the boundary in raw form (`ADR-002`: Rules That Prevent Business Logic Leakage; `ADR-010`: Domain Isolation Rules).

# 8. Input Principles

- What a capability accepts as input is exactly what its Application-layer use case already defines as its request; the boundary adds, removes, or reinterprets nothing (`ADR-010`: Application Boundary Exposure Rules).
- Structural/input validation (for example, confirming a required reference is present) may occur before Domain is invoked; business-rule validation is always ultimately enforced by Domain, never solely at input time (`ADR-007`: Application Validation Responsibilities).
- An input never includes a raw Domain object from another context; where a capability's input logically depends on another context's data (for example, which Availability Slot to book), it references that data by identity only (`ADR-002`: Integration Rules; `docs/database/DOMAIN_DATA_MODEL.md` Section 6: Identity Strategy).

# 9. Output Principles

- What a capability returns is Application's own translated representation of the outcome — success or an explicit rejection — never the Domain object itself (`ADR-005`: Application Layer Responsibilities; `ADR-010`: Application Boundary Exposure Rules).
- A Domain object belonging to one bounded context never leaves that context in raw form, whether returned directly to a consumer or passed to another context (`ADR-002`: Rules That Prevent Business Logic Leakage; `ADR-010`: Domain Isolation Rules).
- Output never carries more information than the consuming Domain Actor is authorized to see, consistent with least privilege (CONST-5; `ADR-003`: Authorization Principles).

# 10. Error Contract Principles

- Every capability's outcome is exactly one of the two categories already fixed in `ADR-008`: a **Domain Error** (an expected, business-meaningful rejection) or an **Infrastructure Failure** (an unexpected technical failure carrying no business meaning) — never an undifferentiated third kind.
- A Domain Error crosses the boundary as the specific, business-meaningful explanation Application already produced; an Infrastructure Failure crosses as the general notice Application already produced (`ADR-008`: User Communication Principles; `ADR-010`: Error Boundary Principles).
- The boundary neither re-classifies nor reinterprets either category; it only carries what Application has already decided (`ADR-010`: Error Boundary Principles).
- Unresolved sub-questions already carried in `ADR-008` (for example, how a cross-context partial failure is handled) remain unresolved for this catalog too; this document does not resolve them.

# 11. Authorization Contract Principles

- Every capability requires an established identity before it may be invoked; authentication and authorization are distinct, per `ADR-003` — authentication establishes identity, authorization decides permission.
- The authorization decision for any fact dependent on business/domain state (for example, whether a Relationship is confirmed, or a Tutor is approved) is made in the Domain layer of the resource's owning context, never at the boundary itself (`ADR-003`: Authorization Principles; `ADR-010`: Security Boundary Principles).
- A Domain Actor can only reach the capabilities relevant to their role, per the Permission Model already fixed in `ADR-003` and restated in Section 6 above — least privilege is given effect at the boundary by never exposing an irrelevant capability to a caller, not by relying on the caller not to attempt it.

# 12. Audit Requirements

- Every capability corresponding to a CONST-2-covered action — booking, rescheduling, or cancelling a Session, or changing Availability — results in a durable audit entry capturing identity, role, action, and timestamp, regardless of which consumer invoked it (`PRODUCT_REQUIREMENTS.md` CONST-2; `ADR-009`: Business Event Audit Rules).
- Approving or suspending a Tutor carries the same audit standard (`ARCHITECTURE.md` Section 16; `ADR-009`).
- Audit ownership follows the same bounded-context ownership as the capability itself; a capability never produces an audit entry attributed to a context it does not belong to (`ADR-009`: Cross-Context Audit Rules).
- Whether a denied authorization attempt, or a Domain Error rejection, must independently produce an audit entry remains unresolved (`ADR-003` Open Question 6; `ADR-008` Open Question 2; `ADR-009` Open Questions 1–2) and is not decided here.

# 13. Versioning Principles

No approved document establishes a requirement for multiple concurrent contract versions or a deprecation policy; TutorFlow v1 is described throughout the approved documents as a single, current system (`ADR-010`: Versioning Principles). How a future change to a capability's exposed shape that would break an existing consumer should be managed is not established — this remains an Open Question, and this document does not invent a versioning policy to fill it.

# 14. Stability Principles

What is exposed for a given capability is expected to remain decoupled from Domain's internal representation, since Domain and the exposed contract are different concerns by design (`ARCHITECTURE.md` Sections 6–7; `ADR-010`: Contract Stability Principles). A change to a Domain object's internal shape does not, by itself, require a change to what a capability exposes, unless that information is meant to be visible to the consumer. No formal contract-stability or backward-compatibility policy is established by any approved document; this is an Open Question.

# 15. Compatibility Principles

- **Consumer compatibility:** because no contract-stability policy is established (Section 14), no compatibility guarantee toward Presentation, or any future consumer, is established either — this is the same unresolved gap, viewed from the consumer's perspective, and is not decided here.
- **Cross-context compatibility:** when one context's Application layer invokes another context's own use case internally (`ADR-005`: Use Case Boundaries; Section 7 above), a change to the invoked use case's input or outcome must not silently break the invoking context's orchestration — this follows directly from `ADR-002`'s and `ADR-005`'s existing rule that a consuming context relies on the owning context's use case as its sole path to that context's data and behavior.
- **External-consumer compatibility** is not addressed by any approved document, since no consumer beyond Presentation is established (Section 3; `ADR-010` Open Question 1).

# 16. Future Expansion

If a consumer beyond Presentation is ever approved, it must be authenticated, authorized, and use-case-scoped exactly as Presentation is today (`ADR-010`: Future Evolution) — it gains no broader access by default, and no new capability is exposed to it beyond what Section 4 already lists unless a new use case is separately approved. If a new bounded context is introduced (per `ADR-002`'s Future Evolution), it exposes its own capabilities the same way established contexts do here. The specific contract-shape technology and inter-context communication mechanism remain deferred Architectural Decision Candidates (`ARCHITECTURE.md` Section 21, Items 6–7).

# 17. Risks

- If an implementer exposes a Domain object directly for convenience, this would violate Section 7's and `ADR-010`'s Domain Isolation Rules, letting a caller bypass business-rule enforcement.
- Several capabilities in Section 4 have unresolved business-rule specifics (for example, cancellation notice periods, who may transition a Session to Completed/No-Show, and the exact mechanics of resolving a booking conflict) — the capability's existence is established, but its full behavior is not; implementation must not fill these gaps by invention.
- Without an established contract-stability or versioning policy, any future change to a capability's exposed shape carries an unquantified risk of breaking Presentation or a future consumer.
- Without an established cross-context compatibility discipline beyond the general rule in Section 15, an internal change to one context's use case could unintentionally break another context's orchestration if not reviewed carefully.

# 18. Open Questions

1. Who, besides Presentation, is permitted to invoke a capability — is any external consumer ever in scope? (`ADR-010` Open Question 1)
2. Is a Domain Event ever exposed as part of a capability's output, or is it strictly internal? (`ADR-010` Open Question 2)
3. What contract-stability or backward-compatibility policy applies when a capability's exposed shape changes? (`ADR-010` Open Question 3)
4. What versioning approach, if any, applies to these capabilities? (`ADR-010` Open Question 4)
5. What is the specific Presentation shape and inter-context communication mechanism that will give these capabilities concrete form? (`ARCHITECTURE.md` Section 21, Items 6–7)
6. Which role(s) may invoke the "transition Session status" capability for Completed or No-Show? (`DOMAIN_MODEL.md` Open Question 6)
7. What are the cancellation/rescheduling notice-period rules the cancel/reschedule capabilities must enforce? (`DOMAIN_MODEL.md` Open Question 5)
8. What specific mechanics does the "resolve a booking conflict" capability include? (`DOMAIN_MODEL.md` Open Question 13)
9. What credentials or information does the "register an account" capability collect for a Tutor's approval, and how is an Admin/Staff account created in the first place? (`DOMAIN_MODEL.md` Open Questions 4, 15)
10. What is the exact age threshold the "book a Session" capability must apply to distinguish an adult Student from a minor Student? (`DOMAIN_MODEL.md` Open Question 1)

# 19. Traceability

| Element of this Document | Source |
|---|---|
| The capability catalog itself | `ARCHITECTURE.md` Section 9; `ADR-005`: Application Layer Responsibilities |
| Service boundaries by bounded context | `ADR-002`: Context Ownership; `ADR-010`: Service exposure rules |
| Public vs. internal distinction | `ADR-010`: Public vs Internal Interface Principles |
| Input/output translation, no raw Domain objects | `ADR-005`; `ADR-010`: Application Boundary Exposure Rules, Domain Isolation Rules |
| Validation split between structural and business-rule checks | `ADR-007`: Application Validation Responsibilities |
| Error classification and boundary carriage | `ADR-008`: Error Classification Principles; `ADR-010`: Error Boundary Principles |
| Authentication/authorization at the boundary | `ADR-003`: Authentication Principles, Authorization Principles; `ADR-010`: Security Boundary Principles |
| Audit coverage per capability | `PRODUCT_REQUIREMENTS.md` CONST-2; `ADR-009` |
| Versioning and stability gaps | `ADR-010`: Versioning Principles, Contract Stability Principles |
| Deferred contract-shape and inter-context mechanism | `ARCHITECTURE.md` Section 21, Items 6–7 |

# 20. Definition of Done

Consistent with `PROJECT_CONSTITUTION.md`'s own Definition of Done, a capability in this catalog is ready to move from "specified" to "implementable" only when:

1. It traces to an approved Functional Requirement in `PRODUCT_REQUIREMENTS.md` (`PROJECT_CONSTITUTION.md`: Definition of Done, Item 1).
2. Every input, output, error, authorization, and audit principle governing it in Sections 8–12 above is satisfied, with no unstated assumption filling a gap that should instead be an Open Question (`PROJECT_CONSTITUTION.md`: Definition of Done, Item 2).
3. Any Open Question in Section 18 that affects the capability's specific behavior has been explicitly resolved, not assumed, before that capability is implemented.
4. It does not contradict any approved document, including `ADR-002` (context ownership), `ADR-007` (validation), `ADR-008` (error handling), or `ADR-009` (audit) (`PROJECT_CONSTITUTION.md`: Definition of Done, Item 4).
5. It has been presented to, and explicitly approved by, the user before any concrete contract or implementation work begins (`PROJECT_CONSTITUTION.md`: Definition of Done, Item 3; Development Workflow).

---

*Status: Draft — pending user approval. No further documents or code are to be created until this document is approved.*
