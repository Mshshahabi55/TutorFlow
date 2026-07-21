**Authoritative sources (in priority order):** `PROJECT_CONSTITUTION.md`, `PRODUCT_REQUIREMENTS.md`, `BUSINESS_MODEL.md`, `DOMAIN_MODEL.md`, `ARCHITECTURE.md` (all approved and immutable). `CleanArchitecture-main` was consulted only for architectural ideas (layering, dependency direction) — no code, namespace, or project layout from it is copied or implied by this decision. This ADR resolves the Architectural Decision Candidate raised in `ARCHITECTURE.md` Section 21, Item 1 ("Monolith vs. modular monolith vs. per-context services"), together with the layering style already proposed in `ARCHITECTURE.md` Section 6. Every claim below traces to one of the five documents above; where they do not settle a question, that is stated explicitly rather than resolved by invention.

---

# ADR-001: Architectural Style — Layered (Clean) Architecture within a Modular Monolith

## Status

**Accepted.** *(Status field corrected 2026-07-21 — this document's own Status was never updated after approval; every ADR from `ADR-013` onward, dated 2026-07-20, already lists this document among its "approved and immutable" sources, and the entire shipped implementation is built on it. Gap first identified by the Final Architecture Review Board's production-readiness audit, 2026-07-21. No content in this ADR is otherwise altered by this correction.)*

## Date

2026-07-19

## Context

`PROJECT_CONSTITUTION.md` establishes TutorFlow as a multi-sided scheduling and booking marketplace whose foundation must be reliable before anything else is built on it (Mission; Product Goal 6). It sets non-negotiable invariants: no session is ever double-booked, lost, or left inconsistent (Core Principle 3; `PRODUCT_REQUIREMENTS.md` CONST-1), all four roles read one authoritative schedule representation (`PRODUCT_REQUIREMENTS.md` CONST-3), every booking/availability-affecting action is attributable to an identity, role, and timestamp (`PRODUCT_REQUIREMENTS.md` CONST-2), and personal data — including minors' data, handled via Parent/Guardian — meets a GDPR-grade standard (`PRODUCT_REQUIREMENTS.md` CONST-4). The Constitution also commits to "small foundation, deliberate growth" (Core Principle 5) and "evolvability over premature generality" (Architecture Principle 7), and explicitly defers stack/hosting/infrastructure choices until justified (Architecture Principle 6).

`DOMAIN_MODEL.md` has already identified four Bounded Context Candidates — Scheduling & Booking, Identity & Relationship, Discovery, Marketplace Oversight — with Payments, Communication/Content Delivery, and Progress Tracking explicitly excluded from the domain altogether. `ARCHITECTURE.md` builds on this by proposing a Clean/Onion-style layered structure (Domain, Application, Infrastructure, Presentation) organized around those four contexts, and lists "Monolith vs. modular monolith vs. per-context services" as an open, unresolved Architectural Decision Candidate. No source document specifies a numeric scale target, a required independent-scaling need per context, or a team/organizational reason to split deployables (`PROJECT_CONSTITUTION.md`: KPI-Based Success Criteria explicitly defers specific targets).

## Problem Statement

What overall architectural style — both the internal layering approach and the deployment/modularity topology — should TutorFlow adopt for v1, such that it satisfies scheduling correctness, maintainability, domain integrity, reliability, and auditability as documented, supports a long-term ERP-quality architecture, and does so without committing to complexity that no approved document justifies?

## Decision

TutorFlow adopts a **Layered (Clean/Onion) Architecture organized internally as a single Modular Monolith**, partitioned along the four bounded contexts already identified in `DOMAIN_MODEL.md` (Scheduling & Booking, Identity & Relationship, Discovery, Marketplace Oversight), with strict dependency inversion: Domain and Application layers carry no dependency on any specific technology; Infrastructure implements the interfaces Application defines; Presentation depends only on Application (`ARCHITECTURE.md` Sections 6–11).

This is a single decision with two parts, both justified below:

1. **Layering style: Layered/Clean, not Traditional N-Tier.**
2. **Deployment topology: Modular Monolith, not Microservices.**

Traditional N-Tier and Microservices are both rejected for v1, for the reasons in the next section.

## Alternatives Considered

**A. Layered Architecture (Clean/Onion) — Selected as the layering style.**
Domain and Application are isolated from infrastructure and presentation concerns via dependency inversion. This directly satisfies Architecture Principle 1 (separation of concerns: the system owning scheduling logic must be isolated from presentation/infrastructure/future payment or messaging concerns) and Architecture Principle 6 (technology decisions remain deferrable, since Domain/Application never depend on a concrete technology). It supports Engineering Principle 2 (the system must be understandable and changeable by someone other than its original author) by giving scheduling-integrity logic a boundary that can be tested and reasoned about independent of persistence or web-framework concerns.

**B. Traditional N-Tier — Rejected.**
N-Tier typically layers by technical concern (Presentation / Business / Data Access) but conventionally allows the Business layer to depend directly on the Data Access layer and its underlying technology, rather than inverting that dependency. For TutorFlow this would let persistence and infrastructure concerns leak into the logic that enforces CONST-1 (no double-booking) and CONST-3 (single source of truth), directly weakening domain integrity and contradicting Architecture Principle 1 and Principle 6. It also makes the scheduling-integrity logic harder to isolate and verify independent of infrastructure, working against Engineering Principle 2 and Quality Principle 2 ("verification precedes release"). N-Tier is not rejected because it is old — it is rejected because its conventional coupling pattern directly conflicts with principles the Constitution already binds this project to.

**C. Modular Monolith — Selected as the deployment/modularity topology.**
A single deployable unit, internally partitioned along the four bounded contexts already identified in `DOMAIN_MODEL.md`, each with clear internal boundaries. This is the strongest fit for CONST-1 and CONST-3: guaranteeing "never double-booked" and "single source of truth" is materially simpler within one transactional boundary and one authoritative data store than across independently owned per-context data stores, where the same guarantees would require distributed coordination (e.g., sagas, distributed locks, eventual consistency) — mechanisms that trade certainty for availability, which is the wrong trade for a zero-tolerance invariant. A Modular Monolith also matches Core Principle 5 ("the narrowest defensible scope... expands only through explicit, documented decisions — not organic feature creep") and Architecture Principle 7 (evolvability over premature generality): the four contexts are already separated internally, so extracting one into an independent service later remains possible if a specific, approved need arises (Product Goal 7), without having paid that complexity cost now.

**D. Microservices — Rejected for v1.**
No approved document establishes a requirement that would justify microservices: there is no documented independent-scaling need per context, no documented team-topology reason to deploy contexts independently, and `PROJECT_CONSTITUTION.md`'s own KPI section explicitly defers specific numeric targets that might someday motivate this. Meanwhile, microservices would directly work against the Constitution's highest-priority invariants: CONST-1 (no double-booking) and CONST-3 (single source of truth) become materially harder to guarantee across independently owned service databases, requiring distributed-transaction or eventual-consistency patterns that introduce exactly the kind of risk Architecture Principle 5 warns against ("favor simple, verifiable mechanisms... over optimizations that increase the risk of inconsistency"). Core Principle 5 ("small foundation, deliberate growth") and Architecture Principle 6 ("defer irreversible technical decisions") further caution against this level of upfront distributed-systems complexity when nothing documented requires it. Per instruction, microservices is not chosen unless the approved documents clearly require it — they do not.

## Consequences

