**Authoritative sources (in priority order):** `PROJECT_CONSTITUTION.md`, `PRODUCT_REQUIREMENTS.md`, `BUSINESS_MODEL.md`, `DOMAIN_MODEL.md`, `ARCHITECTURE.md`, `docs/adr/ADR-001-architecture-style.md`, `docs/adr/ADR-002-domain-boundaries.md`, `docs/adr/ADR-003-authentication-and-authorization.md`, `docs/adr/ADR-004-persistence-strategy.md`, `docs/adr/ADR-005-application-boundary.md`, `docs/adr/ADR-006-domain-events.md`, `docs/adr/ADR-007-validation-strategy.md`, `docs/adr/ADR-008-error-handling.md` (all approved and immutable). This ADR defines architectural principles only — it names no logging framework, telemetry product, monitoring system, or cloud provider, and none is implied by anything below. Where information needed to complete this ADR is missing from the thirteen sources above, it is recorded under Open Questions rather than invented.

---

# ADR-009: Audit, Traceability, and Observability Strategy

## Status

**Accepted.** *(Status field corrected 2026-07-21 — this document's own Status was never updated after approval; every ADR from `ADR-013` onward, dated 2026-07-20, already lists this document among its "approved and immutable" sources, and the entire shipped implementation is built on it. Gap first identified by the Final Architecture Review Board's production-readiness audit, 2026-07-21. No content in this ADR is otherwise altered by this correction.)*

## Context

