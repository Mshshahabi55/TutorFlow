**Authoritative sources (in priority order):** `PROJECT_CONSTITUTION.md`, `PRODUCT_REQUIREMENTS.md`, `BUSINESS_MODEL.md`, `DOMAIN_MODEL.md` (all approved and immutable). `CleanArchitecture-main`, present in the workspace, was studied only for structural understanding — layering, dependency direction, and solution organization conventions. No code, namespace, or project structure from it is copied; TutorFlow's architecture below is organized around its own bounded contexts, aggregates, and rules as established in the four authoritative documents. Every decision here traces to one of them; where none of them answers a question needed to complete this document, it is recorded under Open Questions (Section 23) rather than invented.

**Status:** Draft — pending user approval. No further documents or code are to be created until this document is approved.

---

# 1. Architectural Vision

The architecture exists to serve one job first: a reliable scheduling and booking marketplace, with everything else kept separable from it (`PROJECT_CONSTITUTION.md`: Mission). The system that owns scheduling logic must be identifiable and isolated from presentation, notification, or future payment/messaging concerns (`PROJECT_CONSTITUTION.md`: Architecture Principle 1). The architecture favors simple, verifiable mechanisms over cleverness wherever booking/scheduling correctness is at stake (`PROJECT_CONSTITUTION.md`: Architecture Principle 5), defers technology choices until justified (`PROJECT_CONSTITUTION.md`: Architecture Principle 6), and is built to evolve — more tutors, more students, more markets — without being spent on speculative capability it does not yet need (`PROJECT_CONSTITUTION.md`: Architecture Principle 7, Product Goal 7). Every structural choice below must be traceable to a business rule, principle, or documented requirement, never to convenience alone (`PROJECT_CONSTITUTION.md`: Architecture Principle 8).

# 2. Architectural Drivers

| Driver | Architectural Implication | Source |
|---|---|---|
| Zero tolerance for double-booking | A strong consistency boundary must govern Availability Slot ↔ Session together. | `PRODUCT_REQUIREMENTS.md` CONST-1 |
| Single source of truth for schedule state across all four roles | Availability/booking data cannot be duplicated per role; one authoritative representation is read by all. | `PRODUCT_REQUIREMENTS.md` CONST-3 |
| Every schedule-affecting action must be attributable | An audit trail is a first-class, cross-cutting architectural concern, not an afterthought. | `PRODUCT_REQUIREMENTS.md` CONST-2 |
| Least-privilege access per role | Authorization must be a deliberate, explicit architectural layer, not scattered checks. | `PRODUCT_REQUIREMENTS.md` CONST-5 |
| GDPR-grade protection of personal data, including minors' data via Parent/Guardian | Security and data-handling architecture must be designed in from the start. | `PRODUCT_REQUIREMENTS.md` CONST-4 |
| v1 is web-only | The Presentation layer's scope is a single web-based surface; no native mobile layer is built now. | `PRODUCT_REQUIREMENTS.md` PLAT-1 |
| Deliberate, non-premature growth | The architecture must avoid speculative generality while remaining able to extend. | `PROJECT_CONSTITUTION.md`: Core Principle 5, Architecture Principle 7 |
| Designed for future international expansion without re-architecture | Data and compliance structures must not hard-code a single-market assumption. | `PRODUCT_REQUIREMENTS.md` DATA-3 |
| Four bounded context candidates already identified | Internal modularity should reflect these boundaries rather than an arbitrary structure. | `DOMAIN_MODEL.md`: Bounded Context Candidates |
| Maintainability by someone other than the original author | Layered separation of concerns is required, not optional. | `PROJECT_CONSTITUTION.md`: Engineering Principle 2 |
| Fail safely and visibly | Errors must be detectable, never silently corrupt schedule data or trust. | `PROJECT_CONSTITUTION.md`: Engineering Principle 4 |

# 3. Architecture Principles

These are binding, carried forward directly from `PROJECT_CONSTITUTION.md` and applied to this architecture without modification:

1. **Separation of concerns** — scheduling logic is isolated from presentation, infrastructure, and future payment/messaging concerns. (Architecture Principle 1)
2. **Single source of truth** — no role-specific copy of schedule state may exist or drift. (Architecture Principle 2)
3. **Auditable actions** — every booking/availability-affecting action is attributable to a role and timestamp. (Architecture Principle 3)
4. **Role-aware, not role-hardcoded** — permissions and relationships (e.g., parent-to-student) are modeled as data and rules, not scattered special cases. (Architecture Principle 4)
5. **Reliability over cleverness** — simple, verifiable mechanisms are chosen over optimizations that increase inconsistency risk. (Architecture Principle 5)
6. **Defer irreversible technical decisions** — stack, hosting, and infrastructure choices are made only once justified; none is assumed by this document. (Architecture Principle 6)
7. **Evolvability over premature generality** — the architecture extends as new goals are approved, not for speculative future capability. (Architecture Principle 7)
8. **Traceability** — every architectural decision of consequence traces to a domain rule, principle, or documented requirement. (Architecture Principle 8)

