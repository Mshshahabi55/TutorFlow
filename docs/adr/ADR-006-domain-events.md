**Authoritative sources (in priority order):** `PROJECT_CONSTITUTION.md`, `PRODUCT_REQUIREMENTS.md`, `BUSINESS_MODEL.md`, `DOMAIN_MODEL.md`, `ARCHITECTURE.md`, `docs/adr/ADR-001-architecture-style.md`, `docs/adr/ADR-002-domain-boundaries.md`, `docs/adr/ADR-003-authentication-and-authorization.md`, `docs/adr/ADR-004-persistence-strategy.md`, `docs/adr/ADR-005-application-boundary.md` (all approved and immutable). This ADR is about architectural principles only — it names no message broker, queue, event bus, or any other implementation technology, and none is implied by anything below. Where information needed to complete this ADR is missing from the ten sources above, it is recorded under Open Questions rather than invented.

---

# ADR-006: Domain Events — Architectural Rules

## Status

**Accepted.** *(Status field corrected 2026-07-21 — this document's own Status was never updated after approval; every ADR from `ADR-013` onward, dated 2026-07-20, already lists this document among its "approved and immutable" sources, and the entire shipped implementation is built on it. Gap first identified by the Final Architecture Review Board's production-readiness audit, 2026-07-21. No content in this ADR is otherwise altered by this correction.)*

## Context

`DOMAIN_MODEL.md` already names twelve Domain Events — TutorRegistered, TutorApproved, TutorSuspended, RelationshipInvited, RelationshipConfirmed, AvailabilityDeclared, SessionBooked, SessionRescheduled, SessionCancelled, SessionCompleted, SessionMarkedNoShow, BookingConflictResolved — describing them as "past-tense occurrences of significance to the domain," while noting their exact naming is a modeling convenience over facts already sourced elsewhere. `ARCHITECTURE.md` carried these into the Domain layer's responsibilities (Section 8). `ADR-005` established that the Domain layer raises these events as part of enforcing a business rule, that the Application layer recognizes an event's occurrence in order to invoke the Audit concern, that Application never alters or reinterprets an event's meaning, and left open whether an event must ever trigger a reaction in a different bounded context. `ADR-002` fixed exclusive aggregate ownership and forbade cross-context writes; `ADR-004` fixed that a transaction never spans two bounded contexts and that audit entries must never be lost. This ADR consolidates those threads into a single, precise set of rules governing what a Domain Event is, who owns it, and how it may be published, consumed, and related to transactions and audit — without prescribing any technology.

## Problem Statement

Given the twelve Domain Events already named in `DOMAIN_MODEL.md` and the ownership, transaction, and audit rules already fixed in `ADR-002`, `ADR-004`, and `ADR-005`, what are the precise architectural rules for what qualifies as a Domain Event, who owns and publishes it, whether and how it may be consumed across a bounded-context boundary, how it relates to the transaction that produced it, what it means for audit, and what reliability and ordering guarantees it must uphold — without naming any messaging or event-delivery technology?

## Decision

A Domain Event is the durable record of a completed business fact already enforced by the Domain layer of its owning bounded context; it never itself carries or replaces business logic, and it never grants access across an aggregate boundary. The twelve Domain Events already named in `DOMAIN_MODEL.md` are the complete, official list for v1 — no new event is introduced by this ADR. Each event is owned by the same bounded context that owns the aggregate whose state change it represents (`ADR-002`). An event is raised by Domain only once — or as an inseparable part of — the transaction that made the underlying fact true (`ADR-004`), is recognized by the Application layer (`ADR-005`), and today serves primarily to trigger the Audit concern (CONST-2). Any consumption of an event by a bounded context other than its owner is a read of a completed fact only — it never bypasses `ADR-002`'s no-cross-context-write rule, and it never substitutes for that consuming context's own Domain-layer business rules if a reaction is needed.

## What Qualifies as a Domain Event

