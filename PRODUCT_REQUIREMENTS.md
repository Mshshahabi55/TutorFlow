# TutorFlow — Product Requirements (v1)

**Authoritative source:** `PROJECT_CONSTITUTION.md` (approved 2026-07-19, immutable). Every requirement below must trace back to that document's Vision, Mission, Product Goals, or Scope, or to an explicit business decision recorded on 2026-07-19. Anything not covered by either is recorded as an Open Question (Section 10) rather than assumed.

**Status:** Draft — pending user approval. No architecture, design, or source code work proceeds on any item in this document until it is approved.

---

## 1. Purpose

This document translates the Constitution's Product Goals into concrete, buildable product requirements for v1 — a scheduling and booking marketplace connecting Students, Parents/Guardians, and Tutors, with Admin/Staff oversight. It does not introduce any goal, scope boundary, or principle beyond what the Constitution and the 2026-07-19 business decisions establish.

## 2. Business Objectives

Derived directly from the Constitution's Mission, Product Goals, and Success Criteria:

- Establish a reliable, trustworthy scheduling and booking foundation for a multi-sided tutoring marketplace (Constitution: Mission).
- Ensure no session is ever double-booked, lost, or left in an inconsistent state (Constitution: Product Goal 6, Success Criteria).
- Enable admins to detect and resolve scheduling problems without needing to read code or query a database directly (Constitution: Success Criteria).
- Give each of the four roles — Student, Tutor, Parent/Guardian, Admin/Staff — a coherent, non-contradictory view of the same schedule data (Constitution: Core Principle 4).
- Build a foundation able to sustain commercial growth — more tutors, more students, more markets — without compromising v1 reliability (Constitution: Product Goal 7).
- Engineer trust, accountability, and protection of user data as product qualities from the first release (Constitution: Product Goal 8).

## 3. Value Proposition

- **Students & Parents/Guardians:** a simple, unambiguous way to find a suitable tutor and book, reschedule, or cancel a session with confidence that it will happen as agreed (Constitution: Vision, Product Goal 1–2).
- **Tutors:** a reliable way to manage availability and a roster of students without administrative overhead (Constitution: Product Goal 3).
- **Parents/Guardians specifically:** visibility and appropriate control over sessions booked on behalf of a student, without confusion over whose action took precedence (Constitution: Product Goal 4).
- **Admins/Staff:** the operational tools needed to run and support the marketplace (Constitution: Product Goal 5).
- **The business:** a narrow, trustworthy foundation at the single job of connecting people around a scheduled session, earning the right to expand from that trust (Constitution: Mission).

## 4. Roles (per Constitution, Project Scope)

- **Student** — the person receiving tutoring.
- **Tutor** — an independent tutor offering sessions.
- **Parent/Guardian** — may act on behalf of a Student.
- **Admin/Staff** — operates and supports the marketplace.

## 5. User Journeys

Each journey reflects only the functional requirements defined in Section 6; steps whose exact rules are undecided are marked accordingly and remain governed by Section 10 (Open Questions).

### 5.1 Student

1. Register as a Student (self-registration). If a minor, an associated Parent/Guardian is required before booking.
2. Search for a Tutor by Subject, Availability, Language, or Location/Time Zone.
3. View a Tutor's profile, including hourly rate and offered session duration(s).
4. Book an available slot; the booking is confirmed immediately, with no Tutor approval step.
5. View the resulting session, including its delivery mode (Online or In-Person) and status (Scheduled).
6. Cancel or reschedule the session, subject to notice-period rules to be defined (Open Question 10.2).
7. After the session occurs (outside the platform), its status is updated to Completed or No-Show by a role to be defined (Open Question 10.2).

### 5.2 Parent/Guardian

1. Register as a Parent/Guardian (self-registration).
2. Establish a relationship with one or more Students by invitation, confirmed by the other party.
3. View and manage bookings for any linked Student.
4. Search for and book a Tutor on behalf of a linked Student.
5. Cancel or reschedule a session booked on behalf of a linked Student, subject to notice-period rules to be defined (Open Question 10.2).
6. View the schedules of all linked Students in one place.

