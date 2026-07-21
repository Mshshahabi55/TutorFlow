namespace TutorFlow.Application.Authorization;

// Every named permission this Permission Model recognizes, traced directly
// to docs/adr/ADR-003-authentication-and-authorization.md's Permission Model
// table and the approved Functional Requirements it cites — none invented
// beyond them (Launch Preparation, Priority 2, WP2).
//
// Deliberately NOT included, and not silently assumed:
// - No permission exists for GET /tutors/{id}, GET /students/{id}, a
//   Tutor's or Student's own schedule view, GET /relationships/{id}, etc.
//   ADR-003's Permission Model table names exactly one read action
//   (ViewAllSchedules, ADM-3); no approved document specifies read-scoping
//   for any other query endpoint. This is a real, open gap this WP
//   surfaces rather than invents an answer for — see the WP2 report.
// - No permission exists for Discovery/search: ADR-003's own Cross-Context
//   Authorization Rules state Discovery "performs no authorization-gated
//   mutation; only read access to discoverable Tutor data applies" — this
//   resolves the question rather than leaving it open.
// - No permission exists for self-registration (RegisterTutor/Student/
//   ParentGuardian) — registration is how an identity is created in the
//   first place; it cannot itself require a role-based permission.
public enum Permission
{
    // Identity & Relationship — Tutor's own offering
    // (PRODUCT_REQUIREMENTS.md SCH-3; ADR-003 Permission Model, Tutor row).
    ManageTutorOffering,

    // Scheduling & Booking — declaring an Availability Slot
    // (PRODUCT_REQUIREMENTS.md SCH-3, SCH-5; ADR-003 Permission Model, Tutor row).
    DeclareAvailability,

    // Scheduling & Booking (PRODUCT_REQUIREMENTS.md SCH-5; ADR-003 Permission
    // Model, Student/Parent-Guardian rows).
    BookSession,

    // Scheduling & Booking (PRODUCT_REQUIREMENTS.md SCH-7; ADR-003 Permission
    // Model, Student/Tutor/Parent-Guardian/Admin-Staff rows — every role
    // shares this one).
    CancelSession,
    RescheduleSession,

    // Scheduling & Booking. Granted to Tutor and AdminStaff only, per
    // ADR-003's Addendum, Decision 1 (2026-07-21) — resolving what was
    // DOMAIN_MODEL.md Open Question 6. This enum value expresses only the
    // coarse-grained (role-only) half of that decision; the fine-grained
    // half ("the Tutor assigned to *that* Session") belongs in Scheduling &
    // Booking's own Application-layer handler, not here.
    CompleteSession,
    MarkSessionNoShow,

    // Identity & Relationship (PRODUCT_REQUIREMENTS.md IDR-3, IDR-4).
    // Granted to both Student and ParentGuardian, per ADR-003's Second
    // Addendum, Correction 1 (2026-07-21) — IDR-4 permits either party to
    // invite, with the other confirming; ADR-003's original Permission
    // Model table named only the Parent-Guardian row, a documentation
    // defect corrected there, not a new business decision.
    InviteRelationship,
    ConfirmRelationship,

    // Identity & Relationship — Admin/Staff (PRODUCT_REQUIREMENTS.md ADM-1,
    // ADM-2; ADR-003 Permission Model, Admin/Staff row).
    ApproveTutor,
    SuspendTutor,

    // Marketplace Oversight (PRODUCT_REQUIREMENTS.md ADM-3; ADR-003
    // Permission Model, Admin/Staff row) — the one read action ADR-003
    // itself names.
    ViewAllSchedules,

    // Marketplace Oversight (PRODUCT_REQUIREMENTS.md ADM-4; ADR-003
    // Permission Model, Admin/Staff row). No backend capability implements
    // this yet (RFC-002, pending) — the permission is still defined here
    // since ADR-003 explicitly names it as a permitted action; nothing
    // currently checks it.
    ResolveBookingConflict,

    // Identity & Relationship (PRODUCT_REQUIREMENTS.md ADM-5; ADR-003
    // Permission Model, Admin/Staff row: "manage user accounts"). Mapped to
    // the existing administrator password-reset capability
    // (docs/adr/ADR-017-authentication-mechanism-decision.md) as the one
    // account-management action currently implemented under ADM-5 — not an
    // invented permission, a sourced mapping to an already-approved one.
    ManageUserAccounts,

    // Audit trail read access, per ADR-003's Second Addendum, Decision 6
    // (2026-07-21, WP4 Priority 2): GET /audit-entries is Admin/Staff only.
    // A dedicated value rather than reusing ManageUserAccounts, since the
    // two are distinct capabilities (managing accounts vs. reading audit
    // history) that Decision 6 and its own resolution deliberately kept
    // separate — not an invented permission, a named mapping of an
    // already-ratified decision.
    ViewAuditEntries,
}