- Only an occurrence already named in `DOMAIN_MODEL.md` and corresponding to a business rule or invariant already enforced by Domain qualifies: TutorRegistered, TutorApproved, TutorSuspended, RelationshipInvited, RelationshipConfirmed, AvailabilityDeclared, SessionBooked, SessionRescheduled, SessionCancelled, SessionCompleted, SessionMarkedNoShow, BookingConflictResolved.
- A Domain Event represents a completed, past-tense business fact — never an intention, a request, or a possible future action (`DOMAIN_MODEL.md`: Domain Events).
- A Domain Event does not itself decide anything; it records that a decision already made by Domain has occurred. It never replaces or duplicates the business rule that produced it.
- No occurrence outside this list is a Domain Event under this ADR. Introducing a new one requires first amending `DOMAIN_MODEL.md`, then extending this ADR — never invented ad hoc during implementation.

## Event Ownership

Each event is owned by the same bounded context that owns the aggregate whose state it reports (`ADR-002`: Context Ownership):

| Bounded Context | Events Owned |
|---|---|
| **Scheduling & Booking** | AvailabilityDeclared, SessionBooked, SessionRescheduled, SessionCancelled, SessionCompleted, SessionMarkedNoShow |
| **Identity & Relationship** | TutorRegistered, TutorApproved, TutorSuspended, RelationshipInvited, RelationshipConfirmed |

**BookingConflictResolved** is not cleanly assignable: `ADR-002` establishes that Marketplace Oversight owns no aggregate, and the exact mechanics of "resolve booking conflicts" — including whether it produces any state change of its own or only acts through Scheduling & Booking's existing logic — are unresolved (`DOMAIN_MODEL.md` Open Question 13). Ownership of this event is therefore an Open Question, not decided here.

No context may raise an event on behalf of an aggregate it does not own (`ADR-002`: Data Ownership Rules; `ADR-004`: Aggregate Persistence Rules).

## Event Publication Rules

- An event is published — made available for recognition, per `ADR-005`'s description of Application "recognizing" an event — only by the Application layer of the owning context, after Domain has completed enforcing the corresponding rule.
- Publishing an event never itself performs a write to another context's aggregate; it only communicates that a fact is now true (`ADR-002`: Integration Rules).
- The specific mechanism of publication is not chosen here; it remains bound to the already-open Architectural Decision Candidate on inter-context communication (`ARCHITECTURE.md` Section 21, Item 6).

## Event Consumption Rules

- A bounded context other than the owner may only read the fact a event represents; it may never use the event as a channel to write into the owning context's aggregate, nor to bypass that aggregate's own use case (`ADR-002`: Integration Rules).
- A consuming context must not re-implement or duplicate the business rule that produced the event. If a consuming context needs to react with a state change of its own, that reaction is its own business rule, evaluated by its own Domain layer, merely triggered by the event as an input fact.
- The only consumption established today is the Application layer's own recognition of an event, in order to invoke the Audit concern (`ADR-005`: Domain Event Coordination). Whether any bounded context must consume another context's event to trigger its own reaction is not established by any approved document (`ADR-005` Open Question 2) and is carried forward here.

## Cross-Context Event Rules

- An event may be observed by a bounded context other than its owner only as a read of a completed fact — the same "read, not copy, not write" pattern already fixed for cross-context data access in `ADR-002` and `ADR-004`, applied here to events.
- Consuming an event never grants the consuming context authority over the owning context's aggregate; authority remains exactly where `ADR-002` and `ADR-003` already placed it.
- Cross-context event consumption must never be used to reconstruct a second persisted copy of the owning context's state, since this would violate the single-source-of-truth requirement already restated in `ADR-004`'s Read Model Principles (CONST-3).

## Transaction Relationship

- A Domain Event is raised only as part of, or immediately following, the same atomic transaction that made the underlying fact true (`ADR-004`: Transaction Boundaries). An event is never raised for a fact that could still be rolled back.
- Because `ADR-004` forbids a transaction spanning two bounded contexts, an event is never used to simulate a distributed transaction across contexts. Any cross-context reaction to an event is its own, separately transacted business operation in the consuming context (`ADR-004`: Transaction Boundaries; `ADR-005`: Transaction Coordination Principles).
- Whether the recognition of an event (for audit purposes) must occur within the same transaction as the aggregate mutation it reports, or may occur immediately after, is not established by any approved document — this mirrors the same open question already raised in `ADR-004` about audit-entry transactionality, applied specifically to events, and is carried forward here.

