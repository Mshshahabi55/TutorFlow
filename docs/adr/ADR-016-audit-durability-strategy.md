**Authoritative sources (in priority order):** `PROJECT_CONSTITUTION.md`, `PRODUCT_REQUIREMENTS.md`, `BUSINESS_MODEL.md`, `DOMAIN_MODEL.md`, `ARCHITECTURE.md`, `docs/adr/ADR-001` through `ADR-015` (all approved and immutable, except `ADR-003`, `ADR-011`, `ADR-012`, `ADR-013`, `ADR-014`, and `ADR-015`, which are themselves Proposed), `docs/database/PHYSICAL_DATABASE_STRATEGY.md`. This ADR chooses no messaging technology, no Outbox implementation, and no persistence product. It exists to resolve the audit-durability question already carried as an open item across `ADR-004`, `ADR-006`, and `ADR-009`, and to evaluate — not adopt — an Outbox-style pattern as one candidate answer.

---

# ADR-016: Audit Durability Strategy

## Status

**Accepted — 2026-07-20.**

**Business decision:** same-transaction audit durability. The audit write is staged into the same Scoped `TutorFlowDbContext` the triggering use case's `EfUnitOfWork.SaveChangesAsync()` already commits, so the audit entry and the aggregate mutation succeed or fail together, in one database transaction. No Outbox, no separate-mechanism/best-effort write, no new infrastructure. Failure detection is provided entirely by `ADR-012` Decision 3 (already Accepted): an audit-write failure is a listener failure, which propagates, rolls back the transaction, and returns an explicit failure to the caller through the existing error-handling path — no additional failure-detection mechanism is required. This is the formal answer to `ADR-004` Open Question 6, `ADR-006` Open Question 3, and `ADR-009` Open Question 3, and to this ADR's own three Questions Requiring Approval.

All four of this ADR's dependencies — `ADR-012` (synchronous, in-process dispatch), `ADR-013` (PostgreSQL), `ADR-014` (unique-constraint concurrency, no internal retry-and-recommit), `ADR-015` (AvailabilitySlot/Session ratified as separate aggregates, one transaction) — are Accepted, and their specific outcomes leave same-transaction durability as the only candidate requiring no separate governance action.

## Context

CONST-2 requires every qualifying action be durably and completely auditable. `ADR-004`'s Audit Persistence Principles state the audit entry "must never be lost," but explicitly defer whether it must be persisted within the same transaction as the aggregate mutation it records, or through a separate mechanism (`ADR-004` Open Question 6, echoed in `ADR-006` Open Question 3 and `ADR-009` Open Question 3). Separately, `ADR-012` (Proposed, unresolved) establishes that Domain Event Dispatch's own delivery semantics — synchronous vs. asynchronous, retry, delivery guarantee — are themselves undecided. Audit is architected as a consumer of Dispatch (`ADR-006`: "the Audit concern is the de facto primary consumer of Domain Events"), which is why these two open decisions are directly linked.

## Problem Statement

Given CONST-2's non-negotiable "never lost" requirement and the still-open question of Dispatch's own delivery semantics (`ADR-012`), what durability strategy should govern how an audit entry is persisted relative to the business mutation it records — including whether an Outbox-style pattern is warranted — without assuming a persistence technology (`ADR-013`) or resolving `ADR-012` itself?

## Dependencies on Other Open Decisions

- **Directly and materially depends on which of `ADR-012`'s three delivery candidates is eventually chosen** — not a simple synchronous/asynchronous binary:
  - **Synchronous, in-process:** same-transaction audit durability is straightforwardly achievable, since the audit-write call occurs within the same call stack as the triggering use case and can be included in the same database transaction `IUnitOfWork.SaveChangesAsync` already demarcates.
  - **Asynchronous, in-process:** same-transaction durability is not straightforward, but is not strictly foreclosed either — if the queued work is drained and completed before the encompassing transaction commits, it remains possible in principle, at the cost of engineering complexity that partially defeats the purpose of making dispatch asynchronous in the first place.
  - **Asynchronous, out-of-process:** same-transaction durability is **not** achievable — by definition, processing occurs in a separate execution context after the triggering transaction has already committed.
  This ADR's candidate set is therefore conditional on which of these three `ADR-012` selects, not on a same-transaction-vs-not binary. An earlier draft of this ADR collapsed these three into "synchronous vs. asynchronous"; that collapse is corrected here per Enterprise Architecture Review Board finding.
