**Authoritative sources (in priority order):** `PROJECT_CONSTITUTION.md`, `PRODUCT_REQUIREMENTS.md`, `BUSINESS_MODEL.md`, `DOMAIN_MODEL.md`, `ARCHITECTURE.md`, `docs/adr/ADR-001-architecture-style.md`, `docs/adr/ADR-002-domain-boundaries.md`, `docs/adr/ADR-003-authentication-and-authorization.md`, `docs/adr/ADR-004-persistence-strategy.md`, `docs/adr/ADR-005-application-boundary.md`, `docs/adr/ADR-006-domain-events.md`, `docs/adr/ADR-007-validation-strategy.md`, `docs/adr/ADR-008-error-handling.md`, `docs/adr/ADR-009-audit-and-observability.md` (all approved and immutable). This ADR defines architectural principle only — it defines no endpoint, route, request/response schema, or contract-description format, and names no transport style, data format, or implementation technology. Where information needed to complete this ADR is missing from the fourteen sources above, it is recorded under Open Questions rather than invented.

---

# ADR-010: API Boundary Principles

## Status

**Accepted.** *(Status field corrected 2026-07-21 — this document's own Status was never updated after approval; every ADR from `ADR-013` onward, dated 2026-07-20, already lists this document among its "approved and immutable" sources, and the entire shipped implementation is built on it. Gap first identified by the Final Architecture Review Board's production-readiness audit, 2026-07-21. No content in this ADR is otherwise altered by this correction.)*

## Context

`ARCHITECTURE.md` already establishes that Presentation depends only on Application and exposes no business rule of its own (Section 11), and that the concrete shape of Presentation, and the mechanism by which contexts communicate internally, are deferred Architectural Decision Candidates (Section 21, Items 6–7). `ADR-005` already fixed that the Application layer orchestrates use cases, defines the interfaces Infrastructure implements, and is the only layer Presentation calls. `ADR-002` fixed exclusive bounded-context ownership and forbade any cross-context write or bypass of an owning context's own logic. `ADR-003`, `ADR-007`, `ADR-008`, and `ADR-009` fixed how authentication/authorization, validation, error classification, and audit apply to any use case invocation. This ADR names and governs the externally invokable surface — the boundary through which a use case is reached from outside the system's own layering — without designing its concrete shape.

## Problem Statement

Given the layered architecture (`ADR-001`), bounded-context ownership (`ADR-002`), and Application-layer responsibilities already fixed (`ADR-005`), what are the architectural rules governing the boundary through which any external caller invokes a use case — what it may expose, what it must never expose, how it respects context ownership, and how it relates to the authentication/authorization, validation, error handling, and audit rules already fixed — without defining any endpoint, route, schema, or transport technology?

## Decision

The externally invokable surface — this ADR's "boundary" — exposes exactly the use cases already defined at the Application layer (`ADR-005`), organized by the four bounded contexts (`ADR-002`). It never exposes a Domain object, a Domain-layer operation, or an Infrastructure capability directly. A Domain object belonging to one bounded context is never directly reachable through the boundary of another context, nor exposed in a form that lets a caller bypass the owning context's own Application-layer use case. This boundary is the same one already fixed between Presentation and Application (`ARCHITECTURE.md` Sections 7, 11); this ADR does not introduce a new layer, only names and governs the surface at that existing boundary. Authentication and authorization (`ADR-003`), validation (`ADR-007`), error classification (`ADR-008`), and audit (`ADR-009`) all apply at this boundary exactly as already established — this ADR does not restate their substance, only confirms they govern everything that crosses it. No contract shape, transport mechanism, or technology is chosen here.

## API Boundary Principles

