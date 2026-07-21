**Authoritative sources (in priority order):** `PROJECT_CONSTITUTION.md`, `PRODUCT_REQUIREMENTS.md`, `BUSINESS_MODEL.md`, `DOMAIN_MODEL.md`, `ARCHITECTURE.md`, and all approved ADRs (`docs/adr/ADR-001` through `ADR-010`). This document is a logical domain data model only — it selects no database vendor, SQL dialect, data type, index, or physical constraint mechanism, and defines no table, migration, or ORM model. Where information needed to complete this document is missing from the approved sources, it is recorded under Open Questions rather than invented.

---

# 1. Purpose

This document renders the entities, value objects, aggregates, and invariants already established in `DOMAIN_MODEL.md` — as refined by `ADR-002` (bounded-context ownership) and `ADR-004` (persistence strategy) — into a single logical domain data model. It exists to give anyone designing a physical schema a complete, technology-neutral reference of what data exists, who owns it, how it relates, and what must always hold true, before any physical database design begins (`PROJECT_CONSTITUTION.md`: Architecture Principle 6, defer irreversible technical decisions).

# 2. Scope

**In scope:** logical entities, value objects, enumerations, aggregate boundaries, cardinalities, ownership, referential integrity rules, domain constraints, business invariants, derived data, and read model candidates for the four bounded contexts already established (`ADR-002`).

**Out of scope:** any specific database vendor, SQL dialect, physical table design, index, physical constraint mechanism, data type, migration, or ORM model. These remain deferred Architectural Decision Candidates (`ARCHITECTURE.md` Section 21) and are not addressed here.

# 3. Domain Entities

| Entity | Owning Context | Source |
|---|---|---|
| Student | Identity & Relationship | `DOMAIN_MODEL.md`: Entities; `ADR-002`: Context Ownership |
| Tutor | Identity & Relationship | `DOMAIN_MODEL.md`: Entities; `ADR-002`: Context Ownership |
| Parent/Guardian | Identity & Relationship | `DOMAIN_MODEL.md`: Entities; `ADR-002`: Context Ownership |
| Admin/Staff | Identity & Relationship | `DOMAIN_MODEL.md`: Entities; `ADR-002`: Context Ownership |
| Session | Scheduling & Booking | `DOMAIN_MODEL.md`: Entities; `ADR-002`: Context Ownership |
| Availability Slot | Scheduling & Booking | `DOMAIN_MODEL.md`: Entities; `ADR-002`: Context Ownership |
| Relationship | Identity & Relationship | `DOMAIN_MODEL.md`: Entities; `ADR-002`: Context Ownership |

`DOMAIN_MODEL.md` also describes an **Account** concept — "the registered identity of a Student, Tutor, Parent/Guardian, or Admin/Staff" — as a generalization specialized into each of the four role entities above, rather than a separate, independent entity (`DOMAIN_MODEL.md`: Aggregates). This document treats Student, Tutor, Parent/Guardian, and Admin/Staff as the four specializations of that shared Account concept.

# 4. Aggregate Ownership

| Aggregate (Root) | Contents | Owning Context | Source |
|---|---|---|---|
| Tutor | Tutor identity; approval/suspension state; Hourly Rate; Session Duration(s) offered; Subject, Language, Location/Time Zone attributes | Identity & Relationship | `DOMAIN_MODEL.md`: Aggregates; `ADR-002`: Context Ownership |
| Availability Slot | A bookable time period owned by a Tutor; Delivery Mode | Scheduling & Booking | `DOMAIN_MODEL.md`: Aggregates (candidate boundary — see Open Questions) |
| Session | Session identity; references to Tutor, Student, optional Parent/Guardian; Delivery Mode; Session Duration; Session Status | Scheduling & Booking | `DOMAIN_MODEL.md`: Aggregates |
| Relationship | The link between one Parent/Guardian and one Student; Relationship Status | Identity & Relationship | `DOMAIN_MODEL.md`: Aggregates |
| Account (specialized: Student, Tutor, Parent/Guardian, Admin/Staff) | The registered identity of each role | Identity & Relationship | `DOMAIN_MODEL.md`: Aggregates |

