**Authoritative sources (in priority order):** `PROJECT_CONSTITUTION.md`, `PRODUCT_REQUIREMENTS.md`, `BUSINESS_MODEL.md`, `DOMAIN_MODEL.md`, `ARCHITECTURE.md`, `docs/adr/ADR-001-architecture-style.md`, `docs/adr/ADR-002-domain-boundaries.md` (all approved and immutable). This ADR is about architecture and domain responsibility only — it selects no vendor, framework, protocol, or identity provider, and none is implied by anything below. Where information needed to complete this ADR is missing from the seven sources above, it is recorded under Open Questions rather than invented.

---

# ADR-003: Authentication and Authorization Strategy

## Status

**Accepted — 2026-07-21.**

**Acceptance note:** Accepted as written, with all 8 Open Questions (Section: Open Questions) explicitly carried forward, not resolved by this acceptance. None of the eight blocks the Permission Model table already specified for the four roles' documented actions (`IDR-1` to `IDR-6`, `SCH-3`, `SCH-5`, `SCH-7`, `DISC-1`, `DISC-2`, `ADM-1` to `ADM-5`) — that table is what Priority 2 (Authorization) implements. Any capability that *does* depend on one of the 8 open items (e.g., Admin/Staff permission tiering, denied-authorization-attempt auditing) remains blocked on that specific item until it is separately decided, exactly as `ADR-011`/`ADR-017` were separated: accepting the architecture does not invent the answers to what the architecture itself leaves open.

## Context

`PROJECT_CONSTITUTION.md` requires least-privilege access per role (Security Principle 2; `PRODUCT_REQUIREMENTS.md` CONST-5), attributability of every schedule-affecting action to an authenticated identity and role (Security Principle 4; CONST-2), and protection of personal data — including minors', handled via Parent/Guardian — to a GDPR-grade standard (Security Principle 3; CONST-4). `DOMAIN_MODEL.md` establishes four roles (Student, Tutor, Parent/Guardian, Admin/Staff) with role-specific, sometimes relationship-scoped, permissions (IDR-1 to IDR-6, ADM-1 to ADM-5). `ARCHITECTURE.md` already sketched a two-tier authorization model — role-based access control plus relationship-scoped authorization for Parent/Guardian-Student pairs (Section 17) — and identified the specific authentication mechanism as a deferred Architectural Decision Candidate (Section 21, Item 4). `ADR-002` fixed that all identity data (Student, Tutor, Parent/Guardian, Admin/Staff Accounts, and the Relationship between a Parent/Guardian and a Student) is owned exclusively by the Identity & Relationship bounded context, and that business logic must be enforced only within its owning context. This ADR formalizes how authentication and authorization work across that structure, without choosing any technology.

## Problem Statement

Given the four roles, the relationship-scoped authority of Parent/Guardian over specific Students, the Tutor approval gate, and the bounded-context ownership already fixed in `ADR-002`, how does TutorFlow establish a verified identity (authentication) and decide what that identity may do (authorization) — consistent with least privilege, auditability, and the rule that business authorization logic belongs to the Domain layer of the context that owns the resource in question — without selecting any implementation technology?

## Decision

Authentication establishes a single verified identity, associated with exactly one Account (Student, Tutor, Parent/Guardian, or Admin/Staff), owned exclusively by the Identity & Relationship context (`ADR-002`: Context Ownership). Authorization is a separate, downstream decision: given an established identity, its role, and the relevant domain state (e.g., Relationship confirmation, Tutor approval status), a permission decision is made in the Domain layer of whichever bounded context owns the resource being acted upon. Authorization follows a two-tier model — role-based access control as the baseline, augmented by relationship-scoped authorization for Parent/Guardian-Student pairs — consistent with `ARCHITECTURE.md` Section 17. No authentication mechanism, protocol, or vendor is chosen here; that remains the open Architectural Decision Candidate already listed in `ARCHITECTURE.md` Section 21, Item 4.

## Authentication Principles

