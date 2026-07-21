**Authoritative sources (in priority order):** `PROJECT_CONSTITUTION.md`, `PRODUCT_REQUIREMENTS.md`, `BUSINESS_MODEL.md`, `DOMAIN_MODEL.md`, `ARCHITECTURE.md`, `docs/adr/ADR-001` through `ADR-011` (all approved and immutable, except `ADR-003` and `ADR-011`, which are themselves Proposed). This ADR chooses no messaging technology, no message broker, no queue, no library, and does not decide `ARCHITECTURE.md` Section 21, Item 6 (the inter-context communication mechanism), which remains separately deferred.

---

# ADR-012: Domain Event Dispatch — Delivery Semantics

## Status

**Accepted — 2026-07-20.**

This ADR is binding architecture. The four decisions recorded below govern all Domain Event Dispatch implementation from this point forward. No implementation may deviate from them without a superseding ADR approved through the Constitution's Decision-Making Process.

## Architecture Decision Summary

1. **Delivery Mode:** Domain Event Dispatch is synchronous and in-process.
2. **Ordering:** Ordering is guaranteed within a single aggregate's own raised-event sequence only; ordering between different aggregates participating in the same use case is not guaranteed.
3. **Failure Behavior:** A listener failure propagates — dispatch fails, the unit of work fails, the transaction rolls back, and the caller receives failure. No listener failure may be swallowed; no best-effort behavior; no partial success.
4. **Delivery Guarantee:** Exactly one dispatch attempt per application transaction. If dispatch fails, the transaction is considered failed and the caller retries the entire use case. The system performs no automatic retry, redelivery, retry queue, or retry scheduler.

## Context

`ADR-006` established that a Domain Event is the durable record of a completed business fact, owned by the same bounded context that owns the aggregate it reports, recognized by the Application layer (`ADR-005`: Domain Event Coordination). `ADR-006` left open how that recognition behaves in practice: delivery timing, ordering guarantees beyond a single aggregate, and reliability/retry semantics. The Application Pipeline phase built the mechanical seam that harvests each touched aggregate's `DomainEvents` and clears them at the point `IUnitOfWork.SaveChangesAsync` commits, registering zero consumers — deliberately, so that no delivery-semantics question would be answered by implementation before being explicitly decided. This ADR is that explicit decision.

## Decision

Domain Event Dispatch is synchronous and in-process, preserves ordering within a single aggregate only, propagates listener failures to the triggering use case, and provides exactly one dispatch attempt per transaction with no automatic retry. Each is recorded as its own numbered decision below, together with the rationale the Business Owner approved it under.

## Scope Boundary

- This ADR concerns only a single bounded context's own Application layer recognizing events raised by aggregates it owns. It does not decide whether one bounded context ever reacts to another's event, which remains `ADR-005` Open Question 2 and `ADR-006` Open Question 1, both still open and untouched here.
- This ADR does not decide `ARCHITECTURE.md` Section 21, Item 6 (the inter-context communication mechanism — e.g., in-process mediator vs. another integration mechanism). The mechanism this ADR establishes must not be repurposed for cross-context routing without that separate, still-open decision being made first.
- This ADR does not decide whether every Domain Event requires its own audit entry (`ADR-006` Open Question 4), which bounded context owns `BookingConflictResolved` (`ADR-006` Open Question 2), or whether an audit entry must share a transaction with its triggering mutation (`ADR-004` Open Question 6, resolved for the audit-specific case by `ADR-016`, itself still Proposed) — these remain the Audit phase's own concern, not Dispatch's.

## Decision 1 — Delivery Mode: Synchronous, In-Process

**Decision:** Domain Event Dispatch shall be synchronous and in-process.

**Meaning:**
- Event handlers execute inside the same application call.
- No background processing.
- No in-process queue.
- No message broker.
- No Outbox.
- No asynchronous dispatch.
- No external infrastructure.