- Scheduling-integrity invariants (CONST-1, CONST-3) can be enforced within a single transactional/consistency boundary, which is materially simpler and more verifiable than the distributed alternative.
- A single deployable simplifies operations (one system to deploy, monitor, and audit) while the four-bounded-context internal partitioning keeps the codebase modular rather than an undifferentiated mass.
- Auditability (CONST-2) is centralized rather than needing to be reconciled across independently owned services.
- The codebase requires deliberate discipline to keep the four context boundaries clean; nothing about "monolith" automatically prevents them from eroding into an undifferentiated system over time.
- Extracting a context into an independent service later remains possible, but is a future Structural decision requiring its own explicit proposal and approval (`PROJECT_CONSTITUTION.md`: Decision-Making Process) — this ADR does not pre-approve that path, it only avoids foreclosing it.

## Trade-offs

- **Modular Monolith vs. Microservices:** TutorFlow gives up independent per-context scalability and independent deployability in exchange for simpler transactional consistency, lower operational complexity, and faster early iteration — a trade explicitly favored by Core Principle 5 and Architecture Principle 6 at this stage, and revisitable later per Architecture Principle 7 if a specific, approved need arises.
- **Layered/Clean vs. N-Tier:** TutorFlow gives up N-Tier's simpler, more direct coupling between business logic and data access in exchange for testability, technology deferral, and the long-term maintainability an ERP-quality system requires (Engineering Principle 2, Quality Principle 2). This costs more structural discipline up front.

## Risks

- **Boundary erosion:** without ongoing discipline, the four bounded contexts inside one codebase can degrade into a tightly coupled system, eliminating the future option to extract a context. Mitigated by the folder/module organization already specified in `ARCHITECTURE.md` Sections 19–20.
- **Future extraction cost:** if a genuine need to scale or deploy one context independently emerges later, extracting it from the monolith will still require real engineering effort; this ADR reduces but does not eliminate that cost.
- **Under-specification risk:** because no source document gives a numeric scale target, this decision is a qualitative judgment based on documented principles and invariants, not a quantitative one — see the note on document sufficiency below.

## Future Evolution

If a specific, approved, and documented need later justifies it (for example, an approved KPI target that demonstrably requires independent scaling of one context, or an approved organizational decision to staff a context independently), one or more of the four bounded contexts may be extracted from the Modular Monolith into an independently deployed service. The internal partitioning adopted here is deliberately structured to make that extraction feasible later (Architecture Principle 7; Product Goal 7: "sustain commercial growth... without compromising the reliability established in v1"). Any such extraction is a new Structural decision requiring its own explicit proposal, rationale, and approval under the Constitution's Decision-Making Process — it is not authorized by this ADR.

## Related Documents

- `PROJECT_CONSTITUTION.md` — Mission; Product Goals 6, 7; Core Principle 3, 5; Architecture Principles 1, 5, 6, 7; Engineering Principles 1, 2; Quality Principle 2, 3; Decision-Making Process.
- `PRODUCT_REQUIREMENTS.md` — CONST-1, CONST-2, CONST-3, CONST-4; Section 6 (Functional Requirements).
- `BUSINESS_MODEL.md` — Growth Strategy; Risks.
- `DOMAIN_MODEL.md` — Bounded Context Candidates; Aggregates; Invariants.
- `ARCHITECTURE.md` — Section 4 (Bounded Context Mapping); Section 5 (Context Relationships); Section 6 (Layered Architecture); Section 21, Item 1 (the Architectural Decision Candidate this ADR resolves).

## Note on Document Sufficiency

The approved documents are sufficient to decide the *style* recorded above: the qualitative principles and zero-tolerance invariants (CONST-1, CONST-3) clearly favor Layered/Clean over N-Tier and Modular Monolith over Microservices, and clearly do not meet the bar this task set for choosing Microservices ("unless the approved documents clearly require it"). The documents are **not** sufficient to decide several narrower, downstream questions — for example, the specific persistence technology, the specific inter-module communication mechanism within the monolith, or numeric scale targets — because no source document specifies them. Those remain the open Architectural Decision Candidates already listed in `ARCHITECTURE.md` Section 21 and are out of scope for this ADR.

---

*Status: Accepted. Status field corrected 2026-07-21 to match how every later ADR and the shipped implementation already treat this document — see the Status section above.*