- Authentication establishes identity only; it does not itself decide permissions (Architecture Principle 1, separation of concerns).
- Every authenticated identity corresponds to exactly one Account, and every Account has exactly one role: Student, Tutor, Parent/Guardian, or Admin/Staff (`DOMAIN_MODEL.md`: Domain Actors; `ADR-002`: Context Ownership).
- Student, Tutor, and Parent/Guardian accounts are created through self-registration (`PRODUCT_REQUIREMENTS.md` IDR-1). How an Admin/Staff account is created is not established by any approved document (see Open Questions).
- The Identity & Relationship context is the exclusive owner of all Account data and is therefore the sole authority for establishing identity (`ADR-002`: Context Ownership, Data Ownership Rules).
- Because every action that creates, reschedules, or cancels a booking, or changes availability, must be attributable to an identity and role (`PRODUCT_REQUIREMENTS.md` CONST-2), authentication must always resolve to a durable, attributable identity — never an anonymous or shared credential.

## Authorization Principles

- Least privilege: each role is granted access only to the data and actions necessary for its function (`PRODUCT_REQUIREMENTS.md` CONST-5; `PROJECT_CONSTITUTION.md`: Security Principle 2).
- Role-aware, not role-hardcoded: permissions and relationships (e.g., Parent/Guardian to Student) are modeled as data and rules evaluated at authorization time, not as scattered special cases in feature logic (`PROJECT_CONSTITUTION.md`: Architecture Principle 4).
- Business authorization decisions are made in the Domain layer of the bounded context that owns the resource being acted upon, never in Infrastructure — consistent with `ADR-002`'s rule that business logic is enforced only within its owning context.
- Authorization must combine a coarse-grained check (which role is this) with a fine-grained check where the rule requires it (which specific Student a Parent/Guardian is authorized for) — a role check alone cannot express "confirmed Relationship required" (`PRODUCT_REQUIREMENTS.md` IDR-3, IDR-4).
- A Tutor's bookability is itself gated by an authorization-adjacent business rule: not discoverable/bookable unless approved and not suspended (`PRODUCT_REQUIREMENTS.md` IDR-2, ADM-2; `DOMAIN_MODEL.md`: Invariants).

## Identity Ownership

- The Account — Student, Tutor, Parent/Guardian, or Admin/Staff — is owned exclusively by the Identity & Relationship context; no other context may hold its own copy of identity data (`ADR-002`: Context Ownership; `PRODUCT_REQUIREMENTS.md` CONST-3).
- Scheduling & Booking, Discovery, and Marketplace Oversight reference identity only by reading from Identity & Relationship; none of them owns or duplicates it (`ADR-002`: Integration Rules, Dependency Rules).
- The exact personal data fields that make up an Account's identity are not fully specified; only the rule that collection is minimized to what each role's function requires is established (`PRODUCT_REQUIREMENTS.md` DATA-1; exact fields remain PRD Open Question 10.5, Item 17).

## Role Model

- Four roles exist: Student, Tutor, Parent/Guardian, Admin/Staff (`PROJECT_CONSTITUTION.md`: Project Scope; `DOMAIN_MODEL.md`: Domain Actors).
- A Student has a state distinction, not a separate role: adult (books independently) or minor (requires a confirmed Parent/Guardian) (`PRODUCT_REQUIREMENTS.md` IDR-5, IDR-6).
- A Tutor has a state distinction: pending, approved, or suspended, governing discoverability and bookability (`PRODUCT_REQUIREMENTS.md` IDR-2, ADM-1, ADM-2).
- A Parent/Guardian's authority is always scoped to specific, confirmed Student relationships — it is never a global grant over all Students (`PRODUCT_REQUIREMENTS.md` IDR-3, IDR-4).
- Whether Admin/Staff is a single flat role or has internal permission tiers is not established (`DOMAIN_MODEL.md` Open Question 12) — carried forward here as an Open Question, since it determines whether the Role Model needs sub-roles within Marketplace Oversight.

## Permission Model

Permissions below are drawn directly from the approved Functional Requirements; none is invented beyond them.