### 5.3 Tutor

1. Register as a Tutor (self-registration).
2. Await Admin approval; the account is not publicly discoverable until approved.
3. Once approved, define the session duration(s) offered, hourly rate, and availability.
4. Availability slots become immediately bookable by Students/Parents, with no manual acceptance step per booking.
5. View the resulting schedule of booked sessions, including each session's delivery mode and status.
6. Cancel or reschedule a session, subject to notice-period rules to be defined (Open Question 10.2).

### 5.4 Admin

1. Review pending Tutor registrations.
2. Approve a Tutor, making them publicly discoverable, or suspend an existing Tutor.
3. View all schedules — availability and bookings — across the platform.
4. Identify and resolve booking conflicts (mechanics to be defined, Open Question 10.4).
5. Manage user accounts across all roles.

## 6. Functional Requirements

Each requirement is traceable to the 2026-07-19 business decisions (cited as "Decision #") or directly to the Constitution ("Constitution: ...").

### 6.1 Identity & Relationships

| ID | Requirement | Priority | Acceptance Criteria | Related Product Goal | Source |
|---|---|---|---|---|---|
| IDR-1 | Students, Parents/Guardians, and Tutors can self-register accounts. | Must | Given a person is not yet registered, when they choose to sign up as a Student, Parent/Guardian, or Tutor, then they can create an account without Admin intervention. | Goal 7 | Decision A.1 |
| IDR-2 | A Tutor account is not publicly discoverable until an Admin approves it. | Must | Given a Tutor has registered, when their account has not yet been approved by an Admin, then the Tutor does not appear in Student/Parent search results. | Goal 8 | Decision A.1 |
| IDR-3 | A Parent/Guardian may be associated with multiple Students; a Student may be associated with multiple Parents/Guardians (many-to-many). | Must | Given a Parent/Guardian is linked to one or more Students, when they access their account, then they can view and manage all linked Students; given a Student is linked to more than one Parent/Guardian, each can access that Student's bookings. | Goal 4 | Decision A.2 |
| IDR-4 | A Parent-Student relationship is established through an invitation issued by one party and confirmation by the other — it is never created unilaterally by a single party. | Must | Given a Parent/Guardian or Student initiates a relationship invitation, when the other party confirms it, then the relationship becomes active; it does not become active without confirmation from both parties. | Goal 4 | Decision A.2 |
| IDR-5 | An adult Student may book sessions independently, without a Parent/Guardian. | Must | Given a Student is an adult, when they search for and book a Tutor, then no Parent/Guardian involvement is required to complete the booking. | Goal 2 | Decision A.3 |
| IDR-6 | A minor Student requires an associated Parent/Guardian; a minor cannot book independently. | Must | Given a Student is a minor, when they attempt to book a session, then the booking can only be completed with an associated, confirmed Parent/Guardian. | Goal 4 | Decision A.3 |

### 6.2 Scheduling & Booking

