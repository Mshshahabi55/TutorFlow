**Authoritative sources:** `PROJECT_CONSTITUTION.md`, `PRODUCT_REQUIREMENTS.md`, and `BUSINESS_MODEL.md` (all approved and immutable). This document organizes facts already established in those three documents using Domain-Driven Design terminology. It introduces no new business rule, no technology choice, no persistence structure, and no API — only a structural description of the business domain. Where a modeling structure (an aggregate boundary, an event name) is a reasonable organizing device for already-approved facts rather than a fact itself, it is marked as such. Where information needed to complete the model is missing from the three source documents, it is recorded under Open Questions rather than invented.

**Status:** Draft — pending user approval. No further documents or code are to be created until this document is approved.

---

# Domain Overview

TutorFlow's v1 business domain is scheduling and booking within a multi-sided tutoring marketplace (`PROJECT_CONSTITUTION.md`: Mission). Four actors participate: Student, Tutor, Parent/Guardian, and Admin/Staff (`PROJECT_CONSTITUTION.md`: Project Scope). A Tutor declares availability; a Student or Parent/Guardian books it, producing a session with a tracked lifecycle status; an Admin/Staff oversees the marketplace (`PRODUCT_REQUIREMENTS.md` Section 6). The domain's boundary explicitly excludes the delivery of the tutoring session itself (`PRODUCT_REQUIREMENTS.md` SCH-1), payment processing, in-platform communication or content delivery, and progress tracking or grading (`PROJECT_CONSTITUTION.md`: Project Scope; `PRODUCT_REQUIREMENTS.md` Section 9). The domain's central commitment is reliability: no session is ever double-booked, lost, or left in an inconsistent state, and every party has one non-contradictory view of the same schedule data (`PROJECT_CONSTITUTION.md`: Core Principle 3–4, Product Goal 6).

# Ubiquitous Language

| Term | Meaning |
|---|---|
| Student | The person receiving tutoring. May be an adult (books independently) or a minor (requires a Parent/Guardian). (`PRODUCT_REQUIREMENTS.md` IDR-5, IDR-6) |
| Tutor | An independent tutor offering sessions; not discoverable until Admin-approved. (`PRODUCT_REQUIREMENTS.md` IDR-2) |
| Parent/Guardian | A person who may act on behalf of one or more Students. (`PROJECT_CONSTITUTION.md`: Project Scope; `PRODUCT_REQUIREMENTS.md` IDR-3) |
| Admin/Staff | Operates and supports the marketplace. (`PROJECT_CONSTITUTION.md`: Project Scope) |
| Availability | Time a Tutor declares as open for booking, along with the session duration(s) the Tutor offers. (`PRODUCT_REQUIREMENTS.md` SCH-3, SCH-5) |
| Booking | The act of a Student or Parent/Guardian reserving a Tutor's declared availability. (`PRODUCT_REQUIREMENTS.md` SCH-5; `PROJECT_CONSTITUTION.md`: Product Goal 2) |
| Session | The tutoring engagement that results from a booking; carries a delivery mode and a lifecycle status. (`PRODUCT_REQUIREMENTS.md` SCH-2, SCH-6) |
| Delivery Mode | Whether a session is Online or In-Person; delivery itself happens outside the domain. (`PRODUCT_REQUIREMENTS.md` SCH-1, SCH-2) |
| Session Status | One of Scheduled, Completed, Cancelled, No-Show. (`PRODUCT_REQUIREMENTS.md` SCH-6) |
| Relationship | The association between a Parent/Guardian and a Student, established by invitation and confirmation. (`PRODUCT_REQUIREMENTS.md` IDR-3, IDR-4) |
| Discoverability | Whether a Tutor is visible in search; gated on Admin approval. (`PRODUCT_REQUIREMENTS.md` IDR-2) |
| Subject, Language, Location/Time Zone | Attributes of a Tutor used as search/filter criteria. (`PRODUCT_REQUIREMENTS.md` DISC-1) |
| Hourly Rate | A Tutor's rate, visible to Students/Parents; no payment is processed in this domain. (`PRODUCT_REQUIREMENTS.md` DISC-2) |

# Core Concepts

