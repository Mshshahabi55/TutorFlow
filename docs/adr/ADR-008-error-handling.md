**Authoritative sources (in priority order):** `PROJECT_CONSTITUTION.md`, `PRODUCT_REQUIREMENTS.md`, `BUSINESS_MODEL.md`, `DOMAIN_MODEL.md`, `ARCHITECTURE.md`, `docs/adr/ADR-001-architecture-style.md`, `docs/adr/ADR-002-domain-boundaries.md`, `docs/adr/ADR-003-authentication-and-authorization.md`, `docs/adr/ADR-004-persistence-strategy.md`, `docs/adr/ADR-005-application-boundary.md`, `docs/adr/ADR-006-domain-events.md`, `docs/adr/ADR-007-validation-strategy.md` (all approved and immutable). This ADR defines architectural responsibilities and error semantics only — it names no specific error-signaling mechanism, transport status code, interception mechanism, library, framework, programming language, or logging technology, and none is implied by anything below. Where information needed to complete this ADR is missing from the twelve sources above, it is recorded under Open Questions rather than invented.

---

# ADR-008: Error Handling Strategy

## Status

**Accepted.** *(Status field corrected 2026-07-21 — this document's own Status was never updated after approval; every ADR from `ADR-013` onward, dated 2026-07-20, already lists this document among its "approved and immutable" sources, and the entire shipped implementation is built on it. Gap first identified by the Final Architecture Review Board's production-readiness audit, 2026-07-21. No content in this ADR is otherwise altered by this correction.)*

## Context

`PROJECT_CONSTITUTION.md` requires the system to fail safely and visibly — detectably, and never by silently corrupting schedule data or user trust (Engineering Principle 4) — and treats scheduling-integrity defects as critical by default (Quality Principle 3). `ARCHITECTURE.md` already distinguishes a Domain-rule rejection from an unexpected system failure as two different categories of outcome (Section 18), and requires system-level failures to be surfaced to Admin/Staff in a way that supports detection and resolution without reading code or querying a database directly (Constitution: Success Criteria). `ADR-004` established that a concurrency conflict must be surfaced as an explicit, handled outcome, never silent corruption. `ADR-005` established that Application translates a Domain-level rejection for Presentation and surfaces an Infrastructure-level failure for Admin/Staff, treating the two as distinct categories, and left open how a partial failure across two coordinated cross-context transactions should be handled. `ADR-007` established that a Domain-rule rejection is an expected, explicit business outcome, and that a concurrency conflict is treated as validation-equivalent, not as a separate category. This ADR consolidates these threads into one complete, technology-neutral error handling strategy.

## Problem Statement

Given the layered architecture (`ADR-001`), bounded-context ownership (`ADR-002`), transaction and persistence rules (`ADR-004`), Application boundary (`ADR-005`), and validation strategy (`ADR-007`) already fixed, what is TutorFlow's complete taxonomy of error types, which layer is responsible for producing, classifying, translating, and communicating each, how do errors interact with transaction boundaries and audit, and how must business invariants remain protected even when something fails — without prescribing any implementation mechanism?

## Decision

Two fundamentally distinct categories of failure exist. A **Domain Error** is an expected, meaningful business outcome: the Domain layer of the owning bounded context refused to complete an action because doing so would violate an already-established business rule or invariant. An **Infrastructure Failure** is unexpected and carries no business meaning of its own: a technical capability did not perform as required. A Domain Error is raised only by Domain and is never invented by Application, Infrastructure, or Presentation. An Infrastructure Failure never becomes, implies, or is silently reinterpreted as a business rule, and it never leaves a business invariant in a violated or ambiguous state — the operation is treated as not having happened, never as having half-happened. Application is responsible for classifying which category applies, translating a Domain Error into a result Presentation can communicate to the acting Domain Actor, and surfacing an Infrastructure Failure so that Admin/Staff can detect and resolve it without reading code or querying a database directly. Every qualifying action's outcome remains consistent with CONST-2's attributability requirement.

## Error Classification Principles