`PRODUCT_REQUIREMENTS.md` requires every action that creates, reschedules, or cancels a booking, or changes availability, to be attributable to an authenticated identity, role, and timestamp (CONST-2), reflecting `PROJECT_CONSTITUTION.md`'s Architecture Principle 3 (auditable actions) and Security Principle 4 (accountability for access). `ARCHITECTURE.md` already sketches an Audit Strategy (Section 16) extending this coverage to Tutor approval and suspension, and ties it to the Constitution's Success Criteria that an Admin/Staff member can detect and resolve a scheduling problem without reading code or querying a database directly. `ADR-003` through `ADR-008` each touch a piece of this picture — Audit Requirements (`ADR-003`), Audit Persistence Principles (`ADR-004`), Domain Event Coordination (`ADR-005`), the Audit Relationship to Domain Events (`ADR-006`), and the Audit Relationship to error handling (`ADR-008`) — but no single document consolidates them, and several sub-questions (whether denied-authorization attempts or Domain Error rejections must be audited, and whether an audit entry must share its mutation's transaction) remain open across all of them. This ADR consolidates the existing rules into one complete Audit, Traceability, and Observability strategy without inventing resolutions to what remains genuinely open.

## Problem Statement

Given CONST-2's non-negotiable attributability requirement, the bounded-context ownership already fixed (`ADR-002`), the persistence and transaction rules (`ADR-004`), the Domain Event rules (`ADR-006`), and the error classification (`ADR-008`), what is TutorFlow's complete architectural strategy for audit (a durable record of business-changing actions), traceability (the ability to reconstruct what happened and why), and observability (Admin/Staff's ability to detect and understand system behavior) — without prescribing any implementation technology?

## Decision

Every action that creates, reschedules, or cancels a Session, or changes Availability, is durably recorded with the acting identity, its role, the action, and a timestamp (CONST-2), extended to Tutor approval and suspension per `ARCHITECTURE.md` Section 16. The audit record for a given business fact is owned by the same bounded context that owns the aggregate the action affects (`ADR-002`) — audit is not a separate, ownerless concern. Traceability means every such record can be reconstructed back to who acted, in what role, what business fact occurred, and when. Observability, for v1, is scoped to the business requirement already established in the Constitution's Success Criteria: an Admin/Staff member can detect and resolve a scheduling problem without reading code or querying a database directly — it is not a broader technical-monitoring mandate this ADR invents. Several specific sub-questions inherited from prior ADRs remain unresolved and are consolidated as Open Questions rather than decided here.

## Audit Principles

- **Non-negotiable attributability:** every action creating, rescheduling, or cancelling a Session, or changing Availability, is recorded with identity, role, action, and timestamp — this is not best-effort (CONST-2; `PROJECT_CONSTITUTION.md`: Architecture Principle 3, Security Principle 4).
- **Extended trust-critical coverage:** Tutor approval and suspension receive the same treatment, since they are equally consequential to the trust the Constitution requires (`ARCHITECTURE.md` Section 16; Product Goal 6, 8).
- **Durability over convenience:** an audit entry, once required, must never be lost. A technical failure in recording it is a serious condition requiring explicit surfacing, never silent tolerance (`ADR-004`: Audit Persistence Principles; `ADR-006`: Reliability Principles).
- **Ownership follows the rule, not a separate silo:** the bounded context whose Domain layer enforced the rule owns the corresponding audit record, consistent with `ADR-002`'s exclusive-ownership model. Audit is not modeled as a fifth, ownerless bounded context.
- **Audit is a record of fact, not a decision:** recording an action does not itself authorize, validate, or alter that action's outcome, paralleling `ADR-006`'s rule that Domain Events never replace business logic.

## Traceability Principles

- Every audit record allows reconstruction of who acted (identity), in what capacity (role), what happened (the business fact), and when (timestamp) — the four elements CONST-2 already requires.
- Traceability extends the Constitution's general documentation discipline — "significant decisions and their rationale are documented and discoverable, not held as tribal knowledge" (Documentation Rules) — into the runtime system: a scheduling outcome must be explainable after the fact without inspecting code or a live database (`PROJECT_CONSTITUTION.md`: Success Criteria).
- Traceability is anchored to the Domain Events already named in `DOMAIN_MODEL.md` and formalized in `ADR-006`, since each event already represents a completed business fact; an audit record's business content is drawn from that same set of facts, not a separately invented list.
- Traceability holds across a Session's full lifecycle (Scheduled → Completed/Cancelled/No-Show, per `PRODUCT_REQUIREMENTS.md` SCH-6), so an Admin/Staff member can reconstruct a Session's entire history, not only its final state.

## Business Event Audit Rules

- The following are the definitive business-event audit list for v1, each already established as requiring a record: creating a Session (booking), rescheduling a Session, cancelling a Session, changing Availability (CONST-2), approving a Tutor, and suspending a Tutor (`ARCHITECTURE.md` Section 16; `ADR-003`: Audit Requirements).
- Each corresponds to a named Domain Event (`DOMAIN_MODEL.md`; `ADR-006`): SessionBooked, SessionRescheduled, SessionCancelled, AvailabilityDeclared, TutorApproved, TutorSuspended.
- Whether the remaining named Domain Events — RelationshipInvited, RelationshipConfirmed, SessionCompleted, SessionMarkedNoShow, BookingConflictResolved, TutorRegistered — each independently require their own audit entry, or are covered indirectly by the audit entry of a related action, is not fully settled (`ADR-006` Open Question 4) and is carried forward here.
- Whether a Domain Error — a business-rule rejection, per `ADR-008` — must itself be recorded in the audit trail, separately from a successful mutation, remains unresolved (`ADR-008` Open Question 2) and is not decided here.

## Security Audit Rules

- Accountability for access requires every action affecting another party's data or schedule to be attributable to an authenticated identity and role (`PROJECT_CONSTITUTION.md`: Security Principle 4) — this is the security rationale behind CONST-2 and the Business Event Audit Rules above, not a separate mechanism.
- Least-privilege enforcement (CONST-5; `ADR-003`: Authorization Principles) is a precondition to every audited action; whether a denied attempt is itself audited remains open (`ADR-003` Open Question 6; `ADR-007` Open Question 3; `ADR-008` Open Question 3).
- Personal data within an audit record must still meet the GDPR-grade protection standard already established for personal data generally (CONST-4; `PROJECT_CONSTITUTION.md`: Security Principle 3) — audit creates no exception to that standard.
- Audit records are protected by the same least-privilege model as any other data: only Admin/Staff, through their oversight function (`PRODUCT_REQUIREMENTS.md` ADM-3, ADM-4), are established as needing to read them. No approved document establishes that any other role has read access to another party's audit history.

## Administrative Audit Rules

- Admin/Staff actions — approving/suspending a Tutor, resolving a booking conflict, managing a user account (`PRODUCT_REQUIREMENTS.md` ADM-1 to ADM-5) — are held to at least the same attributability standard as any other business-changing action, since they are equally consequential to trust (`PROJECT_CONSTITUTION.md`: Product Goal 5, 8).
- The audit trail is the mechanism by which an Admin/Staff member detects and resolves a scheduling problem without reading code or querying a database directly (`PROJECT_CONSTITUTION.md`: Success Criteria), supporting the "Operational resolution time" success metric (`BUSINESS_MODEL.md`: Success Metrics).
- Exactly what "resolve a booking conflict" audits is not fully specified, since the mechanics of conflict resolution themselves remain unresolved (`DOMAIN_MODEL.md` Open Question 13; `ADR-006` Open Question 2, on BookingConflictResolved's ownership).
- Whether Admin/Staff permission tiers, if later approved (`DOMAIN_MODEL.md` Open Question 12), would require differentiated audit granularity is not established and is not assumed here.

## Cross-Context Audit Rules

- Each bounded context's Application layer triggers the audit record for business-changing actions occurring within its own owned aggregates, consistent with `ADR-002`'s exclusive-ownership model; a context never records an audit entry on behalf of another context's aggregate.
- When a use case spans contexts (for example, Marketplace Oversight orchestrating a change that Scheduling & Booking's own use case ultimately performs, per `ADR-005`: Use Case Boundaries), the audit entry for the underlying business fact is recorded by the owning context at the point its own transaction completes, not duplicated by the orchestrating context — though whether the orchestrating context separately records its own act is unresolved (see Administrative Audit Rules).
- A context reading another context's audit-relevant fact does so as a read of the owning context's record, never as an independently maintained copy — the same "read, not copy" pattern already fixed for cross-context data generally (`ADR-002`: Integration Rules; `ADR-004`: Read Model Principles).

## Observability Principles

- For v1, observability is scoped to the business requirement already established: Admin/Staff can detect and resolve a scheduling problem without reading code or querying a database directly (`PROJECT_CONSTITUTION.md`: Success Criteria). No broader technical-monitoring requirement is established by any approved document.
- Observability is built on the same audit trail and error classification already established (`ADR-008`: Error Classification Principles); a Domain Error and an Infrastructure Failure are both facts Admin/Staff must be able to observe, though whether they require identical or differentiated observability treatment is not established (see Open Questions).
- No specific scale, latency, or retention target for observability data is established; `PROJECT_CONSTITUTION.md`'s KPI section explicitly defers specific targets and instrumentation until "the domain and metrics ownership are established" — this ADR does not invent one.
- Observability data is subject to the same data-protection standard as any other personal data it may contain (CONST-4); it is not exempt because it serves an operational rather than a user-facing purpose.

## Relationship to Domain Events

- The audit trail's primary trigger, per `ADR-005` and `ADR-006`, is the Application layer's recognition that a Domain Event has occurred; the twelve Domain Events already named in `DOMAIN_MODEL.md` are the vocabulary from which auditable business facts are drawn (`ADR-006`: What Qualifies as a Domain Event).
- An audit entry never precedes the Domain Event it records — since a Domain Event is raised only after Domain has already enforced the corresponding rule within its transaction (`ADR-006`: Transaction Relationship), the audit entry likewise can only record a fact that has already become true.
- This ADR does not resolve whether audit-entry creation must occur within the same transaction as the event-producing mutation, or may occur immediately after — this remains the open question already carried through `ADR-004` and `ADR-006`.

## Relationship to Error Handling

- A Domain Error (per `ADR-008`) is an expected business outcome and does not, by itself, generate the same audit entry a successful mutation would; whether it must nonetheless be recorded is unresolved (`ADR-008` Open Question 2) and not decided here.
- An Infrastructure Failure that prevents an audit entry from being recorded for an action that did succeed at the Domain level must be surfaced explicitly, never silently tolerated (`ADR-008`: Audit Relationship; `ADR-004`: Audit Persistence Principles) — this is the strongest point of overlap between this ADR and `ADR-008`, and both are consistent: audit durability is unconditional once a business fact is true.
- Observability (this ADR) and error classification (`ADR-008`) are complementary: an Admin/Staff member's ability to detect and resolve a problem depends on both a durable audit trail of what happened and a clear classification of why an attempt failed.

## Alternatives Considered

- **A single, undifferentiated log with no distinction between business-audit records, security-audit records, and general observability data** — rejected. This would blur the specific, unconditional durability CONST-2 requires for business-changing actions against the more general observability need, making it harder to guarantee the former.
- **Audit ownership centralized outside the four bounded contexts, in a single ownerless "audit service" that records everything** — rejected. This would contradict `ADR-002`'s exclusive-ownership model and risk becoming a de facto second, cross-context data store, violating CONST-3 (single source of truth) if it duplicated business state to explain itself.
- **Treating observability as purely a technical concern unrelated to the audit trail** — rejected. The Constitution's Success Criteria frames Admin/Staff problem detection and resolution as a business requirement met through visibility into what happened, which the audit trail already provides; splitting them would duplicate effort and risk inconsistency.
- **Per-context audit ownership anchored to the Domain Events already established, feeding a business-requirement-scoped observability capability for Admin/Staff (chosen)** — the only approach consistent with `ADR-002`, `ADR-004`, `ADR-006`, and `ADR-008` as already approved, and with the Constitution's Success Criteria.

## Consequences

- Every business-changing action already required to be auditable (CONST-2, plus Tutor approval/suspension) has a clear, single owning context responsible for recording it, with no ambiguity about accountability for that record's completeness.
- Any future Admin/Staff tooling (not designed by this ADR) must be built on top of this audit trail to satisfy the Constitution's Success Criteria; such tooling depends on the rules established here remaining stable.
- Several sub-questions (denied-authorization coverage, Domain-Error coverage, same-transaction requirement, BookingConflictResolved's audit content) remain open, so the audit trail's coverage — while unambiguous for the core CONST-2 actions — is not yet fully specified at its edges.

## Risks

- If an implementer assumes an answer to one of the open sub-questions without confirmation, Admin/Staff visibility could be incomplete or inconsistent with what other parts of the architecture expect.
- Because BookingConflictResolved's ownership is unresolved (`ADR-006` Open Question 2), the corresponding audit record's ownership is equally unresolved, risking an implementation that either duplicates the record across contexts or omits it.
- Observability scoped only to the Constitution's Success Criteria could under-serve a future need if additional operational requirements are approved later without a corresponding architectural update.

## Future Evolution

If the Constitution's KPI targets and instrumentation are later established (per its own deferral), this ADR's Observability Principles would need to be extended to reflect them, through the Constitution's Decision-Making Process, not assumed now. If new bounded contexts are introduced (per `ADR-002`'s Future Evolution), each adopts the same audit-ownership and traceability rules established here for any new Domain Events it raises. Resolution of the still-open sub-questions should be recorded as explicit amendments or a future ADR once decided, not silently assumed during implementation.

## Open Questions

1. Must a denied authorization attempt be recorded in the audit trail? (`ADR-003` Open Question 6; `ADR-007` Open Question 3; `ADR-008` Open Question 3)
2. Must a Domain Error rejection be recorded in the audit trail, in addition to successful mutating actions? (`ADR-008` Open Question 2)
3. Must an audit entry be persisted within the same transaction as the aggregate mutation it records, or may it be persisted through a separate mechanism? (`ADR-004` Open Question 6)
4. Do the Domain Events not explicitly named in CONST-2's wording (RelationshipInvited, RelationshipConfirmed, SessionCompleted, SessionMarkedNoShow, BookingConflictResolved, TutorRegistered) each require an independent audit entry, or are some covered indirectly? (`ADR-006` Open Question 4)
5. Which bounded context owns the BookingConflictResolved event and its corresponding audit record, given Marketplace Oversight owns no aggregate? (`DOMAIN_MODEL.md` Open Question 13; `ADR-002` Open Question 2; `ADR-006` Open Question 2)
6. Do a Domain Error and an Infrastructure Failure require identical or differentiated observability treatment for Admin/Staff? Not addressed by any approved document.
7. What specific numeric targets or instrumentation apply to the "Operational resolution time" KPI? (`BUSINESS_MODEL.md` Open Questions, Item 10)

## Traceability

| Element of this ADR | Source |
|---|---|
| Non-negotiable attributability of business-changing actions | `PRODUCT_REQUIREMENTS.md` CONST-2; `PROJECT_CONSTITUTION.md`: Architecture Principle 3 |
| Accountability for access | `PROJECT_CONSTITUTION.md`: Security Principle 4 |
| Extended audit coverage for Tutor approval/suspension | `ARCHITECTURE.md` Section 16 |
| Audit ownership follows aggregate ownership | `ADR-002`: Context Ownership |
| Audit durability, never lost | `ADR-004`: Audit Persistence Principles; `ADR-006`: Reliability Principles |
| Domain Events as the vocabulary of auditable facts | `DOMAIN_MODEL.md`: Domain Events; `ADR-006` |
| Admin/Staff detection and resolution without code/database access | `PROJECT_CONSTITUTION.md`: Success Criteria |
| Operational resolution time success metric | `BUSINESS_MODEL.md`: Success Metrics |
| Domain Error vs. Infrastructure Failure classification | `ADR-008`: Error Classification Principles |
| GDPR-grade protection extends to audit/observability data | `PRODUCT_REQUIREMENTS.md` CONST-4; `PROJECT_CONSTITUTION.md`: Security Principle 3 |

---

*Status: Accepted. Status field corrected 2026-07-21 to match how every later ADR and the shipped implementation already treat this document — see the Status section above.*
