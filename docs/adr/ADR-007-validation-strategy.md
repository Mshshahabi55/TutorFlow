**Authoritative sources (in priority order):** `PROJECT_CONSTITUTION.md`, `PRODUCT_REQUIREMENTS.md`, `BUSINESS_MODEL.md`, `DOMAIN_MODEL.md`, `ARCHITECTURE.md`, `docs/adr/ADR-001-architecture-style.md`, `docs/adr/ADR-002-domain-boundaries.md`, `docs/adr/ADR-003-authentication-and-authorization.md`, `docs/adr/ADR-004-persistence-strategy.md`, `docs/adr/ADR-005-application-boundary.md`, `docs/adr/ADR-006-domain-events.md` (all approved and immutable). This ADR defines responsibility boundaries and principles only — it names no validation library, framework, or programming language, and none is implied by anything below. Where information needed to complete this ADR is missing from the eleven sources above, it is recorded under Open Questions rather than invented.

---

# ADR-007: Validation Strategy

## Status

**Accepted.** *(Status field corrected 2026-07-21 — this document's own Status was never updated after approval; every ADR from `ADR-013` onward, dated 2026-07-20, already lists this document among its "approved and immutable" sources, and the entire shipped implementation is built on it. Gap first identified by the Final Architecture Review Board's production-readiness audit, 2026-07-21. No content in this ADR is otherwise altered by this correction.)*

## Context

`ARCHITECTURE.md` already lists Validation as a cross-cutting concern driven by Engineering Principle 1 (correctness before convenience) (Section 12), and fixes that the Domain layer enforces every invariant (Section 8). `ADR-005` already drew a first distinction: structural/input validation may be performed by Application before invoking Domain, but business-rule validation is always ultimately enforced by Domain, never solely by Application — and Application must never implement a shortcut duplicate of a Domain invariant. `ADR-002` requires every business rule to be enforced in exactly one place, the owning context's Domain layer. `ADR-003` established the authorization model as a related but distinct concern. `ADR-004` fixed that Domain validation must succeed before persistence, and that a concurrency conflict must fail explicitly. `ADR-006` fixed that a Domain Event is raised only after Domain validation has already succeeded. This ADR consolidates these threads into one complete validation strategy spanning Presentation, Application, and Domain.

## Problem Statement

Given the layered architecture (`ADR-001`), the bounded-context ownership and aggregate boundaries (`ADR-002`), and the structural-versus-business-rule distinction `ADR-005` already drew, what exactly does each layer validate, how are the eight Domain invariants enforced without duplication, how does validation relate to authorization and persistence, and what happens when a validation fails — without naming any implementation technology?

## Decision

Validation is layered, never duplicated as independent copies of the same rule. Presentation performs convenience-level checks only and has no authority to reject a request finally. Application coordinates structural, use-case-level requirements needed to even attempt a use case, but never decides a business-rule outcome itself. Domain is the sole and final authority on every business rule and invariant; nothing is considered valid until Domain has confirmed it, regardless of what earlier layers already checked. Every one of the eight invariants named in `DOMAIN_MODEL.md` is enforced exclusively by the Domain layer of its owning bounded context (`ADR-002`); no other layer or context independently re-decides them. A failed validation, at any layer, always produces an explicit, visible outcome — never a silent failure.

## Validation Principles

- **Correctness before convenience:** validation exists to guarantee correctness; any convenience-layer check is explicitly secondary and non-authoritative. (`PROJECT_CONSTITUTION.md`: Engineering Principle 1)
- **Single place of enforcement:** a business rule or invariant is validated in exactly one place — the owning context's Domain layer — never duplicated as an independent copy elsewhere. (`ADR-002`: Rules That Prevent Business Logic Leakage; `ADR-005`: Validation Responsibilities)
- **Fail safely and visibly:** a validation failure is explicit and detectable, never silently dropped or silently "corrected." (`PROJECT_CONSTITUTION.md`: Engineering Principle 4)
- **Role-aware, not role-hardcoded:** validation rules depending on role or relationship state are expressed as data/rules within Domain, not as scattered special-case checks across layers. (`PROJECT_CONSTITUTION.md`: Architecture Principle 4)
- **Least privilege:** validating what an identity may even attempt is bound to the authorization model already fixed in `ADR-003`, not treated as a disconnected concern. (`PRODUCT_REQUIREMENTS.md` CONST-5)