| ID | Requirement | Priority | Acceptance Criteria | Related Product Goal | Source |
|---|---|---|---|---|---|
| SCH-1 | TutorFlow's scope is limited to scheduling, booking, and (as of 2026-07-28) orchestrating third-party online-meeting delivery for an already-booked Online Session. It does not build its own video/conferencing infrastructure, host meeting media itself, or deliver any other aspect of the tutoring session (materials, grading, etc.). | Must | Given a Session is booked, when the booking is confirmed, then the platform records the scheduling data and, for an Online Session with a configured meeting provider, can create/update/cancel a third-party meeting and surface its join/host links; it never hosts the video/audio itself. | Goal 2 | Decision B.4; amended 2026-07-28 by `docs/adr/ADR-023-online-lesson-meeting-provider-architecture.md` |
| SCH-2 | A session has a delivery mode of either Online or In-Person. For Online, the actual video/audio delivery happens via a pluggable third-party meeting provider (Google Meet, Microsoft Teams, Zoom) that TutorFlow orchestrates — see ADR-023 — never TutorFlow's own video/conferencing infrastructure. For In-Person, delivery logistics remain fully outside the platform. | Must | Given a Tutor creates an availability slot or a session is booked, when the session record is created, then it stores a delivery mode of either Online or In-Person; an Online session may subsequently have a Meeting created against it via a configured provider. | Goal 1 | Decision B.4; amended 2026-07-28 by `docs/adr/ADR-023-online-lesson-meeting-provider-architecture.md` |
| SCH-3 | Each Tutor defines the duration(s) of the sessions they offer. | Must | Given a Tutor sets up their offering, when they define availability, then they specify the session duration(s) they offer. | Goal 3 | Decision B.5 |
| SCH-4 | Recurring/standing bookings are out of scope for v1. Only discrete, single-session bookings are supported. | Must | Given a Student/Parent books a session, when they complete the booking, then only a single, discrete session is created; no option to create a recurring series is presented. | Goal 2 | Decision B.6 |
| SCH-5 | A Tutor's declared availability slot is immediately bookable by a Student/Parent; no manual Tutor confirmation step occurs after a booking is made. | Must | Given a Tutor has an available slot, when a Student/Parent books it, then the session is immediately confirmed without requiring further action from the Tutor. | Goal 2 | Decision B.7 |
| SCH-6 | A session has one of the following statuses: **Scheduled**, **Completed**, **Cancelled**, **No-Show**. | Must | Given a session exists, when its state is inspected, then it has exactly one of the statuses Scheduled, Completed, Cancelled, or No-Show. | Goal 6 | Decision B.9 |
| SCH-7 | Students, Parents/Guardians, Tutors, and Admins are each permitted to cancel or reschedule a session, subject to business rules to be defined later (see Open Questions 10.2). | Must | Given a session is Scheduled, when a Student, Parent/Guardian, Tutor, or Admin initiates a cancellation or reschedule, then the platform allows the action, subject to notice-period rules to be defined. | Goal 2 | Decision B.8 |

### 6.3 Discovery

| ID | Requirement | Priority | Acceptance Criteria | Related Product Goal | Source |
|---|---|---|---|---|---|
| DISC-1 | Students and Parents can search/filter Tutors by Subject, Availability, Language, and Location/Time Zone. | Must | Given a Student/Parent searches for a Tutor, when they apply filters for Subject, Availability, Language, or Location/Time Zone, then results are limited to Tutors matching the selected criteria. | Goal 1 | Decision C.10 |
| DISC-2 | A Tutor's hourly rate is visible to Students/Parents. No payment processing occurs on the platform. | Must | Given a Tutor profile is displayed, when a Student/Parent views it, then the Tutor's hourly rate is shown; no payment is processed through the platform. | Goal 1 | Decision C.11; Constitution: Project Scope |
| DISC-3 | Ratings and reviews of Tutors are out of scope for v1. | Must | Given a Tutor profile is displayed, when a Student/Parent views it, then no rating or review feature is presented. | Goal 1 | Decision C.12 |

### 6.4 Admin

| ID | Requirement | Priority | Acceptance Criteria | Related Product Goal | Source |
|---|---|---|---|---|---|
| ADM-1 | An Admin can approve a Tutor application, making the Tutor publicly discoverable. | Must | Given a Tutor registration is pending, when an Admin approves it, then the Tutor becomes discoverable to Students/Parents. | Goal 5 | Decision D.13; IDR-2 |
| ADM-2 | An Admin can suspend a Tutor. | Must | Given a Tutor account is active, when an Admin suspends it, then the Tutor is no longer discoverable or bookable. | Goal 5 | Decision D.13 |
| ADM-3 | An Admin can view all schedules across the platform. | Must | Given an Admin is authenticated, when they access the schedule view, then they can see availability and bookings across all Tutors, Students, and Parents/Guardians. | Goal 5 | Decision D.13 |
| ADM-4 | An Admin can resolve booking conflicts. | Must | Given a booking conflict exists, when an Admin investigates it, then the Admin has the tools needed to resolve it (exact mechanics to be defined, see Open Questions 10.4). | Goal 5 | Decision D.13 |
| ADM-5 | An Admin can manage user accounts. | Must | Given a user account exists, when an Admin needs to act on it, then the Admin can view and manage that account. | Goal 5 | Decision D.13 |