| Role | Permitted Actions | Source |
|---|---|---|
| Student (adult) | Register; search Tutors; book, cancel, or reschedule a session. | IDR-1, IDR-5, DISC-1, SCH-5, SCH-7 |
| Student (minor) | Same as above, but only with an associated, confirmed Parent/Guardian Relationship. | IDR-6 |
| Tutor | Register; define availability, session duration(s), and hourly rate; cancel or reschedule a session. Not discoverable/bookable until Admin-approved. | IDR-1, IDR-2, SCH-3, DISC-2, SCH-7 |
| Parent/Guardian | Register; establish a confirmed Relationship; search, book, cancel, or reschedule on behalf of a linked Student only. | IDR-1, IDR-3, IDR-4, SCH-7 |
| Admin/Staff | Approve/suspend a Tutor; view all schedules; resolve booking conflicts; manage user accounts; cancel or reschedule a session. | ADM-1 to ADM-5, SCH-7 |

Which role(s) may transition a Session to Completed or No-Show is not established (`DOMAIN_MODEL.md` Open Question 6) and is carried forward as an Open Question, since it is a genuine gap in this permission model.

## Resource Ownership

Authorization for a resource is decided using facts owned by that resource's bounded context (`ADR-002`: Context Ownership):

- **Session, Availability Slot** → owned by Scheduling & Booking.
- **Student, Tutor, Parent/Guardian, Admin/Staff Accounts, Relationship** → owned by Identity & Relationship.
- **No resource** is owned by Discovery or Marketplace Oversight; both act on or read resources owned elsewhere (`ADR-002`: Context Ownership).

## Cross-Context Authorization Rules

- When Scheduling & Booking authorizes a booking, it must read — not duplicate — the fact of whether the Student is an adult or has a confirmed Parent/Guardian Relationship from Identity & Relationship (`PRODUCT_REQUIREMENTS.md` IDR-5, IDR-6; `ADR-002`: Integration Rules, Dependency Rules).
- When Marketplace Oversight acts on a Tutor (approve/suspend) or a Session (resolve a conflict), both the authorization decision and the resulting state change occur through the owning context's own logic; Marketplace Oversight does not implement a duplicate copy of that business rule (`ADR-002`: Rules That Prevent Business Logic Leakage).
- Discovery, being read-only and owning no resource, performs no authorization-gated mutation; only read access to discoverable Tutor data applies (`PRODUCT_REQUIREMENTS.md` DISC-1; `ADR-002`: Integration Rules).
- No context may grant itself authority over a resource it does not own; authority over a resource always resides with its owning context (`ADR-002`: Data Ownership Rules, Integration Rules).

## Audit Requirements

- Every action that creates, reschedules, or cancels a Session, or that changes Availability, must be recorded with the acting identity, its role, the action, and a timestamp (`PRODUCT_REQUIREMENTS.md` CONST-2).
- The same standard extends to Tutor approval and suspension, since these are equally consequential to the trust the Constitution requires (`PROJECT_CONSTITUTION.md`: Product Goal 6, 8; `ARCHITECTURE.md` Section 16).
- Whether denied authorization attempts (not just successful mutating actions) must also be recorded is not established by any approved document — see Open Questions.
- Audit records are the mechanism by which an Admin/Staff member can detect and resolve a scheduling problem without reading code or querying a database directly (`PROJECT_CONSTITUTION.md`: Success Criteria), supporting the "Operational resolution time" success metric (`BUSINESS_MODEL.md`: Success Metrics).

## Security Principles

Carried forward from `PROJECT_CONSTITUTION.md` and applied specifically to authentication/authorization:

1. **Security by design** — authentication and authorization are architected here, before any implementation choice. (Security Principle 1)
2. **Least privilege** — see Authorization Principles and Permission Model above. (Security Principle 2)
3. **Protection of personal data** — identity data collection is minimized to what each role's function requires (DATA-1), and data belonging to minors is handled via the Parent/Guardian relationship to a GDPR-grade standard. (Security Principle 3; CONST-4)
4. **Accountability for access** — every action affecting another party's data or schedule is attributable to an authenticated identity and role, which is why authentication must always resolve to a durable, non-anonymous identity. (Security Principle 4; CONST-2)
5. **Assume breach, design for containment** — bounded-context ownership (`ADR-002`) limits blast radius: a compromise of Discovery's read access does not itself grant write authority over Scheduling & Booking or Identity & Relationship data, since those remain owned and enforced elsewhere. (Security Principle 5)
6. **Security decisions are owned, not implicit** — the specific authentication mechanism, once chosen, is a Structural decision requiring its own explicit proposal and approval; it is not decided by this ADR. (Security Principle 6; `PROJECT_CONSTITUTION.md`: Decision-Making Process)