- **Domain Error:** the Domain layer refused to complete an action because it would violate a business rule or invariant already established (for example, booking an already-consumed slot, or a minor Student without a confirmed Relationship). This is a first-class, expected outcome of correct operation, not a defect (`ADR-007`: Domain Validation Responsibilities; `ARCHITECTURE.md` Section 18).
- **Infrastructure Failure:** a technical capability — persistence, identity, or audit storage — did not perform as expected. This is unexpected and carries no business meaning of its own (`ARCHITECTURE.md` Section 10: Infrastructure "contains no business logic of its own").
- These two categories are never merged into one undifferentiated notion of "error"; every error handling decision begins by classifying which applies (`ADR-005`: Error Handling Responsibilities — "distinguishes an expected, Domain-rule-driven rejection ... from an unexpected system failure").
- A concurrency conflict at the persistence boundary (for example, two simultaneous booking attempts against the same slot) is classified as a Domain-Error-equivalent outcome, not an Infrastructure Failure, because it reflects the same business invariant (CONST-1) that Domain validation protects (`ADR-007`: Relationship to Persistence; `ADR-004`: Concurrency Strategy).

## Domain Error Principles

- A Domain Error is raised exclusively by the Domain layer of the bounded context that owns the violated rule (`ADR-002`: Rules That Prevent Business Logic Leakage).
- A Domain Error always corresponds to a business rule or invariant already established in `DOMAIN_MODEL.md` or `PRODUCT_REQUIREMENTS.md`; no new business rule is introduced as a byproduct of error handling.
- Raising a Domain Error means the requested change did not happen; the aggregate's state is left exactly as it was before the attempt, never partially applied (`ADR-004`: Transaction Boundaries).
- A Domain Error is not an Infrastructure concept and is never expressed in terms of persistence or technical mechanics, consistent with Domain having zero dependency on Infrastructure (`ARCHITECTURE.md` Section 8).

## Application Error Responsibilities

- Application receives the outcome of invoking Domain and classifies it as success, a Domain Error, or — via Infrastructure — an Infrastructure Failure (`ADR-005`: Error Handling Responsibilities).
- Application translates a Domain Error into a result Presentation can communicate to the acting Student, Tutor, Parent/Guardian, or Admin/Staff, without adding new business meaning of its own (`ADR-005`).
- Application never suppresses, swallows, or converts a Domain Error into a false success (`PROJECT_CONSTITUTION.md`: Engineering Principle 4).
- Application surfaces an Infrastructure Failure so that an Admin/Staff member can detect and resolve it without reading code or querying a database directly (`PROJECT_CONSTITUTION.md`: Success Criteria).
- When an Infrastructure Failure occurs mid-use-case, Application ensures no business invariant is left violated — a use case is never considered complete, and no partially-applied effect is allowed to stand, unless Domain's rule has actually and fully succeeded (`ADR-004`: Transaction Boundaries).

## Infrastructure Error Responsibilities

- Infrastructure reports that a technical capability did not perform as expected; it makes no business-rule judgment about why this matters (`ARCHITECTURE.md` Section 10).
- Infrastructure never invents, approximates, or silently substitutes a business decision when a technical failure occurs — for example, it must never assume a slot is "probably still available" and proceed. Doing so would contradict Engineering Principle 4 and risk exactly the double-booking or inconsistency CONST-1 and CONST-3 forbid.
- An Infrastructure Failure never becomes, or is reinterpreted as, a business rule; Domain remains the only source of business meaning regardless of which technical failure occurred.
- Infrastructure is the only layer aware of any concrete technical failure mode; it reports failures to Application through the interfaces Application defines, without leaking technical detail into Domain (`ARCHITECTURE.md` Section 7).

## Presentation Responsibilities

- Presentation communicates the outcome Application has already classified and translated: a Domain Error is shown as an explicit, business-meaningful explanation; an Infrastructure Failure is shown as a general notice, since Presentation holds no business rules of its own to interpret it (`ARCHITECTURE.md` Section 11).
- Presentation never itself classifies or reinterprets an error, and never decides on its own that a failure "probably" means something business-relevant — the same non-authority principle already established for Presentation validation applies equally to errors (`ADR-007`: Presentation Validation Responsibilities).
- Presentation has no role in resolving an Infrastructure Failure; detection and resolution of such failures is an Admin/Staff concern per the Constitution's Success Criteria, not a concern for the acting Student, Tutor, or Parent/Guardian.

## Cross-Context Error Rules

