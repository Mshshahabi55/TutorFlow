**Authoritative sources:** `PROJECT_CONSTITUTION.md` (approved 2026-07-19, immutable) and `PRODUCT_REQUIREMENTS.md` (approved 2026-07-19, immutable). No fact, assumption, or figure in this document originates from any other source. Where neither document establishes an answer, it is recorded under Open Questions rather than invented.

**Status:** Draft — pending user approval. No further documents or code are to be created until this document is approved.

---

# Executive Summary

TutorFlow is a multi-sided marketplace connecting independent Tutors with Students and their Parents/Guardians, with Admin/Staff providing operational oversight, built first around a reliable scheduling and booking foundation (Constitution: Mission). Approved product requirements establish self-service registration for all roles, an Admin-gated approval step before a Tutor becomes discoverable, instant-book scheduling with no manual Tutor acceptance step, and strict role-based access to schedule data. Core commercial elements of the business model — revenue mechanism, pricing strategy, target market, competitive position, and acquisition strategy — are not defined in either source document and are recorded as Open Questions below rather than assumed.

# Business Vision

"A world in which finding and working with the right tutor is as simple as booking a session — where students, parents, and tutors trust the platform to connect them reliably, and the friction of scheduling never gets in the way of learning."

This vision holds regardless of scale: whether TutorFlow serves its first hundred users or operates as a mature commercial marketplace across many markets, the experience of finding, booking, and trusting a tutoring session must remain simple, dependable, and worthy of the confidence placed in it. Growth is expected to increase the rigor applied to this vision, never to dilute it. (Constitution: Vision)

# Business Mission

"TutorFlow is a multi-tutor marketplace that connects independent tutors with students and their parents/guardians, starting with a reliable, trustworthy scheduling and booking experience as the foundation for everything else the platform will do."

This mission is deliberately narrow at inception: TutorFlow commits first to being trustworthy at the single job of connecting people around a scheduled session, and earns the right to expand from that trust. (Constitution: Mission)

# Value Proposition

- **Students & Parents/Guardians:** a simple, unambiguous way to find a suitable tutor and book, reschedule, or cancel a session with confidence that it will happen as agreed.
- **Tutors:** a reliable way to manage availability and a roster of students without administrative overhead.
- **Parents/Guardians specifically:** visibility and appropriate control over sessions booked on behalf of a student, without confusion over whose action took precedence.
- **Admins/Staff:** the operational tools needed to run and support the marketplace.
- **The business:** a narrow, trustworthy foundation at the single job of connecting people around a scheduled session, earning the right to expand from that trust.

(Source: `PRODUCT_REQUIREMENTS.md`, Section 3 — Value Proposition; Constitution: Vision, Product Goals 1–5)

# Target Market

Neither source document designates an initial launch market. The only market-relevant product decisions on record are that all v1 UI, content, and documentation are in English (`PRODUCT_REQUIREMENTS.md` DATA-2), and that the product must be designed so future international expansion does not require re-architecting the data or compliance model (`PRODUCT_REQUIREMENTS.md` DATA-3). The specific initial country or region of launch is an open question in `PRODUCT_REQUIREMENTS.md` (Section 10.5, Item 16) and remains open here.

# Customer Segments

Segments are drawn directly from the four defined roles (Constitution: Project Scope; `PRODUCT_REQUIREMENTS.md` Section 4):

- **Demand side:** Students, and Parents/Guardians acting on behalf of Students.
- **Supply side:** Tutors.
- **Operational (non-customer):** Admin/Staff, who operate and support the marketplace rather than transact on it.

No finer segmentation (e.g., by age group, subject vertical, or geography) is defined in either source document — see Open Questions.

# User Personas

Personas below are limited strictly to what the source documents establish for each role's goals and interactions (`PRODUCT_REQUIREMENTS.md`, Sections 3 and 5). No demographic, behavioral, or motivational detail beyond what is documented is included.