- **Availability declared by the Tutor** is the raw material of the domain; a Tutor defines session duration(s), rate, and open time periods (`PRODUCT_REQUIREMENTS.md` SCH-3, SCH-5).
- **Booking as instant reservation.** A Student/Parent reserves a Tutor's availability immediately, with no manual Tutor acceptance step (`PRODUCT_REQUIREMENTS.md` SCH-5).
- **Session lifecycle.** A booking produces a Session that moves through a defined, closed set of statuses (`PRODUCT_REQUIREMENTS.md` SCH-6).
- **Trust-gated supply side.** A Tutor is not part of the discoverable marketplace until an Admin approves them, and can be removed from it by suspension (`PRODUCT_REQUIREMENTS.md` IDR-2, ADM-2).
- **Consent-based relationships.** A Parent/Guardian and a Student are connected only by mutual invitation and confirmation, never unilaterally (`PRODUCT_REQUIREMENTS.md` IDR-4).
- **Delegated authority.** A Parent/Guardian may act on behalf of a Student; a minor Student cannot act without one (`PRODUCT_REQUIREMENTS.md` IDR-5, IDR-6; `PROJECT_CONSTITUTION.md`: Product Goal 4).
- **Marketplace oversight.** Admin/Staff hold cross-cutting visibility and control that no other actor has (`PRODUCT_REQUIREMENTS.md` ADM-1 to ADM-5).
- **Excluded concerns.** Session delivery, payment, messaging/content, and progress tracking are deliberately outside this domain's boundary (`PROJECT_CONSTITUTION.md`: Project Scope).

# Domain Actors

- **Student** — registers (self, if adult); searches for Tutors; books, cancels, or reschedules a session; if a minor, requires a confirmed Parent/Guardian to book. (`PRODUCT_REQUIREMENTS.md` IDR-1, IDR-5, IDR-6, DISC-1, SCH-7)
- **Tutor** — registers; awaits Admin approval before being discoverable; defines session duration(s), hourly rate, and availability; can be booked instantly; can cancel or reschedule a session. (`PRODUCT_REQUIREMENTS.md` IDR-1, IDR-2, SCH-3, SCH-5, SCH-7)
- **Parent/Guardian** — registers; establishes a confirmed relationship with one or more Students; searches for and books Tutors on a linked Student's behalf; views/manages that Student's sessions; can cancel or reschedule. (`PRODUCT_REQUIREMENTS.md` IDR-1, IDR-3, IDR-4, SCH-7)
- **Admin/Staff** — approves or suspends Tutors; views all schedules across the platform; resolves booking conflicts; manages user accounts; can cancel or reschedule a session. (`PRODUCT_REQUIREMENTS.md` ADM-1 to ADM-5, SCH-7)

Whether Admin/Staff is one flat role or has internal tiers is not established (see Open Questions).

# Aggregates

Aggregates below are a modeling organization of the facts already established; boundaries marked "candidate" are not dictated by the source documents and are flagged accordingly.

- **Tutor** (root: Tutor) — the Tutor's identity, approval/discoverability state, suspension state, hourly rate, offered session duration(s), and search attributes (Subject, Language, Location/Time Zone). Consistency boundary: a Tutor's discoverability and offering are governed together. (`PRODUCT_REQUIREMENTS.md` IDR-2, SCH-3, DISC-1, DISC-2, ADM-1, ADM-2)
- **Availability Slot** (root: Availability Slot) — a specific bookable time period owned by a Tutor, with a delivery mode. *Candidate boundary:* whether this is a distinct aggregate from Session or a sub-component of the Tutor aggregate is not specified by the source documents (see Open Questions). It is modeled separately here because it must enforce "never bookable twice" independently of the Session it produces (`PRODUCT_REQUIREMENTS.md` CONST-1).
- **Session** (root: Session) — the booked engagement: references to the Tutor, the Student, and (if applicable) the acting Parent/Guardian; delivery mode; duration; status (Scheduled, Completed, Cancelled, No-Show). Consistency boundary: a Session's status transitions and the consumption of its originating Availability Slot must be governed as one unit to prevent double-booking. (`PRODUCT_REQUIREMENTS.md` SCH-2, SCH-6, CONST-1, CONST-2)
- **Relationship** (root: Relationship) — the link between one Parent/Guardian and one Student, with a state reflecting whether it has been invited or mutually confirmed. (`PRODUCT_REQUIREMENTS.md` IDR-3, IDR-4)
- **Account** (root: Account, specialized per role) — the registered identity of a Student, Tutor, Parent/Guardian, or Admin/Staff. *Candidate boundary:* the source documents establish self-registration for Student, Tutor, and Parent/Guardian (`PRODUCT_REQUIREMENTS.md` IDR-1) but do not establish how an Admin/Staff account is created (see Open Questions).