## Alternatives Considered

- **Coarse-grained role check only, no relationship scoping** — rejected. A role check alone cannot express that a Parent/Guardian is authorized only for specific, confirmed Students, which is a hard requirement, not an optional refinement (`PRODUCT_REQUIREMENTS.md` IDR-3, IDR-4).
- **Role-based access control augmented with relationship-scoped authorization (chosen)** — matches the two-tier model already proposed in `ARCHITECTURE.md` Section 17 and is the minimum model capable of expressing every documented rule without inventing new ones.
- **Authorization decisions made in Infrastructure or Presentation** — rejected. Business authorization (e.g., "is this Tutor approved," "is this Relationship confirmed") depends on Domain state owned by specific bounded contexts (`ADR-002`); deciding it outside the Domain would duplicate or leak business rules across layers, contradicting Architecture Principle 1 and `ADR-002`'s leakage-prevention rules.
- **A centralized authorization store independent of Identity & Relationship** — rejected. It would duplicate identity/relationship facts outside their single owning context, violating CONST-3 and `ADR-002`'s data ownership rules.
- **A fixed decision on Admin/Staff tiering (flat vs. tiered)** — not decided here; the approved documents do not settle it (`DOMAIN_MODEL.md` Open Question 12), so no choice is made rather than one being invented.

## Consequences

- Every use case that mutates a Session, Availability Slot, Tutor, or Relationship must invoke an authorization check evaluated in the Application/Domain layer of the owning context before the mutation proceeds.
- Presentation and Infrastructure may participate in authentication (verifying an identity) but must not make the final business-authorization decision; that decision is always deferred to the owning context's Application/Domain layer.
- Because Parent/Guardian authority is relationship-scoped, any relevant use case must resolve the caller's identity to their specific set of confirmed Student relationships before authorizing an action — a role flag alone is insufficient.

## Risks

- If authorization logic is implemented at the Presentation layer for convenience, business rules could leak out of the Domain layer, contradicting `ADR-002` and Architecture Principle 1 — this must be actively guarded against during implementation.
- Undefined Admin/Staff permission tiering (`DOMAIN_MODEL.md` Open Question 12) risks under- or over-granting Marketplace Oversight permissions until resolved.
- Undefined Tutor approval criteria (`DOMAIN_MODEL.md` Open Question 4) means the "approved" state currently functions only as a gate, not a substantiated vetting standard — a trust/safety risk already flagged in `BUSINESS_MODEL.md`.
- Undefined age threshold for "minor" (`PRODUCT_REQUIREMENTS.md` Open Question 10.1, Item 1) means the adult/minor authorization boundary cannot yet be implemented precisely.
- Undefined mechanism for Admin/Staff account creation (`DOMAIN_MODEL.md` Open Question 15) leaves a gap in how that role's identity is established at all.

## Future Evolution

If Admin/Staff permission tiering is later approved, the Role Model and Permission Model in this ADR would need extension through the Constitution's Decision-Making Process, not an ad hoc implementation change. If a new bounded context is introduced in the future (per `ADR-002`'s Future Evolution), it must integrate with this authorization model the same way existing contexts do: owning its own resources and reading identity/relationship facts from Identity & Relationship rather than duplicating them. The specific authentication mechanism remains an open Architectural Decision Candidate (`ARCHITECTURE.md` Section 21, Item 4) to be resolved in a future, separate decision.

## Open Questions

1. What is the exact age threshold distinguishing an adult Student from a minor Student? (`PRODUCT_REQUIREMENTS.md` Section 10.1, Item 1; `DOMAIN_MODEL.md` Open Question 1)
2. How is an Admin/Staff account created? (`DOMAIN_MODEL.md` Open Question 15; `ADR-002` Open Question 4)
3. Do Admin/Staff accounts have permission tiers? (`DOMAIN_MODEL.md` Open Question 12; `ADR-002` Open Question 5)
4. What credentials or information must a Tutor submit for approval, and what criteria does an Admin apply? (`DOMAIN_MODEL.md` Open Question 4; `ADR-002` Open Question 6)
5. Which role(s) may transition a Session to Completed or No-Show? (`DOMAIN_MODEL.md` Open Question 6)
6. Must denied authorization attempts be recorded in the audit trail, in addition to successful mutating actions? Not addressed by any approved document.
7. What are the exact personal data fields comprising each role's Account identity? (`PRODUCT_REQUIREMENTS.md` Section 10.5, Item 17)
8. What is the exact mechanism for the Parent-Student invitation/confirmation flow, and at what point does relationship-scoped authorization begin to apply? (`DOMAIN_MODEL.md` Open Question 2)