- The boundary exposes Application use cases only, never Domain internals or Infrastructure capabilities (`ARCHITECTURE.md` Section 7: dependency rule; `ADR-005`: Application Layer Responsibilities).
- Everything crossing the boundary corresponds to a use case already identified in `ARCHITECTURE.md` Section 9 and `ADR-005`, organized by the four bounded contexts (`ADR-002`); no new use case is invented at the boundary.
- The boundary is a single conceptual surface serving all four Domain Actors — Student, Tutor, Parent/Guardian, Admin/Staff — consistent with v1 being delivered as a single web-based surface (`PRODUCT_REQUIREMENTS.md` PLAT-1; `ARCHITECTURE.md` Section 11).
- No business rule lives at the boundary; the boundary only grants access to use cases whose business correctness is guaranteed by Domain (`ADR-002`; `ADR-007`: Domain Validation Responsibilities).

## Public vs Internal Interface Principles

- A "public" interface is the surface through which Presentation — and, if ever approved, another external caller — invokes an Application use case. It is public relative to the system's own internal layering; whether it is exposed beyond Presentation is not established (see Open Questions).
- An "internal" interface is any interface Application defines for Infrastructure to implement (`ADR-005`), or any interface one context's Application layer uses to call another context's own use case (`ADR-002`: Integration Rules). Internal interfaces are never exposed as part of the externally invokable surface.
- Discovery, owning no aggregate (`ADR-002`: Context Ownership), exposes only its read-only search use case at the boundary; it has no internal aggregate-level interface to keep internal in the first place.
- Marketplace Oversight's boundary exposes only its own use cases — approve/suspend a Tutor, view all schedules, resolve a conflict, manage an account (`PRODUCT_REQUIREMENTS.md` ADM-1 to ADM-5) — never a shortcut directly into Scheduling & Booking's or Identity & Relationship's own aggregates, consistent with `ADR-005`'s Use Case Boundaries.

## Application Boundary Exposure Rules

- Only a bounded context's own Application layer may expose that context's use cases at the boundary; no context exposes another context's use case on its behalf (`ADR-002`: Context Ownership; `ADR-005`: Use Case Boundaries).
- What crosses the boundary as input and outcome is exactly what the Application layer's use case already defines as its request and result; the boundary does not add, remove, or reinterpret meaning beyond what Application already translates (`ADR-005`: Application translates an outcome "without adding new business meaning of its own").
- A Domain object is never returned across the boundary in a form that would let a caller mutate it directly or bypass its owning context's own use case; whatever is exposed is Application's own translated representation of the outcome, not the Domain object itself (`ADR-002`: Rules That Prevent Business Logic Leakage, applied here to the outward-facing surface).

## Domain Isolation Rules

- Domain has zero dependency on Presentation, Infrastructure, or the boundary surface itself (`ARCHITECTURE.md` Sections 7–8); nothing at the boundary calls into Domain directly, bypassing Application.
- A Domain Event (`ADR-006`) is an internal architectural concept, recognized by Application to trigger cross-cutting concerns such as Audit (`ADR-006`; `ADR-009`). This ADR does not establish that a Domain Event is itself exposed at the boundary, and no approved document requires that it be (see Open Questions).
- Domain invariants (`DOMAIN_MODEL.md`: Invariants) remain enforced entirely within Domain regardless of what the boundary exposes; the boundary cannot weaken or bypass them by exposing a different path into the same aggregate.

## Cross-Context Communication Rules

- The same rule already fixed for cross-context calls in general (`ADR-002`: Integration Rules; `ADR-005`: Interaction Rules) applies at the boundary: one bounded context's boundary surface never directly exposes another context's Domain or Infrastructure. If a use case needs another context's effect, that need is satisfied by the consuming context's own Application layer calling the other context's own use case, not by the boundary routing a caller directly into it.
- The specific mechanism for this internal, cross-context communication is not decided here; it remains the open Architectural Decision Candidate already listed in `ARCHITECTURE.md` Section 21, Item 6.
- Discovery's boundary use case (search Tutors) internally composes reads from Identity & Relationship and Scheduling & Booking (`ADR-002`: Context Relationships), but this composition happens inside Discovery's own Application layer, before anything is returned across the boundary — the boundary itself never exposes the two source contexts separately for a caller to assemble.