# Entities

Entities are distinguished by identity, not by attribute values:

- **Student**
- **Tutor**
- **Parent/Guardian**
- **Admin/Staff**
- **Session** — identity persists across its status transitions (Scheduled → Completed/Cancelled/No-Show). (`PRODUCT_REQUIREMENTS.md` SCH-6)
- **Availability Slot** — identity persists whether open or consumed by a booking.
- **Relationship** — identity persists across its Invited → Confirmed transition. (`PRODUCT_REQUIREMENTS.md` IDR-4)

# Value Objects

Attributes without independent identity, defined entirely by their value:

- **Delivery Mode** — Online | In-Person. (`PRODUCT_REQUIREMENTS.md` SCH-2)
- **Session Status** — Scheduled | Completed | Cancelled | No-Show. (`PRODUCT_REQUIREMENTS.md` SCH-6)
- **Relationship Status** — Invited | Confirmed. (`PRODUCT_REQUIREMENTS.md` IDR-4)
- **Session Duration** — a length of time, defined per Tutor. (`PRODUCT_REQUIREMENTS.md` SCH-3)
- **Hourly Rate** — a Tutor's rate; currency/format not specified (see Open Questions). (`PRODUCT_REQUIREMENTS.md` DISC-2)
- **Subject** — a search/filter attribute of a Tutor; whether it is a fixed taxonomy or free text is not specified (see Open Questions). (`PRODUCT_REQUIREMENTS.md` DISC-1)
- **Language** — a search/filter attribute of a Tutor. (`PRODUCT_REQUIREMENTS.md` DISC-1)
- **Location/Time Zone** — a search/filter attribute; its exact meaning for in-person tutoring is not specified (see Open Questions). (`PRODUCT_REQUIREMENTS.md` DISC-1)

# Relationships

- **Parent/Guardian ↔ Student** — many-to-many, existing only once a Relationship is mutually confirmed. (`PRODUCT_REQUIREMENTS.md` IDR-3, IDR-4)
- **Tutor → Availability Slot** — one Tutor declares many Availability Slots. (`PRODUCT_REQUIREMENTS.md` SCH-3, SCH-5)
- **Availability Slot → Session** — an open slot is consumed by exactly one booking, producing one Session; it must never be consumed by more than one. (`PRODUCT_REQUIREMENTS.md` CONST-1)
- **Session → Student** — every Session identifies exactly one Student as beneficiary. (`PRODUCT_REQUIREMENTS.md` SCH-1, IDR-5, IDR-6)
- **Session → Parent/Guardian** — a Session optionally identifies the Parent/Guardian who acted on the Student's behalf, for attribution. (`PRODUCT_REQUIREMENTS.md` IDR-5, IDR-6, CONST-2)
- **Session → Tutor** — every Session identifies exactly one Tutor. (`PRODUCT_REQUIREMENTS.md` SCH-5)
- **Admin/Staff → Tutor** — administrative authority to approve or suspend. (`PRODUCT_REQUIREMENTS.md` ADM-1, ADM-2)
- **Admin/Staff → Session** — oversight authority to view and to help resolve conflicts. (`PRODUCT_REQUIREMENTS.md` ADM-3, ADM-4)

# Business Rules

- A Tutor is not publicly discoverable until an Admin approves the account. (`PRODUCT_REQUIREMENTS.md` IDR-2, ADM-1)
- An Admin may suspend a Tutor, removing discoverability/bookability. (`PRODUCT_REQUIREMENTS.md` ADM-2)
- A Parent/Guardian may be associated with multiple Students; a Student may be associated with multiple Parents/Guardians. (`PRODUCT_REQUIREMENTS.md` IDR-3)
- A Parent-Student Relationship is created only by invitation from one party and confirmation by the other. (`PRODUCT_REQUIREMENTS.md` IDR-4)
- An adult Student may book independently; a minor Student requires an associated, confirmed Parent/Guardian. (`PRODUCT_REQUIREMENTS.md` IDR-5, IDR-6)
- Only single, discrete session bookings are supported; recurring/standing bookings do not exist in this domain. (`PRODUCT_REQUIREMENTS.md` SCH-4)
- A Tutor's declared Availability Slot is immediately bookable; no manual Tutor confirmation step follows a booking. (`PRODUCT_REQUIREMENTS.md` SCH-5)
- A Session has exactly one status at a time, drawn from Scheduled, Completed, Cancelled, or No-Show. (`PRODUCT_REQUIREMENTS.md` SCH-6)
- A Student, Parent/Guardian, Tutor, or Admin may cancel or reschedule a Session, subject to notice-period rules not yet defined. (`PRODUCT_REQUIREMENTS.md` SCH-7)
- A Tutor's slot must never be booked by more than one Student/Parent at the same time. (`PRODUCT_REQUIREMENTS.md` CONST-1)
- Every action that creates, reschedules, or cancels a booking, or changes availability, must be attributable to an authenticated identity, role, and timestamp. (`PRODUCT_REQUIREMENTS.md` CONST-2)
- Each role may access only the data and actions necessary for its function. (`PRODUCT_REQUIREMENTS.md` CONST-5)
- A Tutor's hourly rate is visible to Students/Parents; no payment is processed within this domain. (`PRODUCT_REQUIREMENTS.md` DISC-2)
- No rating or review concept exists within this domain for v1. (`PRODUCT_REQUIREMENTS.md` DISC-3)