## Traceability

| Element of this ADR | Source |
|---|---|
| Least privilege as the governing authorization principle | `PRODUCT_REQUIREMENTS.md` CONST-5; `PROJECT_CONSTITUTION.md`: Security Principle 2 |
| Attributability of every mutating action | `PRODUCT_REQUIREMENTS.md` CONST-2; `PROJECT_CONSTITUTION.md`: Security Principle 4 |
| Role-aware, not role-hardcoded authorization | `PROJECT_CONSTITUTION.md`: Architecture Principle 4 |
| Two-tier (role + relationship) authorization model | `ARCHITECTURE.md` Section 17 |
| Identity owned exclusively by Identity & Relationship | `ADR-002`: Context Ownership, Data Ownership Rules |
| Business logic enforced only in the owning context | `ADR-002`: Rules That Prevent Business Logic Leakage |
| Permission Model per role | `PRODUCT_REQUIREMENTS.md` IDR-1 to IDR-6, SCH-3, SCH-5, SCH-7, DISC-1, DISC-2, ADM-1 to ADM-5 |
| Resource ownership per bounded context | `ADR-002`: Context Ownership |
| Audit requirements | `PRODUCT_REQUIREMENTS.md` CONST-2; `ARCHITECTURE.md` Section 16 |
| GDPR-grade protection of personal data | `PRODUCT_REQUIREMENTS.md` CONST-4; `PROJECT_CONSTITUTION.md`: Security Principle 3 |
| Deferred authentication mechanism | `ARCHITECTURE.md` Section 21, Item 4 |

---

## Addendum: RC1 Governing Decisions (Appended — 2026-07-21)

The following five decisions were made explicitly by the Project Director through the Constitution's Decision-Making Process (Launch Preparation, Priority 2, following the WP2/WP3 Business Rule Verification). They are recorded here as an addendum only — nothing above this section is altered. Where a decision resolves an Open Question listed above, that question is not edited; this addendum is its resolution of record.

**Decision 1 — Session Completion Authority** (resolves Open Question 5 above / `DOMAIN_MODEL.md` Open Question 6): `CompleteSession` and `MarkSessionNoShow` are each permitted to the Tutor assigned to that specific Session, and to Admin/Staff. Student and Parent/Guardian may not perform either action. Rationale: the Tutor is the service provider and owns execution of the lesson; Admin remains the operational override. Note the "Tutor assigned to that specific Session" qualifier is a fine-grained, resource-instance check (which Tutor, which Session) — distinct from the coarse-grained "does the Tutor role have this permission at all" check, per this ADR's own two-tier model (Authorization Principles, above).

**Decision 2 — `GET /tutors/{id}` Visibility**: an approved (discoverable) Tutor's detail is Public. A pending or suspended Tutor's detail is Owner (that Tutor) + Admin only.

**Decision 3 — Student / Parent-Guardian Visibility**: a confirmed Relationship (`IDR-3`, `IDR-4`) grants read visibility of the linked party's account profile in both directions — a confirmed Parent/Guardian may view the linked Student's profile, and a Student may view the linked Parent/Guardian's profile. No visibility exists without a confirmed Relationship.

**Decision 4 — Availability Slot Visibility**: an Availability Slot's detail is visible to the declaring Tutor (owner), the Student with a booking against it, the Parent/Guardian managing that booking, and Admin. No other authenticated user may read it.

**Decision 5 — Health Endpoint**: `GET /health` is Public — no authentication, no authorization. No business information may ever be exposed through it.