One corollary is added, itself derived directly from the above and from `DOMAIN_MODEL.md` rather than invented: of the four Bounded Context Candidates `DOMAIN_MODEL.md` identifies, only the two that own a model — Scheduling & Booking and Identity & Relationship (Section 4) — are the basis for this architecture's Domain-layer modularity. Discovery and Marketplace Oversight, which own no Aggregate, Entity, Value Object, or domain rule of their own, are instead Application-layer modules built on top of those two (Principle 1, 7; `DOMAIN_MODEL.md`: Bounded Context Candidates, Aggregates).

# 4. Bounded Context Mapping

`DOMAIN_MODEL.md` names four Bounded Context *Candidates* — Scheduling & Booking, Identity & Relationship, Discovery, Marketplace Oversight — explicitly marked as candidates, not settled boundaries (`DOMAIN_MODEL.md`: Bounded Context Candidates). Checking that candidate list against `DOMAIN_MODEL.md`'s own Aggregates section shows only two own an Aggregate, Entity, Value Object, or domain rule of their own: Scheduling & Booking (Availability Slot, Session) and Identity & Relationship (Tutor, Relationship, Account and its specializations). Discovery and Marketplace Oversight own neither — `DOMAIN_MODEL.md`'s Aggregates section assigns no aggregate to either, its Relationships section describes Discovery only as composing reads over the other two contexts' data, and Marketplace Oversight only as holding "administrative authority" over Tutor and Session, not owning data of its own. This architecture therefore recognizes exactly two true, Domain-layer Bounded Contexts:

| Bounded Context | Responsibility | Source |
|---|---|---|
| **Scheduling & Booking** (core domain) | Availability Slot and Session lifecycle: declaring availability, booking, cancelling, rescheduling, status transitions. | `DOMAIN_MODEL.md` Bounded Context Candidates, Aggregates; `PRODUCT_REQUIREMENTS.md` Section 6.2 |
| **Identity & Relationship** | Registration of all roles, Tutor approval/suspension workflow, Tutor offering attributes (rate, duration, subjects), and Parent-Student Relationship management. | `DOMAIN_MODEL.md` Bounded Context Candidates, Aggregates; `PRODUCT_REQUIREMENTS.md` Section 6.1, ADM-1, ADM-2 |

Discovery and Marketplace Oversight remain named modules of this architecture — `DOMAIN_MODEL.md` still assigns each a distinct responsibility — but are reclassified rather than dropped:

| Module | Nature | Responsibility | Source |
|---|---|---|---|
| **Discovery** | Application Read Module — composes reads over data owned by Scheduling & Booking and Identity & Relationship; owns no Aggregate, Entity, Value Object, or domain rule of its own. | Search/filtering of Tutors by Subject, Availability, Language, Location/Time Zone. | `DOMAIN_MODEL.md` Bounded Context Candidates, Aggregates, Relationships; `PRODUCT_REQUIREMENTS.md` Section 6.3 |
| **Marketplace Oversight** | Application Module — orchestrates actions over Scheduling & Booking's and Identity & Relationship's own Aggregates through their own use cases; owns no Aggregate of its own. | Admin/Staff visibility across all schedules, conflict resolution, and account management. | `DOMAIN_MODEL.md` Bounded Context Candidates, Aggregates, Relationships; `PRODUCT_REQUIREMENTS.md` Section 6.4 |

This reclassification changes no responsibility, requirement, or business rule already approved — it corrects which architectural layer (Domain vs. Application) each already-approved responsibility belongs to, consistent with the Layered Architecture and Dependency Rules already fixed (Sections 6–7). Sections 19–20 already reflected this distinction — no Domain-layer module was ever given to Discovery or Marketplace Oversight there — before the rest of this document caught up to it.

Explicitly excluded, per approved scope, and therefore **not** modeled anywhere in this architecture: Payments, In-Platform Communication/Content Delivery, Progress Tracking/Grading (`PROJECT_CONSTITUTION.md`: Project Scope; `PRODUCT_REQUIREMENTS.md` Section 9).

# 5. Context Relationships

The dependency direction between contexts follows directly from the data dependencies already documented in the functional requirements; it is an architectural inference from those dependencies, not a new business rule:

- **Identity & Relationship → Scheduling & Booking (upstream/downstream).** A booking must know whether a Student is a minor and, if so, whether a confirmed Parent/Guardian Relationship exists (`PRODUCT_REQUIREMENTS.md` IDR-5, IDR-6); Scheduling & Booking is therefore downstream of, and depends on, facts owned by Identity & Relationship.
- **Identity & Relationship → Discovery** and **Scheduling & Booking → Discovery.** Discovery, an Application Read Module rather than a Bounded Context (Section 4), composes reads over Tutor data (approval state, rate, subjects, language, location — owned by Identity & Relationship) and Availability data (owned by Scheduling & Booking); it has no aggregates of its own and is downstream of both. (`PRODUCT_REQUIREMENTS.md` DISC-1, DISC-2)
- **Marketplace Oversight, an Application Module rather than a Bounded Context (Section 4), is a cross-cutting consumer** of both Identity & Relationship (approve/suspend a Tutor, manage accounts) and Scheduling & Booking (view all schedules, resolve conflicts); it does not own its own core domain data beyond what its actions produce. (`PRODUCT_REQUIREMENTS.md` ADM-1 to ADM-5)

The specific DDD context-mapping pattern for each relationship (e.g., Customer-Supplier vs. Conformist vs. Anticorruption Layer) is an implementation-level decision not yet made — see Architectural Decision Candidates (Section 21).

# 6. Layered Architecture

A Clean/Onion-style layered architecture — Domain, Application, Infrastructure, Presentation, with dependencies pointing inward — is recommended. This is justified directly by the Architecture Principles, not adopted by convenience: it isolates scheduling logic per Principle 1, keeps the domain free of technology assumptions so stack decisions remain deferrable per Principle 6, and supports maintainability by someone other than the original author per Engineering Principle 2. TutorFlow's layers are organized around its own bounded contexts (Section 4), not around any reference template's example features.

- **Domain** — innermost; the Entities, Value Objects, Aggregates, Domain Events, and Invariants defined in `DOMAIN_MODEL.md`. No dependency on any other layer.
- **Application** — orchestrates Domain objects to fulfill the use cases implied by the Functional Requirements; defines the interfaces (ports) that Infrastructure implements. Depends only on Domain.
- **Infrastructure** — implements the interfaces Application defines: persistence, identity/authentication integration, audit trail storage. Depends on Application and Domain.
- **Presentation** — the web-based surface (per PLAT-1) through which Student, Tutor, Parent/Guardian, and Admin/Staff invoke Application use cases. Depends on Application only.

# 7. Dependency Rules

- Dependencies point inward only: Presentation → Application → Domain.
- Domain has zero dependencies on Application, Infrastructure, or Presentation.
- Application depends only on Domain; it defines interfaces that Infrastructure implements (dependency inversion), so Application never depends on Infrastructure directly.
- Infrastructure depends on Application (to implement its interfaces) and Domain; nothing depends on Infrastructure except the composition root that wires implementations to interfaces.
- Presentation depends on Application only; it never bypasses Application to reach Domain or Infrastructure directly.

This enforces Architecture Principle 1 (separation of concerns) and Principle 6 (deferred/swappable technology choices), since Infrastructure — the only layer aware of any concrete technology — can be replaced without touching Domain or Application.

# 8. Domain Layer Responsibilities

Contains exactly what `DOMAIN_MODEL.md` establishes, with no addition:

- **Entities:** Student, Tutor, Parent/Guardian, Admin/Staff, Session, Availability Slot, Relationship.
- **Value Objects:** Delivery Mode, Session Status, Relationship Status, Session Duration, Hourly Rate, Subject, Language, Location/Time Zone.
- **Aggregates:** Availability Slot, Session, Relationship, and Tutor — each named as its own Aggregate Root in `DOMAIN_MODEL.md`: Aggregates. `DOMAIN_MODEL.md` also names "Account (root: Account, specialized per role)" as a fifth aggregate, describing Student, Tutor, Parent/Guardian, and Admin/Staff as its specializations — but it separately, and apparently redundantly, names Tutor as its own root too. Whether Account is truly an independent Aggregate Root, or only a descriptive generalization label with no independent existence, is not clearly settled; this document does not resolve that ambiguity (see Open Question 12, Section 23). Precisely which of the above constitute independent transactional boundaries — in particular the relationship between Availability Slot, Session, and Tutor — is also not yet settled (see Open Question 3, Section 23); aggregate ownership determines the transaction boundary, invariant enforcement, and concurrency boundary for the no-double-booking rule, and none of the three is decided until that question is.
- **Invariants enforced here:** an Availability Slot is never associated with more than one active Session (CONST-1); a Session holds exactly one status from the defined set (SCH-6); a minor Student cannot be in a bookable state without a confirmed Relationship (IDR-6); a Tutor cannot be discoverable/bookable unless approved and not suspended (IDR-2, ADM-2); a Relationship is confirmed only when both parties have acted (IDR-4). The defined set of Session statuses (SCH-6) is established, but the allowed *transitions* between them (e.g., whether any is reversible, or all are reachable from each other) are not; the transition graph must be formally defined before implementation, not assumed from the status list alone (see Open Question 13, Section 23).
- **Domain Events:** TutorRegistered, TutorApproved, TutorSuspended, RelationshipInvited, RelationshipConfirmed, AvailabilityDeclared, SessionBooked, SessionRescheduled, SessionCancelled, SessionCompleted, SessionMarkedNoShow, BookingConflictResolved (`DOMAIN_MODEL.md`: Domain Events). Every Domain Event is owned by the Aggregate whose state change it represents: TutorRegistered/TutorApproved/TutorSuspended by Tutor; RelationshipInvited/RelationshipConfirmed by Relationship; AvailabilityDeclared by Availability Slot; SessionBooked/SessionRescheduled/SessionCancelled/SessionCompleted/SessionMarkedNoShow by Session. BookingConflictResolved is not assigned an owning Aggregate here, since Marketplace Oversight — the module whose action triggers it — owns no Aggregate of its own (Section 4); its owner is not established (Section 23, Open Question 7).
- **Domain Services:** where a business rule spans more than one Aggregate — for example, the no-double-booking check, which depends on both Availability Slot and Session (Open Question 3), or confirming a minor Student has a confirmed Relationship before a booking may proceed (IDR-6), which depends on both Session and Relationship — a Domain Service may be required to coordinate that rule without one Aggregate reaching into another's internal state. No Domain Service is defined here; this only records that the pattern may be necessary once the aggregate-boundary questions above are resolved.

This layer has no knowledge of persistence, web frameworks, or any other technology.

# 9. Application Layer Responsibilities

Orchestrates the Domain layer to fulfill use cases traceable to the Functional Requirements and User Journeys, organized by the two Bounded Contexts and the two Application-only modules identified in Section 4:

- **Scheduling & Booking:** book a session against an Availability Slot (SCH-5, enforcing CONST-1), cancel a session, reschedule a session (SCH-7), transition a session's status (SCH-6).
- **Identity & Relationship:** register an account (IDR-1), define Tutor availability/duration/rate (SCH-3, DISC-2), invite and confirm a Parent-Student Relationship (IDR-4).
- **Discovery** (Application Read Module, Section 4): search Tutors by Subject, Availability, Language, Location/Time Zone (DISC-1).
- **Marketplace Oversight** (Application Module, Section 4): approve/suspend a Tutor (ADM-1, ADM-2), view all schedules (ADM-3), resolve a booking conflict (ADM-4), manage a user account (ADM-5).

This layer also defines the interfaces Infrastructure must implement. For persistence, this takes the shape of a repository abstraction per Aggregate (Tutor, Availability Slot, Session, Relationship), so that Application coordinates Domain behavior through those abstractions and never through a persistence implementation directly — "repository" names a pattern here, not a product or technology (the specific persistence technology remains Section 21, Item 2). Identity and audit interfaces are defined the same way. This layer is also responsible for invoking the cross-cutting Authorization (CONST-5) and Audit (CONST-2) concerns around every use case that mutates state.

Search (Discovery), Tutor/schedule dashboards, and Marketplace Oversight's administrative views (view all schedules, ADM-3) are read-oriented concerns layered on top of the write-oriented use cases above. Because they only read data already owned and validated by Scheduling & Booking and Identity & Relationship, how they are composed and optimized may evolve independently of those write-side use cases without changing what either Bounded Context owns or enforces. No specific read-model or query-separation pattern (e.g., CQRS or a dedicated projection mechanism) is adopted here — none is established by any approved document — only that a read concern is architecturally distinct from a write concern and may evolve on its own terms.

# 10. Infrastructure Layer Responsibilities

Implements the interfaces Application defines, with no business logic of its own:

- **Persistence** of every Domain aggregate, honoring the single-source-of-truth requirement (CONST-3). A strongly consistent, transactional (ACID) datastore is recommended as a category, since it directly supports the no-double-booking invariant (CONST-1) via atomic writes and uniqueness constraints — the specific product is not chosen here (see Section 21).
- **Identity/authentication** integration supporting the self-registration and role model established by IDR-1 through IDR-6.
- **Audit trail** storage implementing the Audit Strategy (Section 16), recording actor identity, role, action, and timestamp for every qualifying action (CONST-2).

