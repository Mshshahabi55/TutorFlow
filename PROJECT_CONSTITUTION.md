# TutorFlow — Project Constitution

This document is the foundational governing reference for TutorFlow. It defines *why* the project exists and *how* decisions are made, before any code, tech stack, or design decisions are chosen. Every other document, plan, and line of code in this project must be consistent with this constitution. Where a conflict exists, this document wins until it is deliberately amended.

---

## Vision

A world in which finding and working with the right tutor is as simple as booking a session — where students, parents, and tutors trust the platform to connect them reliably, and the friction of scheduling never gets in the way of learning.

This vision holds regardless of scale. Whether TutorFlow serves its first hundred users or operates as a mature commercial marketplace across many markets, the experience of finding, booking, and trusting a tutoring session must remain simple, dependable, and worthy of the confidence placed in it by students, parents, and tutors alike. Growth is expected to increase the rigor applied to this vision, never to dilute it.

## Mission

TutorFlow is a multi-tutor marketplace that connects independent tutors with students and their parents/guardians, starting with a reliable, trustworthy scheduling and booking experience as the foundation for everything else the platform will do.

This mission is deliberately narrow at inception. TutorFlow does not attempt to be everything to everyone from day one; it commits first to being trustworthy at the single job of connecting people around a scheduled session, and earns the right to expand from that trust. This is the discipline expected of a commercial product that handles the time, personal data, and confidence of the people who use it.

## Product Goals

1. Make it easy for students and parents to find a suitable tutor.
2. Make booking, rescheduling, and canceling a tutoring session simple and unambiguous for every party.
3. Give tutors a reliable way to manage their availability and their roster of students.
4. Give parents/guardians visibility and appropriate control over sessions booked on behalf of a student.
5. Give admins/staff the tools needed to operate and support the marketplace (onboarding tutors, resolving scheduling issues, oversight).
6. Earn trust through reliability first — the platform must never lose, double-book, or silently drop a session.
7. Build a foundation able to sustain commercial growth — more tutors, more students, more markets — without compromising the reliability established in v1.
8. Treat trust, accountability, and the protection of user data as product qualities to be engineered deliberately from the first release, not as afterthoughts addressed once problems occur.

## Project Scope