## Audit Relationship

- The established purpose of Application recognizing a Domain Event is to invoke the Audit concern, recording the acting identity, role, action, and timestamp (CONST-2; `ADR-004`: Audit Persistence Principles; `ADR-005`: Domain Event Coordination).
- Every event corresponding to a CONST-2-covered action — creating, rescheduling, or cancelling a Session, or changing Availability — must result in an audit entry. The same standard extends to TutorApproved and TutorSuspended (`ARCHITECTURE.md` Section 16; `ADR-003`: Audit Requirements).
- Whether every one of the twelve named events (for example, RelationshipInvited, RelationshipConfirmed, or BookingConflictResolved) independently requires its own audit entry, or is covered indirectly by the audit entry of the action that produced it, is not fully settled by the approved documents — see Open Questions.

## Reliability Principles

- If a Domain Event's corresponding audit entry cannot be durably recorded, this must be surfaced as an explicit failure, never silently dropped — consistent with `PROJECT_CONSTITUTION.md`'s Engineering Principle 4 (fail safely and visibly) and `ADR-004`'s rule that an audit entry must never be lost.
- A Domain Event must never be raised for a fact that did not actually happen; it is strictly a record of what the Domain layer already enforced, never a speculative or best-guess notification.
- No approved document establishes a retry, redelivery, or at-least-once/exactly-once guarantee for event recognition; this ADR does not assume one — see Open Questions.

## Event Ordering Principles

- Events concerning the same aggregate (for example, SessionBooked followed later by SessionCancelled for the same Session) must be recognized in the order their underlying transactions actually completed, since CONST-2's attributability and the Session's single-status invariant (`PRODUCT_REQUIREMENTS.md` SCH-6; `DOMAIN_MODEL.md`: Invariants) depend on a coherent history.
- No ordering guarantee is established or assumed across different aggregates or different bounded contexts beyond what is already implied by their independent transaction boundaries (`ADR-004`).
- The specific mechanism for guaranteeing in-aggregate ordering is not chosen here; only the requirement itself is established.

## Alternatives Considered

- **Domain Events carrying business logic** (for example, a handler that itself decides whether a rule is satisfied) — rejected. A Domain Event never replaces business logic; it is always a fact reported after the Domain layer has already decided, per this ADR's Decision and `ADR-002`'s single-place-of-enforcement rule.
- **Domain Events used to write across aggregate boundaries** (a consuming context updating another context's persisted state upon receiving an event) — rejected. This is precisely what `ADR-002`'s no-cross-context-write rule forbids, and Domain Events never bypass an aggregate boundary.
- **Domain Events used as the mechanism for a cross-context distributed transaction** (a saga-like pattern where multiple contexts commit based on an event chain) — rejected. `ADR-004` explicitly forbids a transaction spanning two contexts, and no approved document establishes any compensating-transaction rule; introducing one here would invent a business rule the documents do not support.
- **Domain Events used only for audit-triggering, with no cross-context consumption established beyond that (chosen)** — the only option the approved documents currently support, matching `ADR-005`'s Domain Event Coordination section exactly, while leaving the broader question of cross-context event-driven reactions an explicit Open Question rather than an invented resolution.
- **Introducing new Domain Events beyond the twelve already named** (for example, a separate event for an authorization denial) — rejected for this ADR. `DOMAIN_MODEL.md` already fixed the official list; any addition requires amending that document first, through the Constitution's Decision-Making Process, not invention here.

## Consequences

- Every Domain Event has a single, unambiguous owning context (with the one noted exception, BookingConflictResolved, left open), and every occurrence of an event corresponds to a transaction already completed within that context.
- The Audit concern is the de facto primary consumer of Domain Events for v1, since no other cross-context consumption is currently established.
- Any future cross-context reaction to an event must be introduced as its own explicit business rule in the reacting context's own Domain layer, proposed and approved like any other Structural decision — never added silently as a side effect of observing an event.

## Risks

- If a future implementer treats an event as an implicit invitation to duplicate the producing context's business logic in a consumer, this would violate `ADR-002` and this ADR's own rule that Domain Events never replace business logic — this must be actively guarded against.
- The unresolved question of whether audit-entry creation must be transactionally tied to the triggering event risks an event's audit record being lost if implemented carelessly, undermining CONST-2's non-negotiable attributability.
- Ownership of BookingConflictResolved is unresolved, risking an inconsistent implementation of that event's producing logic until `DOMAIN_MODEL.md` Open Question 13 is resolved.
- Absent an established ordering guarantee across aggregates or contexts, an implementer might assume an ordering the documents do not guarantee, risking incorrect assumptions in any future cross-context consumer.

## Future Evolution

If a documented, approved need arises for a bounded context to react to another context's event with its own business rule — resolving `ADR-005` Open Question 2 and the related open questions in this ADR — that reaction is introduced as a new, explicit business rule in the reacting context's own Domain layer, following the Constitution's Decision-Making Process, not assumed as a default behavior of merely observing the event. If new bounded contexts are added in the future (per `ADR-002`'s Future Evolution), any new Domain Events they raise follow the same ownership, publication, and audit rules established here. The specific inter-context communication mechanism, and any delivery-guarantee mechanism for event recognition, remain open Architectural Decision Candidates (`ARCHITECTURE.md` Section 21, Item 6) to be resolved separately.

