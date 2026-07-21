**Authoritative sources (in priority order):** `PROJECT_CONSTITUTION.md`, `PRODUCT_REQUIREMENTS.md`, `BUSINESS_MODEL.md`, `DOMAIN_MODEL.md`, `ARCHITECTURE.md`, `docs/adr/ADR-001` through `ADR-014` (all approved and immutable, except `ADR-003`, `ADR-011`, `ADR-012`, `ADR-013`, and `ADR-014`, which are themselves Proposed), `docs/database/DOMAIN_DATA_MODEL.md`. This ADR does not propose a new aggregate boundary; it evaluates the boundary already implemented in code against `ADR-004`'s existing requirements and asks for its explicit ratification or explicit revision.

---

# ADR-015: Transaction Boundary Confirmation

## Status

**Accepted — 2026-07-20.**

**Business decision:** the implemented boundary is ratified as-is. `AvailabilitySlot` and `Session` remain two separate aggregate roots, coordinated only through `AvailabilitySlot.Book(...)`, exactly as already built and tested. This is the formal answer to `DOMAIN_MODEL.md` Open Question 17, `ADR-002` Open Question 3, and `ADR-004` Open Question 1.

## Context

`ADR-004` logically defined the booking transaction boundary: "checking and consuming an Availability Slot and creating the resulting Session — is a single atomic transaction boundary." At the time `ADR-004` was approved, `DOMAIN_MODEL.md` Open Question 17 (is Availability Slot a separate aggregate from Session, or a sub-component of the Tutor aggregate) was, and formally remains, unresolved in the documentation set. The Domain layer, as actually implemented and verified across this engagement, already embodies a specific answer: `AvailabilitySlot` and `Session` are two separate `AggregateRoot<TId>` types, coordinated only through `AvailabilitySlot.Book(...)`, with `Session.Book(...)` deliberately marked `internal` to prevent any other path to Session creation. This is a real, working, tested implementation that three completed roadmap phases (Application Pipeline, Domain Event Dispatch, Read Side) have already been built against. The Architecture Execution Review (an earlier phase of this engagement) flagged this exact situation — a real architectural decision made through implementation rather than through explicit approval — as the same category of gap `ADR-011` was created to close for authentication.

## Problem Statement

Given the aggregate boundary already implemented in code, does it correctly and sufficiently satisfy `ADR-004`'s transaction-boundary requirement, CONST-1, and `ADR-002`'s single-bounded-context-per-transaction rule — such that it can be formally ratified as the answer to `DOMAIN_MODEL.md` Open Question 17, `ADR-002` Open Question 3, and `ADR-004` Open Question 1 — or does it require revision before `ADR-013` and `ADR-014` are designed against it?

## Dependencies on Other Open Decisions

- **No upstream architectural dependency.** This ADR concerns ratifying (or revising) an existing Domain structure, not selecting among competing technologies — it does not itself depend on `ADR-013` or `ADR-014`.
- **Gates both `ADR-013` (Persistence Technology) and `ADR-014` (Concurrency Control Strategy), which are resolved in parallel once this ADR's outcome is known.** Both of those ADRs are being designed against the assumption that this boundary holds. If this ADR is not resolved, or is resolved by revision rather than ratification, both must be revisited. This reflects a correction: an earlier draft of `ADR-013` described itself as fully independent of this ADR; that claim has been withdrawn in `ADR-013` Revision 1, and both documents now agree on this direction.

## Constraints

- Whatever is ratified or revised must keep the transaction boundary within a single bounded context (Scheduling & Booking) — `ADR-004` already forbids a transaction spanning two contexts' owned aggregates.
- Must continue to satisfy CONST-1 as a hard, structurally-enforced invariant, not a merely-documented convention.
- Any revision would itself be a Domain model change, explicitly out of scope for implementation under this roadmap phase's own governance rule ("No Domain model changes unless explicitly approved") — this ADR may only recommend that a revision be considered, never perform one.

## Alternatives Considered

- **Ratify the implemented boundary as-is** — `AvailabilitySlot` and `Session` remain two separate aggregate roots; the transaction boundary is "read-and-mutate-AvailabilitySlot plus create-Session," both within Scheduling & Booking, exactly as `ADR-004` already describes and exactly as the code already does.
- **Availability Slot as a sub-component of the Tutor aggregate** — the alternative `DOMAIN_MODEL.md` itself named at Open Question 17. Under this structure the transaction boundary would instead span Tutor+Session, crossing into Identity & Relationship's aggregate for a Scheduling & Booking operation — a structural change with consequences for `ADR-002`'s context-ownership model well beyond this ADR's scope.
- **A different, previously unnamed boundary** — not proposed here. Inventing a third structural candidate this late, with a working implementation already in place, would itself be new architecture, which this phase's governance explicitly forbids.

## Trade-offs

Per Enterprise Architecture Review Board finding, both alternatives are given substantive architectural treatment below, not only cost treatment.