Per Architecture Principle 6, the specific technologies for each of the above remain deferred until justified — see Architectural Decision Candidates (Section 21).

# 11. Presentation Layer Responsibilities

A single web-based surface (per PLAT-1) exposing the Application layer's use cases to the four Domain Actors, matching the User Journeys already documented (`PRODUCT_REQUIREMENTS.md` Section 5):

- Presents registration, search, booking, cancellation/reschedule, and (for the Student/Parent/Tutor) schedule views.
- Presents the Admin/Staff surfaces for Tutor approval/suspension, all-schedules visibility, conflict resolution, and account management.
- All content is presented in English for v1 (DATA-2).
- Depends only on the Application layer; contains no business rules and no direct persistence access.

Whether the Presentation layer is a server-rendered application, a single-page application backed by a web API, or another web-based shape is not decided here (see Architectural Decision Candidates, Section 21); in any of those shapes, the boundary described above holds.

# 12. Cross-cutting Concerns

| Concern | Requirement Driving It | Source |
|---|---|---|
| Authentication & Authorization | Least-privilege, role-based access. | CONST-5 |
| Audit Logging | Every schedule-affecting action attributable to identity, role, timestamp. | CONST-2 |
| Concurrency Control | No slot ever double-booked. | CONST-1 |
| Validation | Correctness before convenience. | Engineering Principle 1 |
| Error Handling | Fail safely and visibly; no silent corruption. | Engineering Principle 4 |
| Data Protection & Privacy | GDPR-grade handling of personal data, including minors'. | CONST-4 |
| Localization | English-only for v1, not hard-coded against future expansion. | DATA-2, DATA-3 |
| Time Handling | Scheduling correctness depends on an unambiguous representation of time (booking, no-double-booking, Session/Availability Slot timing). | Not established — UTC as an internal representation, time-zone handling for display and for in-person Location, and a Clock abstraction (rather than reading system time directly) are all implied in principle but not specified by any approved document (see Open Question 14, Section 23). |

# 13. Security Architecture

Derived directly from `PROJECT_CONSTITUTION.md`'s Security Principles:

- **Security by design** — security is considered at each layer boundary above, not retrofitted. (Security Principle 1)
- **Least privilege** — each of the four roles (and, within Marketplace Oversight, Admin/Staff) is granted access only to what its function requires; a role-based authorization layer sits between Presentation and Application (CONST-5; Security Principle 2). See Section 17.
- **Protection of personal data** — data belonging to Students, including minors represented via Parent/Guardian, is handled to a standard consistent with the European market; this constrains the Infrastructure layer's data handling and the Application layer's data-minimization behavior (DATA-1). (Security Principle 3; CONST-4)
- **Accountability for access** — every action affecting another party's data or schedule is attributable to an authenticated identity and role, implemented via the Audit cross-cutting concern (Section 16). (Security Principle 4)
- **Assume breach, design for containment** — the bounded-context modularity already established (Section 4) also serves as a containment boundary: a compromise within one context's Infrastructure should not automatically expose another context's data. (Security Principle 5)
- **Security decisions are owned, not implicit** — any Structural security decision made under this architecture (e.g., choice of authentication mechanism) follows the Constitution's Decision-Making Process and requires explicit approval. (Security Principle 6; `PROJECT_CONSTITUTION.md`: Decision-Making Process)

# 14. Scalability Strategy

No numeric scale target is defined in any source document; the KPI categories in `PROJECT_CONSTITUTION.md` explicitly defer specific targets ("specific targets and instrumentation are defined later, once the domain and metrics ownership are established"). Consistent with Architecture Principle 7 (evolvability over premature generality) and Core Principle 5 (small foundation, deliberate growth), this document does not prescribe a scaling mechanism (e.g., sharding, caching tiers, specific horizontal-scaling numbers) that is not yet justified by a demonstrated need. The binding constraint that *is* established is qualitative: the architecture must not preclude "more tutors, more students, more markets" (Product Goal 7) or require re-architecture for future international expansion (DATA-3). This is satisfied by the layered, bounded-context structure above (Sections 6–10), which keeps Infrastructure — the layer most affected by scale — replaceable without changing Domain or Application. Specific scalability targets remain an Open Question (Section 23).

# 15. Reliability Strategy

Directly grounded in the Constitution's reliability commitments:

- The booking operation (consuming an Availability Slot to produce a Session) must be atomic and enforce CONST-1 as a hard invariant — this is the architecture's single highest-priority reliability mechanism, consistent with Quality Principle 3 ("scheduling-integrity defects are critical by default").
- Failures must be detectable and visible rather than silently corrupting schedule data or trust (Engineering Principle 4) — this is implemented jointly by the Error Handling Strategy (Section 18) and the Audit Strategy (Section 16).
- Delivery is incremental and reversible (Engineering Principle 5): changes to Scheduling & Booking logic are the highest-scrutiny changes in the system and are verified before release (Quality Principle 2).
- The single-source-of-truth requirement (CONST-3) rules out any architecture that maintains a separate schedule copy per role (e.g., cached, role-specific projections that could drift) without a mechanism guaranteeing they reconcile to the one authoritative record.

# 16. Audit Strategy

Grounded in CONST-2 and Security Principle 4: every action that creates, reschedules, or cancels a Session, or that changes Availability, must be recorded with the acting identity, its role, the action taken, and a timestamp. The same standard extends to the trust-critical actions in Identity & Relationship and Marketplace Oversight — Tutor approval and suspension (ADM-1, ADM-2) — since these are equally consequential to the trust the Constitution requires (Product Goal 6, 8).

This single, already-established requirement serves three purposes that the source documents do not distinguish into separate audit categories: a **business record** of what happened to a booking or account (CONST-2), a **security accountability** record of who acted and under what role (Security Principle 4), and an **operational** troubleshooting aid — the audit record is the mechanism by which an Admin/Staff member can detect and resolve a scheduling problem without reading code or querying a database directly (`PROJECT_CONSTITUTION.md`: Success Criteria), and supports the "Operational resolution time" KPI (`BUSINESS_MODEL.md`: Success Metrics). Whether these three purposes warrant genuinely separate audit mechanisms — for example, different retention periods, different access restrictions, or different storage — is not established by any approved document; this document does not invent that distinction, and records it instead as an Architectural Decision Candidate (Section 21, Item 11).

The specific storage mechanism for the audit trail (e.g., append-only log vs. event store) is not chosen here — see Section 21.

# 17. Authorization Strategy

A three-part model is implied directly by the documented rules, not invented:

1. **Role-based authorization (RBAC)** as the baseline — Student, Tutor, Parent/Guardian, Admin/Staff each access only the data and actions their function requires (CONST-5).
2. **Relationship-based authorization** layered on top of role for Parent/Guardian access — a Parent/Guardian may act only on behalf of a Student with whom a confirmed Relationship exists (IDR-3, IDR-4); this cannot be expressed by role alone and requires checking the Relationship aggregate's state as part of authorization.
3. **Resource ownership validation**, where a role's own function requires acting only on its own resource instance rather than any instance of that type — for example, a Tutor defines only their own offering and availability (SCH-3), not another Tutor's, and a Student manages only their own sessions, not another Student's. This follows directly from CONST-5's "least privilege" (access limited to what a role's function requires) applied at the level of a specific resource instance, not merely the resource type.

None of these three introduces attribute-based access control (ABAC) or any authorization framework beyond what is already implied: each is a direct expression of an already-approved requirement (CONST-5, IDR-3, IDR-4, SCH-3), not a new mechanism.

Whether Admin/Staff is a single flat role or has internal permission tiers is not established (`DOMAIN_MODEL.md` Open Question 12) and is carried forward as an Open Question here (Section 23), since it determines whether the RBAC model needs sub-roles within Marketplace Oversight.

# 18. Error Handling Strategy

Grounded in Engineering Principle 4 (fail safely and visibly) and CONST-1: an attempt to book an Availability Slot that is no longer available must fail explicitly and be communicated to the acting Student/Parent/Guardian — it must never silently double-book or silently drop the request. System-level failures (e.g., an Infrastructure fault during a booking transaction) must be surfaced to Admin/Staff in a way that supports the Constitution's requirement that an admin can detect and resolve scheduling problems without reading code or querying a database directly (`PROJECT_CONSTITUTION.md`: Success Criteria). Domain-rule violations (e.g., a minor attempting to book without a confirmed Relationship, per IDR-6) are treated as expected, explicit outcomes distinguishable from unexpected system failures.

# 19. Folder / Solution Organization

The solution is organized layer-first at the top level (Domain, Application, Infrastructure, Presentation, mirrored by tests). Domain is further organized by the two true Bounded Contexts identified in Section 4 — Scheduling & Booking and Identity & Relationship — since only they own an Aggregate. Application is organized by all four named modules from Section 4: the same two Bounded Contexts, plus Discovery and Marketplace Oversight, neither of which has a Domain module of its own. Discovery has no Domain module because `DOMAIN_MODEL.md` assigns it no aggregate — it is represented only at the Application layer as a read composition over Identity & Relationship and Scheduling & Booking data (Section 5). Marketplace Oversight likewise has no Domain module, for the same reason (Section 4) — it is represented only at the Application layer, orchestrating actions through Scheduling & Booking's and Identity & Relationship's own use cases.

# 20. Recommended Project Structure

```
src/
  Domain/
    Scheduling/        (Session, Availability Slot — entities, value objects, domain events, invariants)
    Identity/          (Student, Tutor, Parent/Guardian, Admin/Staff, Relationship — entities, value objects, domain events, invariants)
    Common/            (shared domain building blocks only; no context-specific logic)

  Application/
    Scheduling/        (use cases: book, cancel, reschedule, transition session status)
    Identity/          (use cases: register account, define Tutor offering/availability, invite/confirm relationship)
    Discovery/         (use case: search Tutors — composes Identity + Scheduling data; no domain module)
    Oversight/         (use cases: approve/suspend Tutor, view all schedules, resolve conflict, manage account)
    Common/            (cross-cutting interfaces: authorization, audit, validation — consumed by all contexts)

  Infrastructure/
    Persistence/       (implements Application's persistence interfaces for all aggregates)
    Identity/          (implements authentication/identity concerns)
    Audit/             (implements the Audit Strategy, Section 16)

  Presentation/
    Web/               (the single web-based surface serving all four Domain Actors, per PLAT-1)

tests/
  Domain.Tests/
  Application.Tests/
  Infrastructure.Tests/
  Presentation.Tests/
```

No file extensions, languages, or frameworks are implied by this tree; it names layers, the two Bounded Contexts, and the two Application-only modules (Discovery, Oversight) only.

# 21. Architectural Decision Candidates (not ADRs)

These are Structural decisions this architecture surfaces as needing deliberate proposal and explicit approval per the Constitution's Decision-Making Process (`PROJECT_CONSTITUTION.md`: Decision-Making Process, Items 3, 6) — none is decided by this document:

1. **Monolith vs. modular monolith vs. per-context services** for deploying the two Bounded Contexts identified in Section 4 (and, atop them, the Discovery and Marketplace Oversight Application modules). A modular monolith is the leading candidate, consistent with Core Principle 5 ("narrowest defensible scope") and Architecture Principle 7, but requires explicit approval.
2. **Specific persistence technology.** A relational/ACID category is recommended in Section 10; the specific product is not chosen.
3. **Mechanism enforcing no-double-booking** (e.g., a uniqueness constraint plus transaction, optimistic concurrency, pessimistic locking, or another mechanism) at the Infrastructure level; this cannot be finally selected until the aggregate-boundary question (Section 23, Open Question 3) is resolved, and no mechanism is recommended here.
4. **Identity/authentication provider or mechanism** (e.g., a managed identity service vs. a self-hosted mechanism).
5. **Audit trail storage mechanism** (e.g., append-only log table vs. a dedicated event store).
6. **Inter-context communication mechanism** within the chosen deployment shape (e.g., in-process mediator vs. another integration mechanism).
7. **Presentation shape** — server-rendered web application vs. a client application backed by a web API (Section 11).
8. **Hosting/deployment platform.**
9. **Admin/Staff permission model** (flat vs. tiered), pending resolution of `DOMAIN_MODEL.md` Open Question 12.
10. **Programming language and framework selection**, to be justified against the Architecture Principles (Section 3) rather than defaulted from any reference material.
11. **Audit mechanism granularity** — whether the Business Audit, Security Audit, and Operational Audit purposes described in Section 16 are served by one unified audit mechanism or require separate, independently-scoped mechanisms (e.g., different retention or access rules). No approved document distinguishes these purposes into separate mechanisms, and none is recommended here.
12. **Consistency model — strong vs. eventual** — for how Availability Slot and Session state (and any read model built on them, Section 9) remain consistent with each other and with what each of the four roles sees. This directly affects booking integrity (CONST-1) and is not chosen here; it must be selected deliberately once the aggregate-boundary question (Open Question 3) and the concurrency mechanism (Item 3 above) are resolved.

# 22. Risks

- **Concurrency defect in the booking path** could violate CONST-1, the system's highest-priority invariant; the architecture must make this hard to get wrong, not just documented as a rule.
- **Premature technology or scale commitment** would contradict Architecture Principle 6/7 if a specific stack or scaling mechanism is adopted before it is justified by a demonstrated need.
- **Incomplete audit coverage** would undermine the accountability the Constitution requires (Security Principle 4) if any state-mutating action is missed by the Audit Strategy.
- **Unresolved Domain Model open questions propagating into architecture ambiguity** — in particular, the undefined Admin/Staff account-creation mechanism (`DOMAIN_MODEL.md` Open Question 15) leaves a gap in the Identity & Relationship context's design, and the undecided Availability Slot/Session aggregate boundary (`DOMAIN_MODEL.md` Open Question 17) leaves the exact transactional boundary for booking unresolved.
- **Data-protection risk for minors' data** if the Security Architecture (Section 13) is not rigorously implemented to the GDPR-grade standard required by CONST-4 — already flagged as a business-level risk (`BUSINESS_MODEL.md`: Risks) and restated here at the architecture level.
- **Over-modularization risk** — splitting the two Bounded Contexts (or the Discovery/Marketplace Oversight Application modules built on them) into separate deployables before it is justified could itself violate the "narrowest defensible scope" principle (Core Principle 5).

# 23. Open Questions

Carried forward from `DOMAIN_MODEL.md` and `BUSINESS_MODEL.md` where they block a specific architectural decision, plus new architecture-level gaps this document surfaced:

1. How is an Admin/Staff account created? Blocks the Identity & Relationship context's account-creation design. (`DOMAIN_MODEL.md` Open Question 15)
2. Is Admin/Staff a single flat role or are there permission tiers? Blocks whether the Authorization Strategy (Section 17) needs sub-roles. (`DOMAIN_MODEL.md` Open Question 12)
3. Is Availability Slot a separate aggregate from Session, or a sub-component of the Tutor aggregate? This single question determines three distinct things this document does not decide: the **transaction boundary** (which aggregate(s) must be updated atomically to keep CONST-1 true), **invariant enforcement** (which aggregate's own rules are responsible for "never bookable twice"), and the **concurrency boundary** (which aggregate a concurrency-control mechanism, Section 21 Item 3, must be applied to). Blocks the precise transactional boundary for booking (Section 15). (`DOMAIN_MODEL.md` Open Question 17)
4. Does cancelling a Session automatically reopen its Availability Slot? Blocks the Scheduling & Booking use-case design (Section 9). (`DOMAIN_MODEL.md` Open Question 7)
5. Which role(s) may transition a Session to Completed or No-Show? Blocks the authorization rules around that transition (Section 17). (`DOMAIN_MODEL.md` Open Question 6)
6. What are the cancellation/rescheduling notice-period rules? Blocks whether the Application layer needs a time-based guard for those use cases. (`DOMAIN_MODEL.md` Open Question 5)
7. What specific mechanics does "resolve booking conflicts" include, and which Aggregate, if any, owns the resulting BookingConflictResolved Domain Event, given Marketplace Oversight owns no Aggregate of its own (Section 4)? Blocks the Marketplace Oversight use-case design (Section 9) and the Domain Event ownership stated in Section 8. (`DOMAIN_MODEL.md` Open Question 13)
8. What are the specific numeric targets and instrumentation for each KPI category? Blocks setting any concrete Scalability Strategy target (Section 14). (`BUSINESS_MODEL.md` Open Questions, Item 10)
9. What is the initial launch/target market? Blocks any concrete data-residency decision beyond the generic GDPR-grade baseline (Section 13). (`BUSINESS_MODEL.md` Open Questions, Item 1)
10. What credentials/information must a Tutor submit for approval? May affect the Infrastructure layer's data-storage scope (e.g., whether document/credential storage is needed). (`DOMAIN_MODEL.md` Open Question 4)
11. In what currency or format is a Tutor's Hourly Rate expressed, and does the Domain need a general-purpose Money Value Object (amount plus currency) rather than a bare numeric Hourly Rate? No approved document establishes a currency or a multi-currency requirement, so no Money Value Object is introduced here. Affects whether the Hourly Rate Value Object requires localization-aware formatting. (`DOMAIN_MODEL.md` Open Question 18)
12. Is "Account" truly an independent Aggregate Root distinct from Student, Tutor, Parent/Guardian, and Admin/Staff, or only a descriptive generalization label with no independent existence of its own? `DOMAIN_MODEL.md`'s Aggregates section names both Tutor and Account (specialized per role, including Tutor) as roots without resolving whether this is one aggregate or several. Blocks a precise statement of the Identity & Relationship context's aggregate boundaries (Section 8).
13. What are the allowed state transitions between Session statuses (Scheduled, Completed, Cancelled, No-Show)? `DOMAIN_MODEL.md` establishes the closed set of values (SCH-6) but not a transition graph between them (e.g., whether any status is terminal). This must be formally defined before implementation.
14. What time-zone handling policy applies to a Session's scheduled time and a Tutor's declared Availability (e.g., UTC as the internal representation, with conversion only for display), and is a Clock abstraction — rather than direct system-time access — required? Not addressed by any approved document; `DOMAIN_MODEL.md` Open Question 11 (what "Location" means for in-person tutoring) is adjacent but does not cover this.

---

*Status: Draft — pending user approval. No further documents or code are to be created until this document is approved.*