- **Depends on `ADR-013` (Persistence Technology)** for what "same transaction" is even physically capable of meaning, and for whether the chosen technology has native support for an Outbox-style pattern's own durable "pending record" table.
- **Depends on `ADR-014` (Concurrency Control Strategy).** If the chosen concurrency mechanism involves an internal retry-and-recommit sequence (for example, under optimistic concurrency), "the transaction" an audit entry might share is not necessarily the first attempt at booking — this ADR's same-transaction candidate must be evaluated against whatever transactional shape `ADR-014` ultimately produces, not assumed to be a single, non-retried write.
- **No known upstream dependent** within this batch, though it is the final gate before the Audit phase of the ratified roadmap can begin.

## Constraints

- An audit entry, once its triggering action has been decided as auditable (the CONST-2-named actions plus Tutor approval/suspension, per `ADR-009`'s Business Event Audit Rules), must never be permanently lost (`ADR-004`).
- This ADR's answer must not silently assume an outcome for `ADR-012` — if a candidate is only available under one of `ADR-012`'s three delivery modes, that conditionality must be stated, not hidden.
- Must not introduce new infrastructure (a message broker/queue) by default. **This ADR does not itself carry authority to relax the standing, project-wide "no new infrastructure" governance rule** — that rule was established outside any single ADR, and the user has explicitly stated it should not be embedded into one. The Outbox candidate below is evaluated on its architectural merits only; if it is ever adopted, doing so requires the "no new infrastructure" rule to be independently revisited through the same governance process that established it, not through this ADR's own approval.
- Must remain consistent with `ADR-004`'s Transaction Boundaries (a transaction never spans two bounded contexts); an audit entry for a cross-context-orchestrated action is recorded by the owning context, per `ADR-009`'s Cross-Context Audit Rules, already decided and not reopened here.
- **Must meet the GDPR-grade personal-data protection standard already established for audit records generally** (`PRODUCT_REQUIREMENTS.md` CONST-4; `ADR-009`: Security Audit Rules — "Personal data within an audit record must still meet the GDPR-grade protection standard already established for personal data generally... audit creates no exception to that standard"). This ADR does not itself resolve any data-protection question `ADR-009` leaves open, and durability is not a substitute for data protection: a mechanism that is reliable but fails to meet CONST-4 is not an acceptable outcome under this ADR regardless of its durability characteristics.

## Alternatives Considered

- **Same-transaction durability** — the audit write is included in the exact same database transaction as the aggregate mutation, so both succeed or both roll back together. *Conditional*: straightforwardly achievable only if `ADR-012` resolves to synchronous, in-process delivery; possible in principle but materially harder to guarantee under asynchronous, in-process delivery; foreclosed entirely under asynchronous, out-of-process delivery (see Dependencies above for the full three-way breakdown).
- **Separate-mechanism, best-effort** — the audit write happens as its own operation after the triggering mutation commits, with no cross-transaction guarantee; if the audit write itself fails, the business mutation still stands, and the gap is only detected by whatever monitoring exists (none currently does).
- **Outbox pattern** — the *fact* that an audit entry needs to be written is recorded, within the same transaction as the business mutation, as a durable "pending outbox" record; a separate process later reads pending records and performs the actual audit write, retrying until successful. Achieves durability without requiring the audit write itself to be synchronous or in the triggering transaction, at the cost of a new moving part (an outbox table plus a processor) — arguably "new infrastructure" in substance even without a separately deployed service, so adopting it requires the standing "no new infrastructure" rule to be independently revisited first (see Constraints above). It also introduces an additional durable staging location holding personal-data-bearing audit content pending processing, which must independently satisfy CONST-4/`ADR-009`'s protection standard — a distinct question from the pattern's reliability merits.

## Trade-offs

- **Same-transaction:** strongest guarantee (the mutation cannot succeed without the audit record, or vice versa) where available. Straightforwardly available under `ADR-012`'s synchronous, in-process candidate; possible in principle, though materially harder to guarantee, under the asynchronous, in-process candidate; foreclosed entirely under the asynchronous, out-of-process candidate (see Dependencies above).
- **Separate-mechanism/best-effort:** simplest to implement, compatible with any `ADR-012` outcome, but carries a real risk of a silent audit gap if the secondary write fails — in tension with CONST-2's "never lost" language unless paired with its own, currently undecided, failure-detection mechanism.
- **Outbox:** durable without requiring synchronous delivery, but the most structurally novel of the three; its own reliability depends on a working "processor" component that itself needs delivery/retry semantics decided — potentially reintroducing, in miniature, the exact kind of question `ADR-012` already exists to answer for Dispatch generally.

## Risks

- Deciding this ADR before `ADR-012` risks selecting "same-transaction" only to have `ADR-012` later resolve to asynchronous delivery, foreclosing that choice after the fact.
- Under-specifying failure-detection for the "separate-mechanism, best-effort" candidate risks a genuine, undetected violation of CONST-2's non-negotiable framing.
- The Outbox candidate's own reliability mechanism, if not carefully scoped, could become a second, uncoordinated instance of delivery-semantics decision-making, potentially inconsistent with whatever `ADR-012` eventually decides for Dispatch generally.

## Impact on Existing Architecture

None currently — no production audit consumer exists yet. `NullDomainEventDispatcher` (from the Domain Event Dispatch abstraction phase) remains a provable no-op; this decision governs whatever eventually replaces it as the first real listener.

## Impact on Clean Architecture

The audit write, whichever mechanism is chosen, must remain an Infrastructure-layer concern implementing an Application-defined interface, consistent with `ADR-005`. This ADR does not propose placing audit-recording logic in Domain or Web under any candidate.

## Impact on DDD

None to aggregate boundaries. Confirms, without changing, that the audit record is derived data, not a primary Domain entity with its own business behavior (`DOMAIN_DATA_MODEL.md` Section 16).

## Impact on Testing

Whichever mechanism is chosen needs a failure-injection test ("the audit write fails — does the business mutation still stand, and is the gap detectable?"). This test cannot be meaningfully designed until both this ADR and `ADR-012` are resolved.

## Recommendation

No mechanism is recommended or ranked. Procedurally: this decision should be treated as explicitly gated on `ADR-012`'s Required Decision 1, and should also be resolved with `ADR-013` and `ADR-014`'s outcomes known. If the business owner is prepared to decide these together, note how each of `ADR-012`'s three delivery candidates bears on this ADR's candidate set: under synchronous, in-process delivery, the same-transaction candidate is straightforwardly available; under asynchronous, in-process delivery, same-transaction remains possible in principle but is materially harder to guarantee, making separate-mechanism or Outbox worth weighing alongside it; under asynchronous, out-of-process delivery, same-transaction is foreclosed entirely and this ADR's viable candidates narrow to separate-mechanism or Outbox. If Outbox is the intended direction under any of these, the "no new infrastructure" governance rule must be revisited first, as a separate, explicit governance action outside this ADR's own authority.

## Assumptions

The set of auditable actions is exactly what `ADR-009`'s Business Event Audit Rules already establish (booking, rescheduling, cancelling a Session; changing Availability; Tutor approval/suspension). This ADR does not expand or contract that list, which remains `ADR-009`'s own, separately unresolved edges.

## Non-Goals

Does not decide `ADR-012`'s delivery semantics — a prerequisite to, not a subject of, this ADR. Does not decide whether a Domain Error rejection or a denied authorization attempt must also be audited (`ADR-008` Open Question 2; `ADR-003` Open Question 6 — both remain separately open). Does not choose a persistence technology (`ADR-013`) or a concurrency-control mechanism (`ADR-014`) — both are prerequisites this ADR depends on, not subjects it resolves. Does not resolve any CONST-4/GDPR data-protection question left open by `ADR-009`; it only requires that whatever is chosen not violate that standard.

## Questions Requiring Approval — Answered

1. **Decided after `ADR-012`.** `ADR-012` resolved to synchronous, in-process dispatch, narrowing this ADR's candidate set exactly as anticipated: same-transaction durability is straightforwardly available.
2. **Outbox is not adopted; the "no new infrastructure" constraint is not tested.** Same-transaction durability requires no Outbox, so this question does not arise in practice.
3. **No additional failure-detection mechanism is required.** `ADR-012` Decision 3's propagate-and-rollback behavior, surfaced through the existing `Result`/exception-handling path (`ADR-008`), is the failure-detection mechanism.

## Scope Note

The set of auditable actions for this implementation is exactly `ADR-009`'s Business Event Audit Rules: SessionBooked, SessionRescheduled, SessionCancelled, AvailabilityDeclared, TutorApproved, TutorSuspended. The other six named Domain Events (TutorRegistered, RelationshipInvited, RelationshipConfirmed, SessionCompleted, SessionMarkedNoShow, BookingConflictResolved) remain outside this scope, per `ADR-006` Open Question 4 / `ADR-009` Open Question 4, still open and not resolved by this ADR. Identity and role captured in each audit record are read from `ICurrentUserProvider` as-is (currently unauthenticated/null pending `ADR-011`, itself still frozen) — never fabricated.

---

*Status: Accepted — 2026-07-20. Mechanism: same-transaction durability via shared DbContext staging.*