## Contract Stability Principles

- Whatever is exposed at the boundary for a given use case is expected to remain stable enough that Presentation, and any future caller, can rely on it without being coupled to Domain's internal representation, since Domain and the boundary's exposed shape are different concerns by design (`ARCHITECTURE.md` Sections 6–7: dependency inversion, technology deferral).
- A change to a Domain object's internal shape — for example, adding an attribute needed only to enforce an invariant — does not, by itself, require a change to what the boundary exposes, unless that information is meant to be visible to the caller; this separation is precisely what the layered architecture exists to provide (`docs/adr/ADR-001-architecture-style.md`: Decision).
- No approved document establishes a formal contract-stability or backward-compatibility policy (for example, how long a prior exposed shape must remain supported); this is an Open Question, not assumed here.

## Versioning Principles

- No approved document establishes a requirement for multiple concurrent contract versions, a deprecation policy, or a versioning scheme; the approved documents describe TutorFlow v1 as a single, current system, not one required to support multiple simultaneous contract generations.
- How a transition is managed if a future change to a use case's exposed shape would break an existing caller is not established by any approved document — this is an Open Question, and this ADR does not invent a versioning policy to fill it.
- Whatever versioning approach is eventually adopted, it must not violate the ownership, isolation, or single-source-of-truth rules already fixed (`ADR-002`, `ADR-004`); a versioning mechanism is not license to duplicate or bypass those.

## Security Boundary Principles

- Every use case exposed at the boundary is subject to the authentication and authorization model already fixed in `ADR-003`: an identity is established before a use case is invoked, and the authorization decision for any business/domain-state-dependent fact is made in the Domain layer of the resource's owning context, never at the boundary itself.
- The boundary is where least privilege (CONST-5) first takes effect for an external caller: a caller reaches only the use cases relevant to their role, not an undifferentiated set of every use case in the system (`ADR-003`: Authorization Principles, Permission Model).
- The boundary must not become a place where authorization is re-implemented or duplicated outside Domain, echoing `ADR-003`'s rule that business authorization decisions belong to Domain, not Infrastructure — nor, by extension, to the boundary surface itself.

## Error Boundary Principles

- A Domain Error and an Infrastructure Failure, as classified in `ADR-008`, cross the boundary only in their already-translated form, produced by Application (`ADR-008`: Application Error Responsibilities); the boundary does not re-classify or reinterpret them.
- The boundary communicates a Domain Error as the specific, business-meaningful explanation Application already produced, and an Infrastructure Failure as the general notice Application already produced, consistent with `ADR-008`'s User Communication Principles; it adds no new detail and suppresses nothing Application already decided to convey.
- Unresolved sub-questions already carried in `ADR-008` (for example, how a cross-context partial failure should be handled) remain unresolved here too; this ADR does not attempt to resolve them at the boundary level, since the boundary only carries whatever outcome Application produces.

## Alternatives Considered

- **Exposing Domain objects or Domain-layer operations directly at the boundary, bypassing Application** — rejected. This would violate the dependency rule already fixed in `ARCHITECTURE.md` Section 7 and let a caller reach business logic without the orchestration, authorization, audit, and transaction coordination Application is responsible for (`ADR-005`).
- **A single, undifferentiated boundary that does not respect bounded-context ownership** — rejected. Mixing Scheduling & Booking's and Identity & Relationship's use cases at the boundary without attribution to their owning context would contradict `ADR-002`'s exclusive-ownership model and obscure accountability for a given exposed use case.
- **Allowing the boundary to perform its own authorization or business validation independent of Domain** — rejected, per `ADR-003`'s and `ADR-007`'s rule that such decisions belong to Domain, never to a layer outside it.
- **The boundary as a thin, technology-neutral surface exposing exactly the Application use cases already defined, organized by bounded context, with no independent business logic of its own (chosen)** — the only approach consistent with `ADR-001`, `ADR-002`, and `ADR-005` as already approved.