Discovery and Marketplace Oversight own no aggregate; neither persists data of its own (`ADR-002`: Context Ownership; `ADR-004`: Aggregate Persistence Rules).

# 5. Entity Relationships

- **Parent/Guardian ↔ Student:** many-to-many, mediated by a confirmed Relationship (`DOMAIN_MODEL.md`: Relationships; `PRODUCT_REQUIREMENTS.md` IDR-3, IDR-4).
- **Tutor → Availability Slot:** one Tutor to many Availability Slots.
- **Availability Slot → Session:** an open slot is consumed by at most one booking, producing one Session; it must never be consumed by more than one (CONST-1).
- **Session → Student:** every Session identifies exactly one Student.
- **Session → Parent/Guardian:** every Session optionally identifies the Parent/Guardian who acted on the Student's behalf, for attribution.
- **Session → Tutor:** every Session identifies exactly one Tutor.
- **Admin/Staff → Tutor:** an authority relationship (approve/suspend), not a structural data relationship — Marketplace Oversight owns no data of its own (`ADR-002`: Context Ownership).
- **Admin/Staff → Session:** an authority relationship (view/resolve conflicts), likewise not a structural data relationship.

# 6. Identity Strategy (Conceptual Only)

Every Entity is distinguished by a conceptual identity, not by its attribute values (`DOMAIN_MODEL.md`: Entities). This identity persists across an Entity's lifecycle — for example, a Session's identity is unchanged as its status moves from Scheduled to Completed, Cancelled, or No-Show. This document does not choose a specific identifier scheme (for example, sequential versus globally unique); that is a physical decision deferred beyond this document's scope (`PROJECT_CONSTITUTION.md`: Architecture Principle 6). A reference from one aggregate to another (for example, a Session's reference to its Tutor) is a reference to that aggregate's conceptual identity only — it is never an embedding of the referenced aggregate's own data, consistent with the "read, not copy" rule already fixed for cross-context access (`ADR-002`: Integration Rules).

# 7. Value Objects

| Value Object | Owning Context | Notes | Source |
|---|---|---|---|
| Delivery Mode | Scheduling & Booking | Online or In-Person | `DOMAIN_MODEL.md`: Value Objects |
| Session Status | Scheduling & Booking | Scheduled, Completed, Cancelled, No-Show | `DOMAIN_MODEL.md`: Value Objects |
| Relationship Status | Identity & Relationship | Invited or Confirmed | `DOMAIN_MODEL.md`: Value Objects |
| Session Duration | Scheduling & Booking | A length of time, defined per Tutor | `DOMAIN_MODEL.md`: Value Objects |
| Hourly Rate | Identity & Relationship | Currency/format not established (see Open Questions) | `DOMAIN_MODEL.md`: Value Objects |
| Subject | Identity & Relationship | Fixed taxonomy vs. free text not established (see Open Questions) | `DOMAIN_MODEL.md`: Value Objects |
| Language | Identity & Relationship | A Tutor's teaching language | `DOMAIN_MODEL.md`: Value Objects |
| Location/Time Zone | Identity & Relationship | Precise meaning not established (see Open Questions) | `DOMAIN_MODEL.md`: Value Objects |

# 8. Enumerations

Only the following are established as closed sets of values:

- **Delivery Mode:** { Online, In-Person } (`PRODUCT_REQUIREMENTS.md` SCH-2)
- **Session Status:** { Scheduled, Completed, Cancelled, No-Show } (`PRODUCT_REQUIREMENTS.md` SCH-6)
- **Relationship Status:** { Invited, Confirmed } (`PRODUCT_REQUIREMENTS.md` IDR-4)

Subject and Language are not established as closed enumerations by any approved document; whether Subject in particular is a fixed taxonomy remains an open question (see Open Questions).

# 9. Aggregate Boundaries