## Presentation Validation Responsibilities

- Presentation may perform convenience validation only — for example, indicating to a user that a required value appears missing before they submit — solely to improve the experience of the four Domain Actors carrying out their documented User Journeys (`PRODUCT_REQUIREMENTS.md` Section 5).
- Presentation validation is never authoritative: passing a Presentation-level check never guarantees a use case will succeed, and failing one is never the final word. Application and Domain still perform their own checks regardless (`ARCHITECTURE.md` Section 11: Presentation depends only on Application and contains no business rules).
- Presentation must not re-implement any business rule or invariant (for example, it must not itself decide whether a Relationship is confirmed); it may only reflect an outcome already produced by Application/Domain, or perform a superficial check unrelated to business correctness.

## Application Validation Responsibilities

- Application performs the structural/input validation needed to even attempt a use case — for example, confirming a required reference (such as which Availability Slot is being booked) is present and well-formed enough to pass to Domain — before invoking Domain logic (`ADR-005`: Validation Responsibilities).
- Application does not decide business-rule outcomes itself. If a structural check passes, Application still invokes Domain, which alone renders the actual business-rule verdict (`ADR-005`).
- Application must not implement a shortcut duplicate of a Domain invariant "for efficiency"; doing so risks the copy and the original falling out of sync, violating the single-place-of-enforcement rule (`ADR-005`; `ADR-002`).
- Application is responsible for invoking the Authorization concern (`ADR-003`) as part of preparing to attempt a use case — a related but distinct concern from validation (see Relationship to Authorization, below).

## Domain Validation Responsibilities

- Domain is the sole, final authority on every business rule and invariant; a Domain operation refuses to complete an action that would violate one (`ADR-002`: Rules That Prevent Business Logic Leakage; `DOMAIN_MODEL.md`: Invariants).
- Domain validation is what actually enforces, among others: no double-booking (CONST-1), the minor-requires-confirmed-Relationship rule (IDR-6), the Tutor-must-be-approved-and-not-suspended rule (IDR-2, ADM-2), and the Session single-status rule (SCH-6).
- Domain validation occurs regardless of what Presentation or Application already checked; it is never skipped as an optimization, since it is the only enforcement point the architecture treats as authoritative (`ADR-002`; `ADR-005`).
- A successful Domain validation is what allows a Domain Event to be raised — an event is raised by Domain only once the transaction enforcing the underlying rule has succeeded (`ADR-006`), so Domain validation always precedes any Domain Event.

## Aggregate Invariant Enforcement

Each of the eight invariants in `DOMAIN_MODEL.md` is enforced by the aggregate that owns the state involved, per `ADR-002`'s Context Ownership and `ADR-004`'s Aggregate Persistence Rules:

| Invariant | Enforced By |
|---|---|
| No double-booking (CONST-1) | Session/Availability Slot boundary, owned by Scheduling & Booking |
| Single source of truth (CONST-3) | Exactly one persisted representation per aggregate, per owning context (`ADR-004`: Data Ownership) |
| Attributability (CONST-2) | Every mutating Domain operation, feeding the Audit concern (`ADR-004`, `ADR-006`) |
| Minor requires confirmed Relationship (IDR-6) | Scheduling & Booking at booking time, reading — not owning — the confirmation fact from Identity & Relationship |
| Tutor approved & not suspended (IDR-2, ADM-2) | Identity & Relationship, the Tutor aggregate's owner |
| Session holds exactly one status (SCH-6) | The Session aggregate, owned by Scheduling & Booking |
| Relationship confirmed only when both parties acted (IDR-4) | The Relationship aggregate, owned by Identity & Relationship |
| Least privilege (CONST-5) | Jointly with the Authorization model (`ADR-003`), evaluated per resource's owning context |