# Domain Events

Named as past-tense occurrences of significance to the domain; the underlying facts are sourced as cited, though exact naming is a modeling convenience:

- **TutorRegistered** — a Tutor account is self-registered. (`PRODUCT_REQUIREMENTS.md` IDR-1)
- **TutorApproved** — an Admin approves a Tutor, making them discoverable. (`PRODUCT_REQUIREMENTS.md` ADM-1)
- **TutorSuspended** — an Admin suspends a Tutor. (`PRODUCT_REQUIREMENTS.md` ADM-2)
- **RelationshipInvited** — a Parent/Guardian or Student invites the other into a Relationship. (`PRODUCT_REQUIREMENTS.md` IDR-4)
- **RelationshipConfirmed** — the invited party confirms the Relationship. (`PRODUCT_REQUIREMENTS.md` IDR-4)
- **AvailabilityDeclared** — a Tutor declares an Availability Slot. (`PRODUCT_REQUIREMENTS.md` SCH-3, SCH-5)
- **SessionBooked** — a Student/Parent books an Availability Slot, producing a Session in Scheduled status. (`PRODUCT_REQUIREMENTS.md` SCH-5, SCH-6)
- **SessionRescheduled** — a Session's time is changed by a permitted actor. (`PRODUCT_REQUIREMENTS.md` SCH-7)
- **SessionCancelled** — a Session is cancelled by a permitted actor. (`PRODUCT_REQUIREMENTS.md` SCH-7)
- **SessionCompleted** — a Session's status is set to Completed. Triggering actor not established (see Open Questions). (`PRODUCT_REQUIREMENTS.md` SCH-6)
- **SessionMarkedNoShow** — a Session's status is set to No-Show. Triggering actor not established (see Open Questions). (`PRODUCT_REQUIREMENTS.md` SCH-6)
- **BookingConflictResolved** — an Admin resolves a booking conflict. Exact mechanics not established (see Open Questions). (`PRODUCT_REQUIREMENTS.md` ADM-4)

# Invariants

- An Availability Slot is never associated with more than one active Session at the same time (no double-booking). (`PRODUCT_REQUIREMENTS.md` CONST-1)
- Availability and booking data have a single authoritative representation read by all four roles; no role-specific copy may diverge. (`PRODUCT_REQUIREMENTS.md` CONST-3)
- Every mutation of booking or availability state carries an attributable identity, role, and timestamp. (`PRODUCT_REQUIREMENTS.md` CONST-2)
- A minor Student cannot hold a bookable state without at least one confirmed Parent/Guardian Relationship. (`PRODUCT_REQUIREMENTS.md` IDR-6)
- A Tutor cannot be discoverable or bookable unless currently approved and not suspended. (`PRODUCT_REQUIREMENTS.md` IDR-2, ADM-2)
- A Session holds exactly one status at any time, drawn only from the defined set. (`PRODUCT_REQUIREMENTS.md` SCH-6)
- A Relationship exists in a confirmed state only when both the inviting and invited party have acted. (`PRODUCT_REQUIREMENTS.md` IDR-4)
- Each role's access is limited to the data and actions its function requires. (`PRODUCT_REQUIREMENTS.md` CONST-5)

# Bounded Context Candidates

Presented as candidates, since the source documents defer architecture decisions and do not dictate context boundaries (`PROJECT_CONSTITUTION.md`: Architecture Principle 6):