- **Student** — Goal: find a suitable tutor and book, view, cancel, or reschedule sessions with confidence. Registers independently if an adult; requires a confirmed Parent/Guardian relationship if a minor.
- **Parent/Guardian** — Goal: visibility and appropriate control over sessions booked on behalf of one or more linked Students. Establishes relationships with Students via invitation and confirmation; may manage bookings for any linked Student.
- **Tutor** — Goal: a reliable way to manage availability, session duration(s), and hourly rate, and to be found by Students/Parents. Requires Admin approval before becoming publicly discoverable.
- **Admin/Staff** — Goal: operate and support the marketplace — approving/suspending Tutors, viewing all schedules, resolving booking conflicts, and managing user accounts.

Richer persona detail (demographics, pain points, motivations beyond the above) is not established in either source document — see Open Questions.

# Revenue Model

Neither source document defines a revenue model. The Constitution places "Payments, billing, and payouts" explicitly out of scope for the foundational phase (Constitution: Project Scope), and `PRODUCT_REQUIREMENTS.md` confirms that no payment processing occurs on the platform in v1 (DISC-2; Out of Scope for v1). No commission, subscription, listing-fee, or other monetization mechanism is established — see Open Questions.

# Pricing Strategy

The only documented fact is that a Tutor's hourly rate is visible to Students/Parents (`PRODUCT_REQUIREMENTS.md` DISC-2). No platform-side pricing strategy — commission, service fee, discounting, or any influence on the Tutor-set rate — is defined in either source document, consistent with payment processing being out of scope for v1. See Open Questions.

# Marketplace Model

The following structure is drawn directly from the approved functional requirements (`PRODUCT_REQUIREMENTS.md`, Section 6):

- A multi-sided marketplace with self-service registration for Students, Parents/Guardians, and Tutors (IDR-1).
- Gated supply-side entry: a Tutor is not publicly discoverable until an Admin approves the account (IDR-2, ADM-1); an Admin may later suspend a Tutor (ADM-2).
- Many-to-many Parent-Student relationships, established only by mutual invitation and confirmation (IDR-3, IDR-4).
- Instant-book demand side: a Tutor's declared availability is immediately bookable, with no manual Tutor acceptance step (SCH-5).
- Single-session bookings only in v1; no recurring/standing bookings (SCH-4).
- Discovery via filtered search on Subject, Availability, Language, and Location/Time Zone (DISC-1).
- The platform manages scheduling and booking only; the tutoring session itself (online or in person) is delivered outside the platform (SCH-1, SCH-2).
- Admin/Staff oversight across the marketplace: approving Tutors, viewing all schedules, resolving conflicts, and managing accounts (ADM-1 through ADM-5).

# Tutor Verification

A Tutor account self-registers but is not publicly discoverable until an Admin approves it (`PRODUCT_REQUIREMENTS.md` IDR-2, ADM-1); an Admin may also suspend an existing Tutor (ADM-2). Neither source document specifies what information or credentials a Tutor must submit, nor the criteria an Admin uses to approve or reject an application. This is already recorded as an open question in `PRODUCT_REQUIREMENTS.md` (Section 10.1, Item 4) and remains open here.

# Customer Acquisition Strategy

Neither source document addresses acquisition channels, marketing, partnerships, or referral mechanisms. Self-service registration (IDR-1) is the only documented entry mechanism for any role. The strategy for attracting Students, Parents/Guardians, or Tutors to register in the first place is not defined — see Open Questions.

# Growth Strategy

The source documents establish a growth *philosophy*, not a concrete plan:

- "Build a foundation able to sustain commercial growth — more tutors, more students, more markets — without compromising the reliability established in v1." (Constitution: Product Goal 7)
- "Small foundation, deliberate growth. The project starts with the narrowest defensible scope (scheduling/booking) and expands only through explicit, documented decisions — not organic feature creep." (Constitution: Core Principle 5)
- "Evolvability over premature generality." (Constitution: Architecture Principle 7)
- The data and compliance model must be designed so future international expansion does not require re-architecting it (`PRODUCT_REQUIREMENTS.md` DATA-3).

No concrete growth tactics, milestones, sequencing, or expansion markets are defined — see Open Questions.

# Competitive Landscape