## Consequences

- Every use case ever exposed externally is traceable to an existing Application-layer use case and, through it, to a specific approved Functional Requirement (`PRODUCT_REQUIREMENTS.md` Section 6); nothing is exposed that was not already approved as a use case.
- Presentation, and any future caller, is insulated from Domain's internal representation, since the boundary is Application's own translated contract, not a direct view into Domain.
- Because contract-stability and versioning policies are unresolved, any future change to what a use case exposes carries an unquantified risk of breaking a caller until those policies are established.

## Risks

- If an implementer exposes a Domain object directly at the boundary "for convenience," this would violate this ADR's core rule and `ARCHITECTURE.md`'s dependency rule, and could let a caller bypass business-rule enforcement.
- Without an established contract-stability or versioning policy, incremental changes to a use case's exposed shape could unintentionally break Presentation or any future caller.
- The undecided inter-context communication mechanism (`ARCHITECTURE.md` Section 21, Item 6) and undecided Presentation shape (Item 7) mean the concrete form the boundary will take is not yet fully specified, even though its governing principles are fixed here.

## Future Evolution

If a future caller beyond Presentation is approved — for example, an integration partner — it must be authenticated, authorized, and use-case-scoped exactly as Presentation is today, per the rules established here, never granted broader access by default. If a contract-stability or versioning policy is later needed, it must be proposed and approved through the Constitution's Decision-Making Process, and must not be permitted to violate the ownership or isolation rules already fixed. The specific contract-shape technology and the inter-context communication mechanism remain open Architectural Decision Candidates (`ARCHITECTURE.md` Section 21, Items 6–7) to be resolved separately.

## Open Questions

1. Who, besides Presentation, is permitted to call the boundary — is any external integration ever in scope, now or later? Not addressed by any approved document.
2. Is a Domain Event ever exposed at the boundary (for example, for a future integration to observe), or is it strictly an internal architectural concept? Not addressed by any approved document.
3. What contract-stability or backward-compatibility policy applies when a use case's exposed shape changes? Not addressed by any approved document.
4. What versioning approach, if any, applies to the boundary? Not addressed by any approved document.
5. What is the specific Presentation shape and inter-context communication mechanism that will give this ADR's principles concrete form? Remain open Architectural Decision Candidates. (`ARCHITECTURE.md` Section 21, Items 6–7)

## Traceability

| Element of this ADR | Source |
|---|---|
| Boundary exposes Application use cases only | `ARCHITECTURE.md` Section 7; `ADR-005`: Application Layer Responsibilities |
| Presentation depends only on Application, holds no business rules | `ARCHITECTURE.md` Section 11 |
| Bounded-context ownership governs what a context may expose | `ADR-002`: Context Ownership, Integration Rules |
| Use cases already identified per context | `ARCHITECTURE.md` Section 9; `PRODUCT_REQUIREMENTS.md` Section 6 |
| Authentication/authorization applies at the boundary | `ADR-003`: Authentication Principles, Authorization Principles |
| Validation ultimately enforced by Domain, not the boundary | `ADR-007`: Domain Validation Responsibilities |
| Error classification and translation by Application | `ADR-008`: Application Error Responsibilities, User Communication Principles |
| Audit requirements apply regardless of boundary shape | `ADR-009`: Audit Principles |
| Deferred Presentation shape and inter-context mechanism | `ARCHITECTURE.md` Section 21, Items 6–7 |
| Single web-based surface for v1 | `PRODUCT_REQUIREMENTS.md` PLAT-1 |

---

*Status: Accepted. Status field corrected 2026-07-21 to match how every later ADR and the shipped implementation already treat this document — see the Status section above.*