- **Scheduling & Booking** — Availability, Session, and their lifecycle; the core domain per the Constitution's Mission. (`PROJECT_CONSTITUTION.md`: Mission; `PRODUCT_REQUIREMENTS.md` Section 6.2)
- **Identity & Relationship** — registration of all roles, Tutor approval workflow, and Parent-Student Relationship management. (`PRODUCT_REQUIREMENTS.md` Section 6.1, ADM-1, ADM-2)
- **Discovery** — search and filtering of Tutors by Subject, Availability, Language, Location/Time Zone. (`PRODUCT_REQUIREMENTS.md` Section 6.3)
- **Marketplace Oversight** — Admin/Staff visibility across schedules, conflict resolution, and account management. (`PRODUCT_REQUIREMENTS.md` Section 6.4)

Explicitly excluded from this domain's bounded contexts, per approved scope: Payments, In-Platform Communication/Content Delivery, Progress Tracking/Grading. (`PROJECT_CONSTITUTION.md`: Project Scope; `PRODUCT_REQUIREMENTS.md` Section 9)

# Open Questions

1. What is the exact age threshold distinguishing an "adult" Student from a "minor" Student? (`PRODUCT_REQUIREMENTS.md` Section 10.1, Item 1)
2. What is the exact mechanism for the Parent-Student invitation/confirmation flow, and what state(s) does a Relationship pass through beyond Invited/Confirmed? (`PRODUCT_REQUIREMENTS.md` Section 10.1, Item 2)
3. May a minor Student's account exist before any Relationship is confirmed, and in what state? (`PRODUCT_REQUIREMENTS.md` Section 10.1, Item 3)
4. What information or credentials does a Tutor submit for Admin approval, and what criteria does an Admin apply? (`PRODUCT_REQUIREMENTS.md` Section 10.1, Item 4; `BUSINESS_MODEL.md` Open Questions, Item 6)
5. What are the cancellation/rescheduling notice-period rules, and do they differ by role? (`PRODUCT_REQUIREMENTS.md` Section 10.2, Item 5)
6. Which role(s) are permitted to transition a Session to Completed or No-Show? This determines the triggering actor for the SessionCompleted and SessionMarkedNoShow events. (`PRODUCT_REQUIREMENTS.md` Section 10.2, Item 6)
7. When a Session is cancelled, does its originating Availability Slot automatically become open again? This determines the exact relationship between the Availability Slot and Session aggregates. (`PRODUCT_REQUIREMENTS.md` Section 10.2, Item 7)
8. Is there a minimum or maximum lead time for booking a Session? (`PRODUCT_REQUIREMENTS.md` Section 10.2, Item 8)
9. Is there a limit on the number of concurrent or future Sessions a Student or Tutor may hold? (`PRODUCT_REQUIREMENTS.md` Section 10.2, Item 9)
10. Is Subject a fixed, platform-defined taxonomy or free text set by each Tutor? (`PRODUCT_REQUIREMENTS.md` Section 10.3, Item 10)
11. What does "Location" mean for in-person tutoring — a specific address, a city/region, or a travel radius? (`PRODUCT_REQUIREMENTS.md` Section 10.3, Item 12)
12. Do Admin/Staff accounts share one flat permission level, or are there tiered admin roles? (`PRODUCT_REQUIREMENTS.md` Section 10.4, Item 14)
13. What specific mechanics does "resolve booking conflicts" include (e.g., can an Admin unilaterally move or cancel a Session)? (`PRODUCT_REQUIREMENTS.md` Section 10.4, Item 15)
14. What are the exact personal data fields collected for each role's Account entity? (`PRODUCT_REQUIREMENTS.md` Section 10.5, Item 17)
15. How is an Admin/Staff account created? The source documents establish self-registration for Student, Tutor, and Parent/Guardian (`PRODUCT_REQUIREMENTS.md` IDR-1) but do not address Admin/Staff account creation.
16. Is "Booking" a distinct concept from "Session," or is a booking simply the act that creates a Session in Scheduled status (as modeled in this document)? Not disambiguated in the source documents.
17. Is Availability Slot a separate aggregate from Session, or a sub-component of the Tutor aggregate? Not disambiguated in the source documents.
18. In what currency or format is a Tutor's Hourly Rate expressed? Not specified in the source documents.

---

*Status: Draft — pending user approval. No further documents or code are to be created until this document is approved.*