**In scope for the project's foundational direction:**
- A multi-sided marketplace serving four roles: Students, Tutors, Parents/Guardians, and Admins/Staff.
- Scheduling and booking as the first and most important problem to solve.
- The relationships between roles (e.g., a parent managing a student's bookings, a tutor managing their availability) as first-class concerns.

**Explicitly out of scope until deliberately brought in scope:**
- Payments, billing, and payouts.
- In-platform communication or content delivery (messaging, video, materials).
- Progress tracking, grading, or outcome reporting.
- Technology stack, hosting, infrastructure, CI/CD, and tooling decisions — these are implementation decisions made later, and must not be assumed by this document or by early planning.

Scope changes must be reflected here before being acted on elsewhere.

## Success Criteria

TutorFlow's foundation is successful when:
- A student or parent can find a tutor and book a session without ambiguity about what happens next.
- A tutor can define their availability and trust that bookings will respect it.
- No session is ever double-booked, lost, or left in an inconsistent state between the parties involved.
- A parent can act on behalf of a student without confusion about whose action took precedence.
- An admin can see and resolve scheduling problems without needing to read code or query a database directly.
- Every decision made about the product can be traced back to a goal in this document.

## KPI-Based Success Criteria

Qualitative success criteria are complemented by measurable indicators. These KPIs are illustrative of the categories the business must track; specific targets and instrumentation are defined later, once the domain and metrics ownership are established — this document commits to *what* is measured, not *how*.

- **Booking reliability:** rate of bookings completed without double-booking, loss, or inconsistency (target: zero tolerance for double-booking).
- **Time to first booking:** elapsed time from a student/parent starting a search to a confirmed session.
- **Availability accuracy:** rate of bookings that respected a tutor's declared availability without manual correction.
- **Role clarity:** volume of support or admin intervention caused by ambiguity between parent and student actions on a booking.
- **Operational resolution time:** time for an admin/staff member to detect and resolve a scheduling issue.
- **Trust retention:** repeat booking rate as a proxy for whether students, parents, and tutors continue to trust the platform after their first experience.

## Core Principles

1. **Business requirements before implementation.** No technology, framework, or architecture decision is made until the business problem it serves is understood and documented.
2. **No assumptions.** If a requirement is unknown, it is asked about — it is not invented, inferred, or defaulted silently.
3. **Scheduling integrity is non-negotiable.** Since scheduling is the v1 core problem, correctness and reliability of bookings outweigh feature breadth or speed of delivery.
4. **Four roles, one truth.** Students, Tutors, Parents/Guardians, and Admins/Staff must each have a coherent, non-contradictory view of the same underlying schedule data.
5. **Small foundation, deliberate growth.** The project starts with the narrowest defensible scope (scheduling/booking) and expands only through explicit, documented decisions — not organic feature creep.

## Governance Principles

1. **Single point of accountability.** Every decision of consequence — scope, architecture, process, or risk acceptance — has a named owner accountable for it. Decisions are not made anonymously or by default.
2. **This constitution is supreme.** No lower-level document, plan, or individual decision may override this constitution. It may only be changed through the amendment process defined under Change Management.
3. **Stakeholder representation.** Governance decisions consider the interests of all four roles (Students, Tutors, Parents/Guardians, Admins/Staff) alongside the commercial interests of the business operating the platform.
4. **Transparency of decisions.** Significant decisions and their rationale are documented and discoverable, not held as tribal knowledge.
5. **Segregation of proposal and approval.** The party proposing a significant change is not, by default, the party approving it — proposals are reviewed and explicitly approved by the business owner before being acted upon.

## Engineering Principles

1. **Correctness before convenience.** Engineering choices favor verifiable correctness over developer convenience, especially wherever scheduling integrity is at stake.
2. **Build for maintainability.** Every part of the system must be understandable and changeable by someone other than its original author.
3. **Automate what can be verified.** Where a rule can be checked automatically, it should be, reducing reliance on manual vigilance. This principle does not itself prescribe any tool or technology.
4. **Fail safely and visibly.** When something goes wrong, the system must fail in a way that is detectable and does not silently corrupt schedule data or user trust.
5. **Incremental, reversible delivery.** Work is delivered in small, reviewable increments that can be verified and, where necessary, reversed without disproportionate cost.

## Security Principles

1. **Security by design.** Security and data protection are considered from the first design decision, not retrofitted after launch.
2. **Least privilege.** Each role — Student, Tutor, Parent/Guardian, Admin/Staff — is granted access only to the data and actions necessary for its function.
3. **Protection of personal data.** TutorFlow handles data belonging to students, including minors represented via parents/guardians. Personal data must be handled in compliance with applicable data protection law at every stage of design and operation, consistent with the standards expected of a company operating in the European market.
4. **Accountability for access.** Every action affecting another party's data or schedule must be attributable to an authenticated identity and role.
5. **Assume breach, design for containment.** Systems are designed so that a failure or compromise in one area is contained rather than catastrophic across the whole platform.
6. **Security decisions are owned, not implicit.** Like architecture decisions, security decisions are documented, attributed to an owner, and reviewed — never assumed.

## Quality Principles

1. **Quality is defined by trust, not polish.** For TutorFlow, quality means the schedule is always correct and every role can rely on what they see. This outweighs cosmetic or feature-completeness concerns.
2. **Verification precedes release.** No change is considered complete until it has been verified against the relevant requirement, principle, or success criterion.
3. **Scheduling-integrity defects are critical by default.** Any defect that risks a double-booking, a lost session, or an incorrect schedule state takes precedence over new feature work.
4. **Continuous improvement.** Quality practices are reviewed and improved over time as the product and organization mature; they are not fixed permanently at this early stage.

## Decision-Making Process

1. Decisions are classified by impact: **Foundational** (this constitution, scope, roles), **Structural** (architecture, process, governance), and **Operational** (day-to-day execution within already-approved boundaries).
2. Foundational decisions require explicit approval from the business owner and, once approved, are recorded in this constitution or a document it governs.
3. Structural decisions are proposed with their rationale and trade-offs, and require explicit approval before being acted upon.
4. Operational decisions may be made within the boundaries already established by approved Foundational and Structural decisions, without re-approving those boundaries each time.
5. When a decision's classification is unclear, it defaults to the higher-impact category and is escalated for approval rather than assumed.
6. Every Foundational or Structural decision is recorded with: the problem, the options considered, the recommendation, and the rationale.

## Architecture Principles

These are technology-agnostic principles that any future stack decision must honor. They do not prescribe a stack.

1. **Separation of concerns.** The system that owns scheduling logic must be identifiable and isolated from concerns like presentation, notification, or future payment/messaging features.
2. **Single source of truth for schedule state.** Availability, bookings, and their status must have one authoritative representation that all roles read from — no role-specific copies that can drift.
3. **Auditable actions.** Every action that changes a booking or availability (create, reschedule, cancel) must be attributable to a role and a timestamp, since trust depends on being able to answer "what happened and who did it."
4. **Role-aware, not role-hardcoded.** The system should model permissions and relationships (e.g., parent-to-student) as data and rules, not as scattered special cases in logic.
5. **Design for reliability over cleverness.** Favor simple, verifiable mechanisms for booking/scheduling over optimizations that increase the risk of inconsistency.
6. **Defer irreversible technical decisions.** Stack, hosting, and infrastructure choices are made only once the domain model (roles, scheduling rules, relationships) is well understood.
7. **Evolvability over premature generality.** The architecture must be easy to extend as new goals are approved, without having been speculatively built for capabilities that are currently out of scope.
8. **Traceability from business rule to system behavior.** Every architectural decision of consequence must be traceable back to a domain rule, principle, or documented requirement — never to convenience alone.

## Architecture Governance

1. Architecture decisions of Structural or Foundational impact are documented as they are made, including the rationale and the alternatives considered.
2. No architecture decision may contradict the Architecture Principles or this constitution without an explicit, documented exception approved by the business owner.
3. Architecture is reviewed for continued fitness whenever a scope change is approved — architecture exists to serve the business domain, not the reverse.
4. Technology and tooling selection, whenever it occurs, must be justified against the Architecture Principles before adoption; it is never chosen by default or by convenience alone.

## Documentation Rules

1. This constitution is the top-level authority. All other documents (architecture docs, ADRs, backlog items, API docs) must be consistent with it or explicitly amend it.
2. Documentation is created deliberately, one document at a time, only when there is enough real information to write it truthfully — not scaffolded speculatively.
3. No document may assume information that hasn't been established (in this constitution, an approved ADR, or explicit direction). Unknowns are recorded as open questions, not filled with placeholders.
4. Every significant decision (scope, architecture, process) must live in a discoverable document — not only in conversation history.
5. Documents are living; when a decision changes, the relevant document is updated, not silently contradicted by a newer one.

## Documentation Governance

1. Documentation has an owner. The business owner approves foundational and structural documents; other documents may be maintained by whoever is accountable for the area they describe.
2. A change to an approved document is a deliberate, attributable act — not an incidental edit made in passing while doing other work.
3. Contradictions between documents are treated as defects. They are resolved by determining which document is authoritative — this constitution, unless explicitly amended — and correcting the other.
4. Documentation debt — missing, outdated, or contradictory documents — is tracked and addressed deliberately, not left to accumulate silently.

## Development Workflow

1. Business requirements are established before design, and design is established before implementation.
2. No source code is written until the relevant domain rules (starting with scheduling/booking) are documented.
3. Each unit of work should trace back to a Product Goal or a Success Criterion in this document. Work that doesn't trace back to either is out of scope until this document is amended.
4. Foundational documents are reviewed and approved by the user before being built upon.
5. Changes to scope, roles, or core principles require an explicit update to this constitution before downstream work proceeds.
6. Work progresses in sequence — business requirement, then domain rule, then architecture decision, then implementation. No stage is skipped or assumed complete.
7. Verification is part of the workflow, not a separate afterthought: nothing is considered delivered until it has been verified against the requirement, principle, or criterion it serves.

## Change Management

1. This constitution may be amended, but only explicitly and deliberately — never as a side effect of unrelated work.
2. A proposed amendment states what is changing, why, and which downstream documents or decisions are affected.
3. Amendments require explicit approval from the business owner before taking effect.
4. Approved amendments are reflected directly in this document. This constitution does not accumulate contradictory addenda elsewhere.
5. Scope changes follow the same discipline defined under Project Scope: they must be reflected here before being acted on elsewhere.

## Risk Management Principles

1. Risks are identified as early as possible — during business requirement discovery and architecture decisions, not only after an incident occurs.
2. Every significant risk has an accountable owner and a documented response: accept, mitigate, transfer, or avoid.
3. Risks to scheduling integrity, data protection, and user trust are treated as the highest-priority category, consistent with the Core Principles and Security Principles.
4. Unmanaged or unknown risk is itself treated as a risk. Where uncertainty exists, it is surfaced and discussed rather than silently carried forward.
5. Risk posture is reviewed whenever scope, architecture, or significant process changes are approved.

## AI Collaboration Rules

1. The AI assistant does not make assumptions about business requirements, scope, or user needs. When information is missing, it asks before proceeding.
2. The AI assistant does not write source code until the business and domain foundation (starting with this constitution) is approved.
3. The AI assistant creates only what is explicitly requested — no speculative scaffolding, extra documents, or unrequested abstractions.
4. The AI assistant surfaces conflicts between a new request and this constitution rather than silently resolving them.
5. The AI assistant treats this document as binding context for all future work in this project unless the user amends it.

## Definition of Done

A piece of work (a document, a decision, or later, a feature) is **done** when:
1. It is traceable to a goal, principle, or explicit instruction in this constitution.
2. It contains no unstated assumptions — open questions were asked and answered, not guessed.
3. It has been presented to the user and explicitly approved.
4. It does not contradict any other approved document; if it changes a prior decision, that document has been updated accordingly.
5. Nothing beyond what was explicitly requested was created.

---

*Status: Draft — pending user approval. No further documents or code are to be created until this constitution is approved.*