- **Ratifying as-is — architectural merits, not only cost:** `AvailabilitySlot` as a small, independent aggregate root — owning exactly the "not consumed twice" invariant, referencing `Tutor` by identity only, and producing `Session` as a new aggregate rather than mutating a larger parent — is consistent with standard aggregate-design heuristics (favor small aggregates sized to their true invariant; reference other aggregates by identity; one aggregate mutated per transaction). It is also the only structure consistent with `ARCHITECTURE.md` Section 4's already-approved Bounded Context Mapping, which explicitly assigns "Availability Slot and Session lifecycle: declaring availability, booking, cancelling, rescheduling, status transitions" to Scheduling & Booking — not to Identity & Relationship. Separately, it carries zero implementation cost, since the boundary is already built, tested, and depended upon by three completed roadmap phases.
- **Reverting to "slot as Tutor sub-component" — architectural merits, not only cost:** this alternative could be argued to better reflect that an Availability Slot has no meaning independent of the Tutor who declared it, and that "defining availability" is described alongside a Tutor's other offering attributes in the same user-journey step (`PRODUCT_REQUIREMENTS.md` 5.3 step 3: "define the session duration(s) offered, hourly rate, and availability" — one combined step, not three separate ones). Weighed against this genuine structural argument: `ARCHITECTURE.md` Section 4 already assigns Availability Slot/Session lifecycle to Scheduling & Booking, meaning this alternative would need to either move that responsibility into Identity & Relationship (contradicting the already-approved Bounded Context Mapping) or retain it in Scheduling & Booking while nesting the aggregate inside a `Tutor` object physically owned by Identity & Relationship — a cross-context aggregate embedding `ADR-002` already forbids (Data Ownership Rules: every aggregate has exactly one owning context). Independent of this structural tension, reverting would also require reworking `AvailabilitySlot`'s status as an independent aggregate, `AvailabilitySlotRepository`'s existence, and every handler depending on `IAvailabilitySlotRepository` across three completed roadmap phases.

## Risks

- Ratifying without genuine scrutiny — confirming the status quo because it is already built, rather than because it is architecturally sound — would substitute sunk cost for reasoned approval. This risk is mitigated, but not eliminated, by the structural (not merely cost-based) argument for ratification identified in Trade-offs above; the business owner should still weigh it as a genuine decision, not a formality.
- Revising, if chosen, would need to either contradict `ARCHITECTURE.md` Section 4's already-approved Bounded Context Mapping or introduce a cross-context aggregate embedding `ADR-002` already forbids — either outcome would itself require amending an already-approved document, a materially larger governance action than this ADR alone contemplates.
- Leaving this permanently unratified (neither confirmed nor revised) means `ADR-013` and `ADR-014` are being designed against a boundary the documentation set still officially calls "an unresolved candidate" — a governance inconsistency already flagged once in this engagement.
- If revised after `ADR-013`/`ADR-014` are already resolved, both would need to be revisited — the highest-cost ordering error this batch of ADRs exists to avoid.

## Impact on Existing Architecture

If ratified as-is: none — a documentation action only, formalizing what already exists. If revised: substantial — would touch `Domain/Scheduling/AvailabilitySlot.cs`, `Domain/Scheduling/Session.cs`, their repository interfaces and implementations, and every handler and test built against them across the Application Pipeline, Domain Event Dispatch, and Read Side phases.

## Impact on Clean Architecture

None either way — this is entirely a Domain-layer aggregate-boundary question. Application, Infrastructure, and Web all depend on it only through already-abstracted repository interfaces.

## Impact on DDD

This is squarely a DDD tactical-pattern question — aggregate/consistency-boundary definition — and is the central question this ADR exists to resolve or confirm.

## Impact on Testing

If ratified as-is: no test changes needed — `Domain.Tests`'s existing `AvailabilitySlotTests` and `SessionTests` already validate this boundary's behavior. If revised: every test depending on `IAvailabilitySlotRepository`/`AvailabilitySlot` as an independent aggregate would need rework, a non-trivial regression surface given how much of the completed roadmap already depends on it.

## Recommendation

No structural outcome is recommended. Procedurally: because `ADR-013` and `ADR-014` are both blocked on knowing this boundary with confidence, this decision should be resolved first, before either of them. The business owner's decision between the two alternatives should rest on the architectural analysis in Trade-offs above — the Bounded Context Mapping consistency question and the aggregate-design heuristics — not solely on the cost asymmetry between them, even though that asymmetry is real and separately worth knowing.

## Assumptions

The four-bounded-context model (`ADR-002`) and Session/Availability Slot's ownership by Scheduling & Booking are both fixed and not reopened by this ADR — only the *internal* aggregate-boundary shape within that ownership is in scope.

## Non-Goals

Does not reopen `ADR-002`'s bounded-context assignments. Does not decide `DOMAIN_MODEL.md` Open Question 7 (does cancelling a Session reopen its Availability Slot) — a related but distinct lifecycle question. Does not decide `ARCHITECTURE.md` Open Question 12 (whether "Account" is a genuine independent aggregate root) — a separate, similarly-unratified boundary question this ADR does not attempt to also resolve.

## Questions Requiring Approval

1. Is the implemented boundary (Availability Slot and Session as two separate aggregate roots, coordinated only through `AvailabilitySlot.Book`) ratified as the answer to `DOMAIN_MODEL.md` Open Question 17 / `ADR-002` Open Question 3 / `ADR-004` Open Question 1?
2. If not ratified, is revision to "Availability Slot as a Tutor sub-component" the intended alternative, or is a different structure intended — and if so, what?

---

*Status: Accepted — 2026-07-20. Ratified as-is.*