**Rationale:** Consistency with `PROJECT_CONSTITUTION.md` Engineering Principle 1 (correctness before convenience), Engineering Principle 4 (fail safely and visibly), Architecture Principle 5 (simple, verifiable mechanisms over cleverness), Architecture Principle 6 (defer irreversible technical decisions), Core Principle 5 (narrowest defensible scope), and CONST-2 (non-negotiable attributability, most directly satisfiable when dispatch shares the same call stack, and — per `ADR-016` — the same transaction, as the triggering mutation).

**Alternatives rejected:** Asynchronous, in-process delivery was rejected as introducing engineering complexity to guarantee anything about timing relative to the transaction, with no corresponding demonstrated need in any approved document. Asynchronous, out-of-process delivery was rejected as new infrastructure (a message broker/queue) unjustified by any approved requirement.

## Decision 2 — Ordering

**Decision:** Dispatch must preserve ordering only within the same aggregate. There is no architectural guarantee regarding ordering between different aggregates participating in the same use case.

Guaranteed: events raised by one aggregate (for example, Aggregate A: Event1, Event2, Event3) will always be recognized in the order they were raised.

Not guaranteed: the relative ordering between an event raised by one aggregate and an event raised by a different aggregate within the same use case (for example, Aggregate A's event vs. Aggregate B's event) is intentionally unspecified.

**Rationale:** Same-aggregate ordering was already required by `ADR-006` and is satisfied as a direct consequence of Decision 1 — synchronous, in-process, single-call-stack delivery processes each aggregate's own event list in insertion order. No approved document establishes a need for cross-aggregate ordering, consistent with Architecture Principle 7 (evolvability over premature generality).

## Decision 3 — Failure Behavior

**Decision:** Listener failure shall propagate.

If any listener throws:
- Dispatch fails.
- The unit of work fails.
- The transaction rolls back.
- The caller receives failure.

No listener failure may be swallowed. No best-effort behavior. No partial success.

**Rationale:** Consistency with Engineering Principle 1 (correctness before convenience), CONST-2 (audit attributability is non-negotiable, not best-effort), and `ADR-008`'s existing rule that no partially-applied effect may stand unless the underlying Domain rule has actually and fully succeeded. A failed, explicitly visible, retriable attempt is the approved outcome in preference to a mutation that succeeds while its audit record is silently lost.

**Accepted residual risk:** because dispatch and the triggering mutation share a transaction, a bug in an unrelated listener can cause a legitimate, otherwise-correct business mutation (for example, a valid booking) to be reported as failed to the acting Student, Parent/Guardian, or Tutor. This trade-off was weighed against the alternative — silently completing a mutation with a missing audit record — and rejected in favor of failing safely and visibly, consistent with Engineering Principle 4. This risk is accepted, not unresolved.

## Decision 4 — Delivery Guarantee

**Decision:** Exactly one dispatch attempt per application transaction.

If dispatch fails, the transaction is considered failed and the caller retries the entire use case. The system shall not perform automatic retry. No redelivery. No retry queue. No retry scheduler.

**Rationale:** The strongest guarantee achievable without introducing new retry infrastructure, consistent with Architecture Principle 6 and Core Principle 5, and the direct consequence of Decisions 1 and 3 above.

## Related, Separately Open Questions (Not Resolved Here)

This acceptance resolves only the four decisions above. The following remain open, unaffected by this ADR, and are not to be inferred as decided:

- Whether a bounded context must ever consume another context's event to trigger its own reaction (`ADR-005` Open Question 2; `ADR-006` Open Question 1).
- Which bounded context owns the `BookingConflictResolved` event (`ADR-006` Open Question 2; `DOMAIN_MODEL.md` Open Question 13).
- Whether every one of the twelve named Domain Events independently requires its own audit entry (`ADR-006` Open Question 4).
- The inter-context communication mechanism (`ARCHITECTURE.md` Section 21, Item 6).
- Audit durability specifics beyond what this ADR's delivery mode enables (`ADR-016`, itself still Proposed).

---

*Status: Accepted — 2026-07-20. This ADR governs all Domain Event Dispatch implementation. Superseding this decision requires a new ADR approved through the Constitution's Decision-Making Process.*