- **Session** governs its own Session Status transitions and, jointly with the Availability Slot it consumed, the no-double-booking guarantee (CONST-1) — these must be reasoned about, and transacted, together (`ADR-004`: Transaction Boundaries).
- **Availability Slot** as a boundary separate from Session, versus a sub-component of the Tutor aggregate, is an unresolved candidate boundary (`DOMAIN_MODEL.md` Open Question 17; `ADR-004` Open Question 1) — this document does not resolve it.
- **Tutor** governs its approval/suspension state together with its offering attributes (Hourly Rate, Session Duration(s), Subject, Language, Location/Time Zone), since discoverability depends on both (`PRODUCT_REQUIREMENTS.md` IDR-2, ADM-2, DISC-1, DISC-2).
- **Relationship** governs its own Invited → Confirmed transition as a self-contained unit, referencing its Parent/Guardian and Student by identity only, not by embedding their data (`PRODUCT_REQUIREMENTS.md` IDR-4).
- **Account**, specialized per role, is scoped to a single role instance; no Account aggregate spans more than one role.
- No aggregate boundary spans two bounded contexts: Session (Scheduling & Booking) and Tutor (Identity & Relationship) are never merged into one aggregate or one transaction (`ADR-002`: Context Ownership; `ADR-004`: Transaction Boundaries).

# 10. Cardinality Rules

| Relationship | Cardinality | Source |
|---|---|---|
| Parent/Guardian to Student (via Relationship) | Many-to-many | `PRODUCT_REQUIREMENTS.md` IDR-3 |
| Relationship to Parent/Guardian | Many-to-one (each Relationship instance has exactly one Parent/Guardian) | `PRODUCT_REQUIREMENTS.md` IDR-4 |
| Relationship to Student | Many-to-one (each Relationship instance has exactly one Student) | `PRODUCT_REQUIREMENTS.md` IDR-4 |
| Tutor to Availability Slot | One-to-many | `DOMAIN_MODEL.md`: Relationships |
| Availability Slot to Session | One-to-zero-or-one (at most one active Session per slot) | CONST-1 |
| Session to Student | Many-to-one | `DOMAIN_MODEL.md`: Relationships |
| Session to Parent/Guardian | Many-to-zero-or-one (optional) | `DOMAIN_MODEL.md`: Relationships |
| Session to Tutor | Many-to-one | `DOMAIN_MODEL.md`: Relationships |

# 11. Lifecycle Rules

- **Session:** created in Scheduled status when a booking succeeds (SessionBooked); may transition to Completed, Cancelled, or No-Show (`PRODUCT_REQUIREMENTS.md` SCH-6). Whether a terminal status (Completed, Cancelled, No-Show) can transition further is not established (see Open Questions).
- **Availability Slot:** declared by a Tutor (AvailabilityDeclared); becomes consumed when booked. Whether cancelling the resulting Session reopens the slot is unresolved (`DOMAIN_MODEL.md` Open Question 7).
- **Tutor:** registered (TutorRegistered) → pending approval → approved (TutorApproved), becoming discoverable → may be suspended (TutorSuspended), removing discoverability/bookability (`PRODUCT_REQUIREMENTS.md` IDR-2, ADM-1, ADM-2).
- **Relationship:** invited (RelationshipInvited) → confirmed (RelationshipConfirmed) (`PRODUCT_REQUIREMENTS.md` IDR-4). Whether a confirmed Relationship can later be revoked is not established (see Open Questions).
- **Account (Student, Parent/Guardian):** created via self-registration (`PRODUCT_REQUIREMENTS.md` IDR-1). No further lifecycle state beyond creation is established, though Admin/Staff is granted a general "manage user accounts" capability (ADM-5) whose precise effect on account lifecycle is not specified (see Open Questions).

# 12. Ownership Rules

- Every Entity and Value Object has exactly one owning bounded context; no other context may persist an independent copy of it (CONST-3; `ADR-002`: Data Ownership Rules; `ADR-004`: Aggregate Persistence Rules).
- A mutation to an owned Entity occurs only through its owning context's own Application/Domain logic — never through a direct write from another context (`ADR-002`: Integration Rules).
- Discovery and Marketplace Oversight, owning no data, act on or read data owned by Identity & Relationship and Scheduling & Booking rather than maintaining their own authoritative copy (`ADR-002`: Context Ownership).

# 13. Referential Integrity Rules

