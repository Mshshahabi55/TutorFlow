# TutorFlow — Final Architecture Review and Implementation Plan

## 1. Architectural Summary

TutorFlow should be treated as a modular monolith centered on one core mission: reliable scheduling and booking for tutoring sessions.

The architecture should remain intentionally narrow in v1 and preserve a clear separation between:
- Identity and relationships
- Scheduling and booking
- Discovery and oversight
- Cross-cutting concerns such as authentication, authorization, audit, validation, and error handling

The current codebase already reflects most of this structure well, especially in the backend layering and in the frontend feature organization.

## 2. Recommended Architectural Model

### 2.1 Core Bounded Contexts

1. Identity & Relationship
   - Responsible for accounts, roles, tutor approval/suspension, and parent-student relationships.
   - Owns the trust and access rules of the platform.

2. Scheduling & Booking
   - Responsible for availability slots, booking, session lifecycle, and slot-to-session consistency.
   - This is the highest-risk business area and must remain the most tightly governed part of the system.

### 2.2 Application Modules

3. Discovery
   - Read-oriented module for tutor search and filtering.
   - It should depend on the domain data exposed by Identity & Relationship and Scheduling & Booking.

4. Marketplace Oversight
   - Admin/staff operations such as approvals, conflict resolution, account oversight, and platform monitoring.
   - It should orchestrate existing capabilities rather than own independent domain state.

## 3. Architectural Principles to Preserve

- Separation of concerns
- Single source of truth for scheduling data
- Auditability for every state-changing action
- Least-privilege access by role
- Reliability over cleverness
- Evolvability without premature abstraction

## 4. Current Project Assessment

### Strengths
- The backend already follows a clean layered structure: Domain, Application, Infrastructure, Web.
- Domain entities for tutoring and scheduling are already modeled with explicit invariants.
- The booking rule is implemented at the domain boundary through the AvailabilitySlot aggregate, which is the correct architectural direction for preventing double-booking.
- The frontend is structured around feature-oriented folders and uses a modern React/Vite stack.

### Gaps to address
- The implementation should formalize authorization enforcement more strongly at the application boundary.
- Audit logging should be consistently required for all schedule-affecting actions.
- The domain should define and enforce a precise session-state transition model.
- A clear decision must be made on the aggregate boundary between Availability Slot and Session before further expansion.
- Admin account creation and admin-permission tiers should be decided explicitly.

## 5. Recommended Design Decisions

### 5.1 Deployment Shape

Recommendation: start with a modular monolith.

Why:
- It matches the current project size and maturity.
- It keeps implementation simple and reliable.
- It avoids premature service decomposition.

### 5.2 Persistence

Recommendation: use a relational database with strong consistency guarantees.

Why:
- The no-double-booking invariant requires robust transactional semantics.
- The current backend is already aligned with EF Core and PostgreSQL patterns.

### 5.3 Concurrency Model

Recommendation: enforce booking integrity through a transactional invariant at the persistence layer, supported by an optimistic or pessimistic strategy depending on the final aggregate design.

The important point is that booking must not rely on application-only checks alone.

### 5.4 Authentication and Authorization

Recommendation:
- Use role-based authorization as the baseline.
- Add relationship-based checks for parent/guardian actions.
- Enforce ownership rules at the application layer and protect sensitive routes through endpoint-level authorization.

### 5.5 Audit Strategy

Recommendation:
- Every mutation affecting availability or booking should generate an auditable record.
- Include actor identity, role, action, timestamp, and target entity reference.

## 6. Implementation Roadmap

### Phase 1 — Stabilization
- Finalize the core domain rules for session state transitions.
- Lock down the booking invariant and aggregate boundary.
- Strengthen authorization and audit handling.
- Add end-to-end tests for the booking workflow.

### Phase 2 — Core User Flows
- Complete student/parent/tutor registration and onboarding.
- Implement tutor approval and suspension flows.
- Implement discovery search and profile viewing.
- Implement booking, cancellation, and rescheduling flows.

### Phase 3 — Admin Operations
- Build admin dashboards for tutor oversight and booking visibility.
- Add conflict-resolution workflow.
- Add account-management capabilities.

### Phase 4 — Hardening and Release Readiness
- Add resilience and observability.
- Improve privacy handling and data minimization.
- Prepare deployment, migrations, and monitoring.

## 7. Recommended Priorities

Priority 1:
- Booking integrity
- Audit trail
- Role-based authorization

Priority 2:
- Tutor approval workflow
- Parent/guardian relationship model
- Session lifecycle rules

Priority 3:
- Discovery experience
- Admin oversight UI
- Operational dashboards

## 8. Key Open Questions Before Full Implementation

These should be answered explicitly before the next engineering sprint:
- What is the exact age threshold for adult vs. minor?
- What is the definitive invitation/confirmation flow for parent-student relationships?
- What are the notice-period rules for cancellation and rescheduling?
- Which role may mark a session as Completed or No-Show?
- What is the final aggregate boundary for Availability Slot and Session?
- How should admin accounts be created and tiered?

## 9. Final Recommendation

The project should proceed as a modular monolith with a strong domain-centered core, not as a distributed system. The current architecture direction is already correct; the next step is to formalize the remaining behavioral rules and strengthen the enforcement layers around booking, authorization, and auditing.