- A Domain Error raised within one bounded context is never reinterpreted, replaced, or converted into a different rule by another context. If Marketplace Oversight's orchestration invokes Scheduling & Booking's own use case (`ADR-005`: Use Case Boundaries) and that use case produces a Domain Error, Marketplace Oversight receives and communicates that same error rather than inventing its own version of the rule.
- A context reading another context's data as part of its own validation (`ADR-002`: Integration Rules; `ADR-007`: Cross-Context Validation Rules) must treat a failure to obtain that data as an Infrastructure Failure of its own operation, not as a Domain Error about the other context's rule — a context has no authority to render a business verdict on a rule it does not own.
- No context proceeds with a state-changing operation if a required cross-context read fails; failing safely means treating that as a failed precondition, never proceeding as if the read had returned a favorable answer (`PROJECT_CONSTITUTION.md`: Engineering Principle 4).

## Transaction Failure Principles

- Within a single bounded context's transaction boundary (`ADR-004`: Transaction Boundaries), any failure — Domain Error or Infrastructure Failure — leaves the aggregate's persisted state exactly as it was before the attempt; there is no partially-applied outcome.
- Because a transaction never spans two bounded contexts (`ADR-004`), a cross-context use case's two separately-transacted steps can, in principle, leave one context's transaction succeeded while a second, dependent transaction fails. How the consuming context's Application layer must handle that scenario is not established by any approved document — this is the same unresolved gap already flagged in `ADR-004` and `ADR-005`, and is carried forward here rather than resolved by invention.
- Whatever resolution is eventually chosen for that gap, it must not violate CONST-1 or CONST-3 in the interim: no interim state may present different roles with drifted views of the same schedule fact, and no interim state may allow a slot to appear both bookable and booked at once.

## Audit Relationship

- A Domain Error is an expected business outcome and does not itself trigger the same audit entry a successful mutation would, since `ADR-004`'s Audit Persistence Principles tie audit entries to actions that create, reschedule, or cancel a booking, or change availability. Whether a Domain Error rejection must nonetheless be recorded in the audit trail is not established by any approved document (see Open Questions).
- An Infrastructure Failure that prevents an audit entry from being recorded for an action that did succeed at the Domain level is a serious condition: `ADR-004` already establishes that an audit entry must never be lost, since CONST-2 treats attributability as non-negotiable. This ADR reinforces that such a failure in the audit path must be surfaced explicitly, never silently tolerated, per Engineering Principle 4.
- Whether a denied authorization attempt must also be recorded in the audit trail remains open (`ADR-003` Open Question 6; `ADR-007` Open Question 3) and is not resolved here.

## User Communication Principles

- A Domain Error is communicated to the acting Student, Tutor, or Parent/Guardian as a specific, business-meaningful explanation of why the requested action did not happen (for example, that the slot is no longer available), consistent with the requirement that booking, rescheduling, and cancelling be simple and unambiguous for every party (`PROJECT_CONSTITUTION.md`: Product Goal 2).
- An Infrastructure Failure is communicated as a general notice that the action could not be completed, without exposing technical detail, since such detail carries no business meaning for the acting Domain Actor and Presentation holds no business rules to interpret it (`ARCHITECTURE.md` Section 11).
- Regardless of category, the acting Domain Actor is never left believing an action succeeded when it did not (`PROJECT_CONSTITUTION.md`: Engineering Principle 4; Product Goal 6 — no session is ever lost or left inconsistent).
- Admin/Staff communication is broader than the acting actor's: Admin/Staff must be able to detect and resolve both Domain Errors that recur unexpectedly often and Infrastructure Failures, without reading code or querying a database directly (`PROJECT_CONSTITUTION.md`: Success Criteria).

## Alternatives Considered

- **A single, undifferentiated "error" concept with no distinction between Domain Errors and Infrastructure Failures** — rejected. This would blur the distinction already established in `ADR-005` and `ADR-007`, risking either exposing raw technical failure as if it were a business explanation, or suppressing a genuine business rejection as a generic technical error — both violate Engineering Principle 4.
- **Treating an Infrastructure Failure as an implicit "assume success and reconcile later" outcome** — rejected. This risks violating CONST-1 and CONST-3 by letting an operation proceed on an unconfirmed assumption; Engineering Principle 4 requires failing safely, not optimistically.
- **Allowing Presentation to interpret or classify errors on its own** — rejected. `ARCHITECTURE.md` Section 11 already forbids Presentation from holding business rules, and classifying a Domain Error requires business meaning only Application/Domain may render.
- **Domain Errors and Infrastructure Failures classified and handled distinctly, with Application as the translation point and the aggregate's prior state always preserved on failure (chosen)** — the only approach consistent with `ADR-002`, `ADR-004`, `ADR-005`, and `ADR-007` as already approved.