- A Session must always reference exactly one valid Tutor and exactly one valid Student (`DOMAIN_MODEL.md`: Relationships).
- A Session's optional Parent/Guardian reference, when present, must correspond to a Parent/Guardian holding a confirmed Relationship with the Session's Student at the time of booking (`PRODUCT_REQUIREMENTS.md` IDR-4, IDR-6).
- A Relationship must always reference exactly one Parent/Guardian and exactly one Student (`PRODUCT_REQUIREMENTS.md` IDR-3, IDR-4).
- An Availability Slot must always reference exactly one Tutor.
- A Session must reference an Availability Slot that was open at the moment of booking, never one already consumed (CONST-1).
- A cross-context reference is a reference to another aggregate's conceptual identity only (see Identity Strategy); whether an entity in any context can ever be deleted outright, as opposed to reaching a terminal or suspended state, is not established by any approved document (see Open Questions).

# 14. Domain Constraints

- Session Status must be exactly one value from { Scheduled, Completed, Cancelled, No-Show } (`PRODUCT_REQUIREMENTS.md` SCH-6).
- Delivery Mode must be exactly one value from { Online, In-Person } (`PRODUCT_REQUIREMENTS.md` SCH-2).
- Relationship Status must be exactly one value from { Invited, Confirmed } (`PRODUCT_REQUIREMENTS.md` IDR-4).
- A Tutor must carry both an approval state and a suspension state, jointly determining discoverability and bookability (`PRODUCT_REQUIREMENTS.md` IDR-2, ADM-2).
- A Student must carry an adult/minor distinction (`PRODUCT_REQUIREMENTS.md` IDR-5, IDR-6); the exact criterion (age threshold) is not established (see Open Questions).
- Every mutating action across all owned data must carry an attributable identity, role, and timestamp (CONST-2) — a cross-cutting constraint on mutations generally, not an attribute of any single entity.

# 15. Business Invariants

Restated from `DOMAIN_MODEL.md`: Invariants, unchanged:

1. An Availability Slot is never associated with more than one active Session at the same time (CONST-1).
2. Availability and booking data have a single authoritative representation read by all four roles; no role-specific copy may diverge (CONST-3).
3. Every mutation of booking or availability state carries an attributable identity, role, and timestamp (CONST-2).
4. A minor Student cannot hold a bookable state without at least one confirmed Parent/Guardian Relationship (IDR-6).
5. A Tutor cannot be discoverable or bookable unless currently approved and not suspended (IDR-2, ADM-2).
6. A Session holds exactly one status at any time, drawn only from the defined set (SCH-6).
7. A Relationship exists in a confirmed state only when both the inviting and invited party have acted (IDR-4).
8. Each role's access is limited to the data and actions its function requires (CONST-5).

# 16. Derived Data

- **Audit Record:** a derived, append-only record of a business-changing action — capturing the acting identity, role, action, and timestamp — required by CONST-2 and detailed in `ADR-009`. It is owned by whichever bounded context's action it records (`ADR-009`: Cross-Context Audit Rules), and is not itself a primary domain entity with independent business behavior.
- Whether every one of the twelve Domain Events (`DOMAIN_MODEL.md`: Domain Events; `ADR-006`) requires its own independent Audit Record, or some are covered indirectly, is unresolved (`ADR-006` Open Question 4; `ADR-009` Open Question 4).
- Any future search-oriented derived structure for Discovery must be explicitly non-authoritative and reconciled to the owning context's data if it is ever introduced (`ADR-004`: Read Model Principles) — no such structure is established to exist today.

# 17. Read Model Candidates

- **Tutor search view (Discovery):** a read composition over Tutor data (Identity & Relationship) and Availability data (Scheduling & Booking), supporting search/filter by Subject, Availability, Language, Location/Time Zone (`PRODUCT_REQUIREMENTS.md` DISC-1). Whether this requires a materialized, non-authoritative projection or can be computed live at query time is unresolved (`ADR-002` Open Question 1; `ADR-004`: Read Model Principles).
- **All-schedules view (Marketplace Oversight):** a read composition over Scheduling & Booking's own Session and Availability Slot data (`PRODUCT_REQUIREMENTS.md` ADM-3); no separate persisted copy is required (`ADR-004`: Read Model Principles).
- **Audit trail view (Marketplace Oversight):** a read composition over the Derived Audit Record data described above, supporting the Constitution's Success Criteria and the "Operational resolution time" KPI (`ADR-009`).