**Architectural note carried forward from these decisions**: Decisions 1, 2, 3, and 4 are each fine-grained, resource-instance-specific rules under this ADR's own two-tier model (Authorization Principles: "a role check alone cannot express... a fine-grained check where the rule requires it"). None can be enforced by a coarse-grained, role-only mechanism (e.g. Priority 2 WP3's Authorization Middleware) — each requires a check against the specific resource's own state (which Tutor is assigned, which Relationship is confirmed, which booking exists), evaluated in the Application/Domain layer of the bounded context that owns that resource, consistent with this ADR's Resource Ownership and Cross-Context Authorization Rules sections above. Decision 5 is the one purely coarse-grained (role-independent) rule of the five.

---

## Second Addendum: WP3 Verification Governance Items (Appended — 2026-07-21)

Raised by the Project Director's independent Final Gate verification of Priority 2, WP3 (Authorization Middleware). Nothing above this section, including the first Addendum, is altered.

**Correction 1 — Relationship Invitation/Confirmation Permissions.** The Permission Model table above and `RolePermissionCatalog` (WP2) granted `InviteRelationship`/`ConfirmRelationship` to the Parent/Guardian row only. This contradicts `PRODUCT_REQUIREMENTS.md` IDR-4, which this ADR's own authoritative-sources ordering places above this document: "a Parent-Student relationship is established through an invitation issued by **one party** and confirmation by **the other**" — either the Parent/Guardian or the Student may be the inviter, with the other confirming. This is a documentation defect (`PROJECT_CONSTITUTION.md`: Documentation Governance, Item 3 — "contradictions between documents are treated as defects... resolved by determining which document is authoritative... and correcting the other"), not a new business decision: IDR-4 already settles the answer, and no Director judgment call was required to resolve it. Correction: `InviteRelationship` and `ConfirmRelationship` are permitted to **both** Student and Parent/Guardian (coarse-grained). The fine-grained rule that the confirming party must be the *other* party to that specific Relationship's invitation (not the same party who issued it) is, consistent with every other fine-grained rule in this ADR, evaluated in Identity & Relationship's own Application-layer handler — never expressible by a role-only check.

**Decision 6 — Audit Trail Visibility** (new Open Question raised by WP3 verification, not previously carried forward): `GET /audit-entries` is Admin/Staff only. Rationale: `PROJECT_CONSTITUTION.md`'s Success Criteria frames the audit trail as the mechanism by which "an admin can detect and resolve scheduling problems," and this ADR's own Audit Requirements tie its purpose to operational/security accountability, not general-purpose access; no approved document gives any other role a reason to read cross-platform audit data naming every other actor's identity and actions. This is a coarse-grained, role-only rule — no resource-instance qualifier applies, since the permission is inherently platform-wide (mirroring `ViewAllSchedules`, ADM-3).

**Decision 7 — Session Detail Visibility** (new Open Question raised by WP3 verification): `GET /sessions/{id}` follows the same rule as Decision 4 (Availability Slot Visibility): visible to the Tutor on that Session, the Student on that Session, the Parent/Guardian who booked it (if applicable), and Admin/Staff. No other authenticated user may read it. Rationale: a Session is the direct product of booking an Availability Slot and carries the same parties Decision 4 already named; no basis exists for a narrower or broader rule. This is fine-grained (resource-instance-specific), like Decisions 1–4, and cannot be enforced by a coarse-grained, role-only mechanism.

**Architectural note**: Decision 6 is coarse-grained only, alongside Decision 5 (Health Endpoint) — both are fully expressible by Priority 2 WP3's Authorization Middleware alone, with no fine-grained follow-up required. Decision 7 and Correction 1's fine-grained half join Decisions 1–4 as work that must be implemented in the owning bounded context's Application layer, never in Presentation or Infrastructure, per this ADR's Resource Ownership and Cross-Context Authorization Rules.

*Status: Accepted — 2026-07-21. Correction 1 and Decisions 6–7 recorded above; nothing else in this ADR, including the first Addendum, is changed.*

---

## Third Addendum: Resolution of the Six Remaining Open Authorization Rows (Appended — 2026-07-21)

Raised by the Final Architecture Review Board's production-readiness audit (`IMPLEMENTATION_PLAN_V3.md`) and resolved by the Project Director through the Constitution's Decision-Making Process. These are the six rows `docs/api/AUTHORIZATION_MATRIX.md` §5 previously listed as genuinely Open. Nothing above this section, including the first and Second Addenda, is altered.