### 6.5 Data

| ID | Requirement | Priority | Acceptance Criteria | Related Product Goal | Source |
|---|---|---|---|---|---|
| DATA-1 | Only the minimum personal data required for each role's function is collected. | Must | Given any role's account is created, when data is collected, then only the fields required for that role's function are requested (exact fields to be defined, see Open Questions 10.5). | Goal 8 | Decision E.14; Constitution: Security Principle 2, Principle 3 |
| DATA-2 | All product UI, content, and documentation are in English for v1. | Must | Given any user accesses the platform, when they view UI, content, or documentation, then it is presented in English. | Goal 7 | Decision E.15 |
| DATA-3 | The product must be designed so that future international expansion does not require re-architecting the data or compliance model. | Should | Given the data and compliance model is implemented for v1, when future international expansion is considered, then no re-architecture of that model is required to support it. | Goal 7 | Decision E.15; Constitution: Architecture Principle 7 |

Note: the initial launch/target market itself is an Open Question — see Section 10.5.

### 6.6 Platform

| ID | Requirement | Priority | Acceptance Criteria | Related Product Goal | Source |
|---|---|---|---|---|---|
| PLAT-1 | v1 is delivered as a web application only; no native mobile application is built for v1. | Must | Given a user accesses TutorFlow, when they do so, then it is available as a web application; no native mobile application is provided in v1. | Goal 7 | Decision F.16 |

### 6.7 Tutor Profile Enrichment (added 2026-07-28)

Resolves Section 10.1 Item 4 and Section 10.3 Item 10 — see `docs/adr/ADR-024-tutor-profile-enrichment-and-onboarding-wizard.md` (Accepted) for full rationale.

| ID | Requirement | Priority | Acceptance Criteria | Related Product Goal | Source |
|---|---|---|---|---|---|
| PROF-1 | A Tutor may provide a display name, headline, biography, country, city, additional spoken languages, years of experience, education, certifications, teaching methodology, and lesson specialties — all self-declared free text, not independently verified. | Must | Given a Tutor completes their profile, when they submit these fields, then they are stored and shown on the Tutor's profile without any automated or third-party verification step. | Goal 1, Goal 7 | ADR-024 |
| PROF-2 | A Tutor may teach more than one subject, optionally at a stated level per subject, in addition to their existing single primary Subject. Discovery's search/filter behavior is unchanged by this — it continues to filter on the primary Subject. | Should | Given a Tutor has multiple subjects, when their profile is viewed, then every declared subject/level is shown; search results are unaffected. | Goal 1 | ADR-024 |
| PROF-3 | A Tutor may provide a profile photo URL, an introductory video URL, and gallery image URLs — links to externally-hosted content. TutorFlow does not host, store, or validate the linked media, and provides no file-upload capability in v1. | Could | Given a Tutor provides a media URL, when their profile is viewed, then the linked content is referenced (e.g. as an image `src` or a link); no upload workflow exists. | Goal 1 | ADR-024 |
| PROF-4 | A Tutor may declare trial-lesson availability and a trial-lesson price, in the same single currency (Rial/Toman) as the existing hourly rate. No payment is processed for a trial lesson in v1. | Could | Given a Tutor sets trial-lesson availability/price, when a Student/Parent views the profile, then it is shown; no payment flow is triggered. | Goal 1 | ADR-024; DISC-2 |
| PROF-5 | A Tutor's profile has a completion state of Draft or Submitted, tracked separately from Admin approval (`IsApproved`/`IsSuspended`, IDR-2/ADM-2, unchanged). Only a Submitted profile enters the existing Admin approval queue. | Must | Given a Tutor has not yet submitted their profile, when they leave the onboarding flow, then their progress is saved and not discoverable; given they submit, then the profile enters the existing approval queue unchanged. | Goal 8 | ADR-024 |