No invariant is enforced by two different aggregates or contexts independently; where a cross-context fact is needed (for example, IDR-6's Relationship-confirmation fact), it is read from the owning context, never re-derived or duplicated (`ADR-002`: Integration Rules).

## Cross-Context Validation Rules

- A bounded context validating a rule that depends on a fact owned by another context must read that fact from the owning context at the time of validation; it must never maintain its own independent copy of the rule that produces it (`ADR-002`: Integration Rules).
- Validation never triggers a write to another context's aggregate. If a validation failure implies some other context should also change state, that is a separate, explicitly authorized use case in that context, not a side effect of validation (`ADR-002`; `ADR-005`: Use Case Boundaries).
- A consuming context must not "pre-validate" another context's business rule speculatively and cache the result for reuse beyond the single operation at hand, since domain state (for example, Relationship confirmation or Tutor approval) can change, and a stale cached validation would violate the single-source-of-truth requirement (CONST-3).

## Error Reporting Principles

- A validation failure at any layer produces an explicit, detectable outcome; it is never silently dropped or silently corrected (`PROJECT_CONSTITUTION.md`: Engineering Principle 4; `ARCHITECTURE.md` Section 18: Error Handling Strategy).
- A Domain-rule rejection (for example, attempting to book an already-consumed Availability Slot, or a minor Student attempting to book without a confirmed Relationship) is an expected, explicit business outcome, distinct in kind from an unexpected system or Infrastructure failure; both are surfaced, but as different categories (`ADR-005`: Error Handling Responsibilities).
- Application translates a Domain-level rejection into a result Presentation can communicate to the acting Student, Tutor, Parent/Guardian, or Admin/Staff, without adding new business meaning of its own (`ADR-005`: Application Layer Responsibilities).
- Whether a denied authorization attempt, as distinct from a substantive validation/business-rule rejection, must also be reported or audited is not established by any approved document (`ADR-003` Open Question 6) and is carried forward here.

## Relationship to Authorization

- Validation and authorization are distinct concerns: validation asks whether an action, as described, is consistent with business rules and invariants; authorization asks whether this identity is permitted to attempt this action at all (`ADR-003`: Authentication Principles, Authorization Principles).
- Both are ultimately anchored in the Domain layer of the owning context where the decision depends on domain state (for example, whether a Tutor is approved, or a Relationship is confirmed). The same underlying facts often inform both, but they answer different questions and must not be conflated into a single undifferentiated check.
- Authorization is evaluated as part of the Application layer's orchestration of a use case, before or alongside validation; Application invokes the Authorization concern, then invokes Domain, which performs any remaining business validation and the actual state change (`ADR-005`).

## Relationship to Persistence

- Domain validation must succeed before any persistence write is attempted for the fact it protects; a violation never reaches the persisted store (`ADR-004`: Aggregate Persistence Rules, Transaction Boundaries).
- The atomic transaction boundary that enforces CONST-1 (`ADR-004`: Transaction Boundaries) is a persistence-level backstop for the same invariant Domain validation checks logically; both must agree — validation is not a substitute for the concurrency-safe transaction, nor the reverse (`ADR-004`: Concurrency Strategy).
- A concurrency conflict detected at the persistence layer (for example, two concurrent booking attempts against the same slot) is surfaced as a validation-equivalent explicit failure, per this ADR's Error Reporting Principles and `ADR-004`'s Concurrency Strategy — treated the same way as a Domain-level rule rejection, not as a separate category of error.

## Alternatives Considered

- **Validation performed only in Presentation, trusted as sufficient** — rejected. This would make business correctness depend on a layer `ARCHITECTURE.md` explicitly forbids from holding business rules (Section 11), and would violate correctness-before-convenience since a client-side-only check is not authoritative and can be bypassed.
- **Validation duplicated identically in both Application and Domain, "for defense in depth"** — rejected as a general policy. `ADR-002` and `ADR-005` require a business rule to be enforced in exactly one place to prevent two copies drifting out of sync; Application may still perform non-duplicative structural checks, but must not re-implement a Domain invariant.
- **Validation delegated to Infrastructure alone** (for example, enforced only by a persistence-level constraint with no Domain-level check) — rejected as the sole mechanism, since Domain must be the layer that authoritatively decides business correctness (`ARCHITECTURE.md` Section 8; `ADR-005`); a persistence-level backstop for concurrency is still preserved (see Relationship to Persistence) as a defense against race conditions, not as a replacement for Domain validation.
- **Domain as the sole and final validation authority, with Presentation offering convenience-only checks and Application handling structural/use-case-level checks (chosen)** — the only arrangement consistent with `ADR-002`, `ADR-005`, and the Constitution's Engineering Principles as already approved.

## Consequences

- Every use case's business correctness depends on Domain validation succeeding, regardless of what Presentation or Application already checked — a deliberate layering of Presentation convenience, Application structural checks, and Domain authority, not a duplication of business logic.
- Any change to a business rule's condition (for example, once cancellation notice-period rules are decided) is made in exactly one place — Domain — never requiring a parallel update to Application or Presentation logic.
- Distinguishing validation from authorization keeps both auditable and traceable back to distinct approved requirements, rather than being conflated into one opaque check.

## Risks

- If an implementer places a business-rule check in Presentation or Application "to fail fast" without an accompanying Domain enforcement, or lets the two drift apart, the single-place-of-enforcement guarantee is broken — this must be actively guarded against.
- Because several invariant preconditions remain open questions (for example, the exact age threshold underlying IDR-6), Domain validation for those rules can be architecturally located but not yet fully specified; implementation must not fill this gap with an invented default.
- Conflating validation and authorization into a single undifferentiated check could make it harder to audit which requirement — business rule or permission — actually blocked an action, weakening the Constitution's traceability principle (`PROJECT_CONSTITUTION.md`: Architecture Principle 8).

## Future Evolution

If new business rules are approved in the future (for example, cancellation notice periods), they are added to the Domain layer of the context that owns the relevant aggregate, following the same layered validation model established here, not bolted onto Presentation or Application. If a new bounded context is introduced (per `ADR-002`'s Future Evolution), it must adopt the same three-tier validation responsibility split established here. Whether additional error-reporting granularity (for example, machine-readable rejection reasons) is needed is not established by any approved document and would be a future, separate decision.

## Open Questions

1. What is the exact age threshold distinguishing an adult Student from a minor Student, needed to fully specify Domain validation for IDR-6? (`DOMAIN_MODEL.md` Open Question 1)
2. What are the cancellation/rescheduling notice-period rules that Domain validation for SCH-7 must eventually enforce? (`DOMAIN_MODEL.md` Open Question 5)
3. Must a denied authorization attempt be reported or audited in addition to a substantive validation/business-rule rejection? (`ADR-003` Open Question 6)
4. What information or credentials must a Tutor submit for approval, which Domain validation for IDR-2/ADM-1 would eventually need to check? (`DOMAIN_MODEL.md` Open Question 4)
5. Does cancelling a Session automatically reopen its Availability Slot, affecting what Domain validation must check when a new booking is attempted against a just-cancelled slot? (`DOMAIN_MODEL.md` Open Question 7)
6. Is Availability Slot a separate aggregate from Session, affecting exactly which aggregate enforces the no-double-booking invariant? (`DOMAIN_MODEL.md` Open Question 17)

## Traceability

| Element of this ADR | Source |
|---|---|
| Correctness before convenience as the governing validation principle | `PROJECT_CONSTITUTION.md`: Engineering Principle 1 |
| Structural vs. business-rule validation distinction | `ADR-005`: Validation Responsibilities |
| Single place of enforcement | `ADR-002`: Rules That Prevent Business Logic Leakage |
| Presentation has no business rules, depends only on Application | `ARCHITECTURE.md` Section 11 |
| The eight Domain invariants | `DOMAIN_MODEL.md`: Invariants |
| Aggregate ownership determining who enforces which invariant | `ADR-002`: Context Ownership; `ADR-004`: Aggregate Persistence Rules |
| Domain validation precedes Domain Events | `ADR-006`: Transaction Relationship |
| Fail safely and visibly | `PROJECT_CONSTITUTION.md`: Engineering Principle 4; `ARCHITECTURE.md` Section 18 |
| Validation distinguished from authorization | `ADR-003`: Authentication Principles, Authorization Principles |
| Concurrency conflict as an explicit, validation-equivalent failure | `ADR-004`: Concurrency Strategy |

---

*Status: Accepted. Status field corrected 2026-07-21 to match how every later ADR and the shipped implementation already treat this document — see the Status section above.*