# 18. Future Expansion Points

- If Payments, In-Platform Communication/Content Delivery, or Progress Tracking are ever brought into scope through a Constitution amendment, each would introduce new entities within a new bounded context, following the same ownership and boundary rules established here and in `ADR-002` (`PROJECT_CONSTITUTION.md`: Project Scope).
- If Admin/Staff permission tiering is approved (`DOMAIN_MODEL.md` Open Question 12), the Admin/Staff entity may need to model sub-role or permission-tier data — not decided here.
- If a Marketplace Oversight-owned record (for example, for conflict resolution) is later established (`DOMAIN_MODEL.md` Open Question 13), it would introduce a new owned entity within that context — not decided here.
- If Discovery is later found to need a materialized read model, it must be introduced as an explicitly non-authoritative derived structure, justified against CONST-3, per `ADR-004`.

# 19. Open Questions

1. Is Availability Slot a separate aggregate/entity from Session, or a sub-component of the Tutor aggregate? (`DOMAIN_MODEL.md` Open Question 17; `ADR-004` Open Question 1) — affects Aggregate Boundaries (Section 9) and Entity Relationships (Section 5).
2. Does cancelling a Session automatically reopen its Availability Slot? (`DOMAIN_MODEL.md` Open Question 7) — affects Lifecycle Rules (Section 11).
3. What is the exact age threshold distinguishing an adult Student from a minor Student? (`DOMAIN_MODEL.md` Open Question 1) — affects Domain Constraints (Section 14).
4. Is Subject a fixed taxonomy or free text? (`DOMAIN_MODEL.md` Open Question 10) — affects whether Subject is an Enumeration (Section 8) or an unconstrained Value Object (Section 7).
5. What does Location/Time Zone precisely represent — an address, a city/region, or a travel radius? (`DOMAIN_MODEL.md` Open Question 12) — affects that Value Object's internal structure.
6. In what currency or format is Hourly Rate expressed? (`DOMAIN_MODEL.md` Open Question 18)
7. How is an Admin/Staff account created — self-registered like the other three roles, or created differently? (`DOMAIN_MODEL.md` Open Question 15) — affects the uniformity of the Account generalization (Sections 3, 11).
8. Does Marketplace Oversight require any owned entity of its own (for example, a conflict-resolution record)? (`DOMAIN_MODEL.md` Open Question 13; `ADR-002` Open Question 2) — affects Aggregate Ownership (Section 4) and Future Expansion Points (Section 18).
9. Can a Session's terminal status (Completed, Cancelled, No-Show) ever transition again, or are these permanently terminal? Not addressed by any approved document.
10. Can a confirmed Relationship ever be revoked or unconfirmed? Not addressed by any approved document.
11. Can any Account (Student, Tutor, Parent/Guardian, Admin/Staff) ever be deleted outright, as opposed to merely suspended or deactivated? Not addressed by any approved document.
12. Does every one of the twelve Domain Events require its own derived Audit Record? (`ADR-006` Open Question 4; `ADR-009` Open Question 4)

# 20. Traceability

| Element of this Document | Source |
|---|---|
| Entities, Value Objects, Aggregates, Relationships | `DOMAIN_MODEL.md` |
| Bounded-context ownership of each entity/aggregate | `ADR-002`: Context Ownership |
| Aggregate persistence and read-model principles | `ADR-004`: Aggregate Persistence Rules, Read Model Principles |
| Domain Events as lifecycle triggers | `DOMAIN_MODEL.md`: Domain Events; `ADR-006` |
| Business invariants | `DOMAIN_MODEL.md`: Invariants; `PRODUCT_REQUIREMENTS.md` CONST-1 to CONST-5 |
| Audit record as derived data | `PRODUCT_REQUIREMENTS.md` CONST-2; `ADR-009` |
| Deferred physical/technology decisions | `PROJECT_CONSTITUTION.md`: Architecture Principle 6; `ARCHITECTURE.md` Section 21 |

---

*Status: Draft — pending user approval. No further documents or code are to be created until this document is approved.*