## 7. Requirements Directly Mandated by the Constitution

These are not new business decisions — they restate binding Constitution principles in product terms, since any v1 feature must satisfy them.

| ID | Requirement | Source |
|---|---|---|
| CONST-1 | The system must never allow a Tutor's slot to be booked by more than one Student/Parent at the same time (no double-booking). | Constitution: Core Principle 3, Product Goal 6, KPI "Booking reliability" |
| CONST-2 | Every action that creates, reschedules, or cancels a booking or changes availability must be attributable to an authenticated identity, role, and timestamp. | Constitution: Architecture Principle 3, Security Principle 4 |
| CONST-3 | Availability and booking data has a single authoritative source that all four roles read from — no role-specific copies that can drift. | Constitution: Architecture Principle 2 |
| CONST-4 | Personal data (including data relating to minors, handled via Parent/Guardian) is handled to a standard consistent with the data-protection standards expected of a company operating in the European market. | Constitution: Security Principle 3 |
| CONST-5 | Each role is granted access only to the data and actions necessary for its function (Student/Parent cannot see another family's data; Tutor cannot see another Tutor's schedule; etc.). | Constitution: Security Principle 2 |

## 8. Non-Functional Requirements

| ID | Category | Requirement | Source |
|---|---|---|---|
| NFR-1 | Reliability | Zero tolerance for double-booking. | CONST-1; Constitution: Core Principle 3 |
| NFR-2 | Reliability | The system fails safely and visibly rather than silently corrupting schedule data. | Constitution: Engineering Principle 4 |
| NFR-3 | Security & Privacy | Role-based least-privilege access: each role can access only the data and actions necessary for its function. | CONST-5; Constitution: Security Principle 2 |
| NFR-4 | Security & Privacy | Personal data is handled to a standard consistent with data protection expectations for a company operating in the European market. | CONST-4; Constitution: Security Principle 3 |
| NFR-5 | Auditability | Every schedule-affecting action is attributable to an authenticated identity, role, and timestamp. | CONST-2; Constitution: Architecture Principle 3 |
| NFR-6 | Data Integrity | Availability and booking data has a single authoritative source; no role-specific copies that can drift. | CONST-3; Constitution: Architecture Principle 2 |
| NFR-7 | Localization | All v1 UI, content, and documentation are in English. | DATA-2 |
| NFR-8 | Maintainability | The system is understandable and changeable by someone other than its original author. | Constitution: Engineering Principle 2 |
| NFR-9 | Platform | v1 is delivered as a web application only. | PLAT-1 |

## 9. Out of Scope for v1

Carried forward from the Constitution's Project Scope, plus the 2026-07-19 decisions that narrow v1 further:

- Payments, billing, and payouts (Constitution: Project Scope).
- Learning-materials delivery (Constitution: Project Scope). Text-based in-platform messaging was brought into scope 2026-07-28 (Constitution: Project Scope; `docs/adr/ADR-022-communication-and-notifications-architecture.md`) — see that ADR for the Conversation/Message/Notification model. Online lesson delivery via pluggable third-party meeting providers was also brought into scope 2026-07-28 (Constitution: Project Scope; `docs/adr/ADR-023-online-lesson-meeting-provider-architecture.md`) — see that ADR for the Meeting/IMeetingProvider model. Learning-materials delivery itself remains out of scope.
- Progress tracking, grading, or outcome reporting (Constitution: Project Scope).
- Recurring/standing bookings (Decision B.6).
- Ratings and reviews (Decision C.12).
- Native mobile applications (Decision F.16).
- Real file/media upload infrastructure for Tutor profile photo/video/gallery — v1 accepts externally-hosted URLs only (`ADR-024`, added 2026-07-28).
- Third-party or automated Tutor identity/education/certificate/background-check verification — v1 verification is self-attestation reviewed by the existing Admin approve/suspend gate only (`ADR-024`, added 2026-07-28).
- Payment for trial lessons, or any payment processing whatsoever — unchanged, still gated behind the unresolved payments question (`ADR-020`/`ADR-021`).