**Decision 8 — `GET /tutors/pending` Visibility**: Admin/Staff only. Coarse-grained, enforced via the existing `ApproveTutor` permission (no new `Permission` enum value) — viewing the pending-approval queue is the direct precondition of ADM-1, and this is the least-permissive option consistent with Addendum Decision 2's already-ratified rule that an individual pending Tutor's detail is Owner + Admin only (a bulk list of the same non-public data cannot be more permissive than the single-record view).

**Decision 9 — `GET /relationships/{id}` Visibility**: visible to either named party to the Relationship (the Parent/Guardian or the Student), regardless of whether it is Invited or Confirmed, and to Admin/Staff. No other authenticated user. Rationale: IDR-4 requires the invited party to confirm a Relationship via the already-approved `ConfirmRelationship` capability, which is only reachable if the invited party can resolve a `RelationshipId` — pre-confirmation visibility to both named parties is a functional precondition of an already-approved capability, not a new grant. Admin visibility follows this ADR's Marketplace Oversight authority (ADM-4, ADM-5).

**Decision 10 — `GET /accounts/{id}/relationships` Visibility**: visible to the named account itself and to Admin/Staff. No other authenticated user. Rationale: the narrowest self-service reading of IDR-3 ("when they access their account, then they can view and manage all linked Students"); Admin visibility follows ADM-4/ADM-5 as in Decision 9.

**Decision 11 — `GET /students/{id}/schedule` Visibility**: visible to the named Student, a Parent/Guardian with a confirmed Relationship to that Student, and Admin/Staff. No other authenticated user. Rationale: directly sourced, not inferred — `PRODUCT_REQUIREMENTS.md` §5.2 step 6 states a Parent/Guardian may "view the schedules of all linked Students in one place." The confirmed-Relationship restriction mirrors the already-ratified Addendum Decision 3 (account-profile visibility) applied to the same protected relationship. Admin visibility follows ADM-3.

**Decision 12 — `GET /tutors/{id}/schedule` Visibility**: visible to the named Tutor only, and Admin/Staff. No other authenticated user — explicitly not the Student or Parent/Guardian party to any individual session with that Tutor, since they already have a correctly-scoped view of their own session via the existing `GET /sessions/{id}` (Decision 7). Rationale: `PRODUCT_REQUIREMENTS.md` CONST-5 states verbatim, "Tutor cannot see another Tutor's schedule"; granting any other party the Tutor's *full* schedule would leak unrelated families' bookings, contradicting the same CONST-5 sentence ("Student/Parent cannot see another family's data"). Admin visibility follows ADM-3.

**Decision 13 — `GET /tutors/{id}/availability-slots` Visibility**: any authenticated caller (regardless of role) may view the list when the Tutor is discoverable (`Tutor.IsDiscoverable`); when not discoverable, visible only to the named Tutor and Admin/Staff, mirroring Addendum Decision 2's own-profile rule exactly. Explicitly **not** visible to an unauthenticated caller under any circumstance — the Project Director's decision narrows the candidate "Public when discoverable" reading (which would have mirrored Decision 2 exactly) to "authenticated when discoverable," consistent with this ADR's Authorization Principles ("least privilege") and the instruction not to assume a permissive default where the approved documents underdetermine the answer. `PRODUCT_REQUIREMENTS.md` §5.1 steps 2–4 and DISC-1 establish that browsing a Tutor's specific open slots is a necessary precondition of the already-approved public search-then-book flow, but do not by themselves establish that the slot list must be reachable by a fully anonymous caller rather than any signed-in one.

**Architectural note**: all six decisions are fine-grained, resource-instance-specific rules under this ADR's own two-tier model, joining Decisions 1–4, 7, and Correction 1 as work implemented in the owning bounded context's Application layer — except Decision 8, which is the one coarse-grained-only rule of the six (alongside Decisions 5–6), fully expressible by `RequirePermission(ApproveTutor)` alone with no fine-grained follow-up.

*Status: Accepted — 2026-07-21. Decisions 8–13 recorded above; nothing else in this ADR, including the first and Second Addenda, is changed. This resolves the last six Open rows in `docs/api/AUTHORIZATION_MATRIX.md`; zero rows remain Open as of this addendum.*