## Consequences

- Every use case's outcome is exactly one of three kinds — success, Domain Error, or Infrastructure Failure — and each layer's responsibility is defined in terms of which of these it may produce, translate, or communicate.
- A business invariant is never left violated by either kind of failure; this is treated as unconditional, not best-effort, consistent with Product Goal 6.
- Admin/Staff tooling must be capable of exposing both kinds of failure for detection and resolution, per the Constitution's Success Criteria, though this ADR does not design that tooling.

## Risks

- The unresolved cross-context partial-failure question (Transaction Failure Principles) remains a real gap; until resolved, an implementer could inadvertently leave two contexts' states inconsistent with each other during a cross-context use case failure.
- If Infrastructure Failures are not clearly distinguished from Domain Errors during implementation, technical detail could leak to end users, or a genuine business rejection could be misreported as a system fault, undermining the trust Product Goal 6 and 8 require.
- If whether a Domain Error rejection must itself be audited is assumed one way without confirmation, Admin/Staff visibility into recurring rejections — a possible trust/safety signal — could be incomplete.

## Future Evolution

Once the cross-context partial-failure handling approach is decided — resolving the shared open question already carried from `ADR-004` and `ADR-005` — it must be recorded as its own Structural decision through the Constitution's Decision-Making Process, and must not violate CONST-1 or CONST-3 in any interim state. If new bounded contexts or use cases are introduced (per `ADR-002`'s Future Evolution), they adopt the same Domain-Error/Infrastructure-Failure classification and per-layer responsibilities established here. Whether richer failure detail is needed to satisfy the Constitution's Success Criteria for Admin/Staff resolution is not established by any approved document and would be a future, separate decision.

## Open Questions

1. How should the Application layer handle a partial failure when a cross-context use case's effects are coordinated across two separate context transactions? (`ADR-004`; `ADR-005` Open Question 1)
2. Must a Domain Error rejection itself be recorded in the audit trail, in addition to successful mutating actions? Not addressed by any approved document.
3. Must a denied authorization attempt be recorded in the audit trail? (`ADR-003` Open Question 6; `ADR-007` Open Question 3)
4. What specific mechanics does the "resolve booking conflicts" use case include, which would inform how a Marketplace Oversight-initiated Domain Error is communicated to an Admin? (`DOMAIN_MODEL.md` Open Question 13)
5. Is structured, queryable failure detail required for Admin/Staff to meet the Constitution's Success Criteria, or is a durable audit/error record sufficient? Not addressed by any approved document.

## Traceability

| Element of this ADR | Source |
|---|---|
| Fail safely and visibly as the governing principle | `PROJECT_CONSTITUTION.md`: Engineering Principle 4 |
| Domain-rule rejection vs. unexpected system failure distinction | `ARCHITECTURE.md` Section 18; `ADR-005`: Error Handling Responsibilities |
| Concurrency conflict treated as Domain-Error-equivalent | `ADR-004`: Concurrency Strategy; `ADR-007`: Relationship to Persistence |
| Business logic confined to owning context's Domain layer | `ADR-002`: Rules That Prevent Business Logic Leakage |
| Transaction atomicity and no partial application | `ADR-004`: Transaction Boundaries |
| Infrastructure holds no business logic | `ARCHITECTURE.md` Section 10 |
| Presentation holds no business rules | `ARCHITECTURE.md` Section 11 |
| Admin/Staff detection and resolution without code/database access | `PROJECT_CONSTITUTION.md`: Success Criteria |
| Audit entry must never be lost | `PRODUCT_REQUIREMENTS.md` CONST-2; `ADR-004`: Audit Persistence Principles |
| Unresolved cross-context partial-failure handling | `ADR-004`; `ADR-005` Open Question 1 |

---

*Status: Accepted. Status field corrected 2026-07-21 to match how every later ADR and the shipped implementation already treat this document — see the Status section above.*