Neither source document identifies any competitor, alternative, or market positioning. This section cannot be populated without inventing information — see Open Questions.

# Risks

Risks below are limited to what the Constitution names as high-priority categories, plus risks directly implied by gaps already on record. No external or hypothetical risks (e.g., named competitors, specific market conditions) are introduced.

- **Scheduling integrity:** the Constitution treats any defect risking a double-booking, lost session, or inconsistent schedule state as critical by default and the marketplace's top-priority risk category (Constitution: Core Principle 3, Quality Principle 3, Risk Management Principle 3).
- **Data protection:** the platform handles personal data belonging to Students, including minors represented via Parent/Guardian, and must meet a data-protection standard consistent with the European market (Constitution: Security Principle 3; `PRODUCT_REQUIREMENTS.md` CONST-4).
- **Trust and safety of unverified criteria:** Tutor approval is Admin-gated, but the credentials required and approval criteria are not yet defined (`PRODUCT_REQUIREMENTS.md` Section 10.1, Item 4), so the trust bar the marketplace commits to is currently unspecified.
- **Business sustainability:** no revenue model is defined (see Revenue Model above), so the marketplace has no documented path to commercial sustainability at this stage.
- **Go-to-market uncertainty:** no initial launch market is designated (see Target Market above), so market-specific regulatory, demand, or competitive risk cannot yet be assessed.
- **Unmanaged unknowns:** the Constitution treats unmanaged or unknown risk as itself a risk requiring surfacing rather than silent carry-forward (Constitution: Risk Management Principle 4); the volume of open questions across the Constitution, `PRODUCT_REQUIREMENTS.md`, and this document should be tracked deliberately as documentation debt (Constitution: Documentation Governance, Item 4).

# Success Metrics

The Constitution defines the following KPI categories as illustrative of what must be measured; it explicitly defers specific numeric targets and instrumentation until the domain and metrics ownership are established (Constitution: KPI-Based Success Criteria):

- **Booking reliability** — rate of bookings completed without double-booking, loss, or inconsistency (target: zero tolerance for double-booking).
- **Time to first booking** — elapsed time from a student/parent starting a search to a confirmed session.
- **Availability accuracy** — rate of bookings that respected a tutor's declared availability without manual correction.
- **Role clarity** — volume of support or admin intervention caused by ambiguity between parent and student actions on a booking.
- **Operational resolution time** — time for an admin/staff member to detect and resolve a scheduling issue.
- **Trust retention** — repeat booking rate as a proxy for whether students, parents, and tutors continue to trust the platform after their first experience.

Specific numeric targets for each category are not yet defined — see Open Questions.

# Open Questions

1. **Target Market** — What is the initial launch/target market for v1? (Cross-referenced: `PRODUCT_REQUIREMENTS.md` Section 10.5, Item 16.)
2. **Customer Segments** — Is any finer segmentation planned beyond the four platform roles (e.g., by age group, subject vertical, or geography)?
3. **User Personas** — What demographic, behavioral, or motivational detail should inform fuller personas beyond the documented role goals and journeys?
4. **Revenue Model** — What revenue mechanism, if any, will fund the marketplace once payments are brought into scope?
5. **Pricing Strategy** — Will the platform apply a commission, listing fee, or subscription, and will it influence or cap the Tutor-set hourly rate?
6. **Tutor Verification** — What credentials or information must a Tutor submit, and what criteria does an Admin use to approve or reject a Tutor application? (Cross-referenced: `PRODUCT_REQUIREMENTS.md` Section 10.1, Item 4.)
7. **Customer Acquisition Strategy** — Through what channels, partnerships, or campaigns will Students, Parents/Guardians, and Tutors be acquired?
8. **Growth Strategy** — What specific sequencing, milestones, or expansion markets follow the documented "deliberate growth" principle?
9. **Competitive Landscape** — Who are TutorFlow's competitors or alternatives, and how is TutorFlow positioned relative to them?
10. **Success Metrics** — What are the specific numeric targets and instrumentation for each KPI category listed above?

---

*Status: Draft — pending user approval. No further documents or code are to be created until this document is approved.*