## 10. Open Questions

Everything below is a genuine gap not covered by the Constitution or the 2026-07-19 decisions. None of it is assumed, defaulted, or implemented until answered.

### 10.1 Identity & Relationships
1. What is the exact age threshold distinguishing an "adult" Student from a "minor" Student in v1?
2. What is the exact mechanism for the Parent-Student invitation/confirmation flow (e.g., email invite, in-app code, Admin-mediated)?
3. May a minor Student complete self-registration before any Parent/Guardian relationship is confirmed, and if so, can that account book while unlinked?
4. ~~What information or credentials does a Tutor submit for Admin approval, and what are the Admin's approval criteria?~~ **Resolved 2026-07-28** — see Section 6.7 (PROF-1 through PROF-5) and `ADR-024`: self-declared free-text fields, reviewed holistically by the existing, unchanged Admin approve/suspend gate. No automated or third-party verification in v1.

### 10.2 Scheduling & Booking
5. What are the cancellation/rescheduling notice-period rules, and do they differ by role?
6. Which role(s) are permitted to change a session's status among Scheduled, Completed, Cancelled, and No-Show?
7. When a session is cancelled, does the underlying availability slot automatically reopen for booking?
8. Is there a minimum or maximum lead time for booking a session (how far in advance it can or must be booked)?
9. Is there any limit on the number of concurrent or future bookings a Student or Tutor may hold?

### 10.3 Discovery
10. Is "Subject" a fixed, platform-defined taxonomy, or free text set by each Tutor? **Partially resolved 2026-07-28** — a Tutor may declare multiple subjects (`TutorSubjects`, Section 6.7 PROF-2), each free text with an optional free-text level, additive alongside the existing single `Subject` field (`ADR-024`). Whether the *existing* single `Subject` field itself is a fixed taxonomy or free text remains open — `ADR-024` did not revisit it, and Discovery's search/filter behavior is explicitly unchanged.
11. How are search results ordered/ranked when multiple Tutors match a search?
12. For in-person tutoring, does "Location" mean a specific address, a city/region, or a travel radius?
13. Do pagination or result-count limits apply to search results?

### 10.4 Admin
14. Do all Admin/Staff accounts share one flat permission level, or are there tiered admin roles?
15. What specific mechanics does "resolve booking conflicts" include (e.g., can an Admin unilaterally move or cancel a session, and is that logged per CONST-2)?

### 10.5 Data & Market
16. What is the initial launch/target market for v1?
17. What are the exact personal data fields collected for each role?
18. What are the data retention periods for each data category?
19. Are there jurisdiction-specific regulatory requirements beyond general GDPR-equivalent compliance, once a launch market is designated?

### 10.6 Platform
20. What browsers/devices and responsive-design expectations apply to the v1 web application?

---

## Addendum: Regional Deployment & Market Scope (Appended — 2026-07-21)

`docs/adr/ADR-018-regional-deployment-and-market-scope.md` (Accepted) resolves Section 10.5, Open Question 16: **TutorFlow v1 targets Iran only** — English-only UI, `Asia/Tehran` (UTC+03:30, no DST) as the operating timezone, a single currency (Rial/Toman), hosting inside Iran, and no dependency on Stripe, PayPal, Twilio, SendGrid, or any other US-hosted cloud service. Payments remain out of scope for v1 (Section 9, unchanged); ADR-018 records only the constraint a future payment integration must satisfy. Section 10.5 Open Question 19 (jurisdiction-specific regulatory requirements) is partially informed by this addendum but not fully resolved. Nothing above this addendum is altered.

---

*Status: Draft — pending user approval. No further documents or code are to be created until this document is approved.*