## Open Questions

1. Does any Domain Event need to trigger a reaction in a bounded context other than the one that raised it? (`ADR-005` Open Question 2)
2. Which bounded context owns the BookingConflictResolved event, given Marketplace Oversight owns no aggregate and the exact mechanics of "resolve booking conflicts" are unresolved? (`DOMAIN_MODEL.md` Open Question 13; `ADR-002` Open Question 2)
3. Must the recognition of a Domain Event, for audit purposes, occur within the same transaction as the aggregate mutation it reports, or may it occur immediately after? (Mirrors `ADR-004` Open Question 6)
4. Must every one of the twelve named Domain Events independently produce its own audit entry, or are some covered indirectly by the audit entry of the triggering action? Not addressed by any approved document.
5. What delivery guarantee (for example, at-least-once recognition) applies if an event's audit-triggering recognition initially fails? Not addressed by any approved document.
6. Which role(s) may transition a Session to Completed or No-Show, affecting who triggers the SessionCompleted/SessionMarkedNoShow events? (`DOMAIN_MODEL.md` Open Question 6)

## Traceability

| Element of this ADR | Source |
|---|---|
| The twelve named Domain Events | `DOMAIN_MODEL.md`: Domain Events |
| Events raised by Domain, recognized by Application | `ADR-005`: Domain Event Coordination |
| Event ownership follows aggregate ownership | `ADR-002`: Context Ownership |
| No cross-context writes via events | `ADR-002`: Integration Rules |
| Events never span a transaction across contexts | `ADR-004`: Transaction Boundaries |
| Audit purpose of event recognition | `PRODUCT_REQUIREMENTS.md` CONST-2; `ADR-004`: Audit Persistence Principles; `ADR-003`: Audit Requirements |
| Single source of truth preserved against event-derived copies | `PRODUCT_REQUIREMENTS.md` CONST-3; `ADR-004`: Read Model Principles |
| Fail safely and visibly on event/audit failure | `PROJECT_CONSTITUTION.md`: Engineering Principle 4 |
| Session single-status invariant informing ordering | `PRODUCT_REQUIREMENTS.md` SCH-6; `DOMAIN_MODEL.md`: Invariants |
| Deferred inter-context communication mechanism | `ARCHITECTURE.md` Section 21, Item 6 |

---

*Status: Accepted. Status field corrected 2026-07-21 to match how every later ADR and the shipped implementation already treat this document — see the Status section above.*
