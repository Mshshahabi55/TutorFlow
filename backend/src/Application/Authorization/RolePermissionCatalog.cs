using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Authorization;

// The Role -> Permission[] mapping ("permission registration"), fixed at
// compile time. ADR-003 describes a closed, four-role model with no mention
// of runtime-configurable permissions anywhere — static, in-code
// registration is the correct representation of that, not a database table
// (Launch Preparation, Priority 2, WP2: "If permissions are static
// configuration: Do NOT create database tables").
//
// This is the single source of truth for "which role has which permission."
// Nothing outside this file should hard-code that mapping; every future
// authorization check (WP3 onward) must consume IPermissionEvaluator rather
// than re-deriving this table.
internal static class RolePermissionCatalog
{
    private static readonly IReadOnlyDictionary<Role, IReadOnlyCollection<Permission>> RolePermissions =
        new Dictionary<Role, IReadOnlyCollection<Permission>>
        {
            [Role.Student] =
            [
                Permission.BookSession,
                Permission.CancelSession,
                Permission.RescheduleSession,
                Permission.InviteRelationship,
                Permission.ConfirmRelationship,
                Permission.UseMessaging,
            ],
            [Role.Tutor] =
            [
                Permission.ManageTutorOffering,
                Permission.DeclareAvailability,
                Permission.CancelSession,
                Permission.RescheduleSession,
                Permission.CompleteSession,
                Permission.MarkSessionNoShow,
                Permission.UseMessaging,
                Permission.ManageMeetings,
            ],
            [Role.ParentGuardian] =
            [
                Permission.InviteRelationship,
                Permission.ConfirmRelationship,
                Permission.BookSession,
                Permission.CancelSession,
                Permission.RescheduleSession,
                Permission.UseMessaging,
            ],
            // Modeled as one flat role, matching ADR-003's Permission Model
            // table exactly — whether Admin/Staff has internal permission
            // tiers is an explicitly carried-forward Open Question
            // (DOMAIN_MODEL.md Open Question 12), not resolved or invented
            // here.
            [Role.AdminStaff] =
            [
                Permission.ApproveTutor,
                Permission.SuspendTutor,
                Permission.ViewAllSchedules,
                Permission.ResolveBookingConflict,
                Permission.ManageUserAccounts,
                Permission.CancelSession,
                Permission.RescheduleSession,
                Permission.CompleteSession,
                Permission.MarkSessionNoShow,
                Permission.ViewAuditEntries,
                Permission.UseMessaging,
            ],
        };

    // CompleteSession/MarkSessionNoShow: granted to Tutor and AdminStaff only,
    // per ADR-003's Addendum, Decision 1 (2026-07-21) — resolving the
    // previously-open DOMAIN_MODEL.md Open Question 6. This is the
    // coarse-grained (role-only) half of that decision; the fine-grained
    // half ("the Tutor assigned to *that* Session") is not expressible here
    // and is not attempted here — it belongs in Scheduling & Booking's own
    // Application-layer handler, per ADR-003's Resource Ownership rules.
    //
    // InviteRelationship/ConfirmRelationship: granted to both Student and
    // ParentGuardian, per ADR-003's Second Addendum, Correction 1
    // (2026-07-21) — the original table granted these to ParentGuardian
    // only, contradicting PRODUCT_REQUIREMENTS.md IDR-4 ("issued by one
    // party and confirmation by the other"), a documentation defect
    // corrected there, not a new business decision. The fine-grained rule
    // that the confirming party must be the *other* party to that specific
    // invitation is not expressible here and belongs in Identity &
    // Relationship's own Application-layer handler.
    //
    // ViewAuditEntries: granted to AdminStaff only, per ADR-003's Second
    // Addendum, Decision 6 (2026-07-21) — coarse-grained, no resource-instance
    // qualifier, since the permission is inherently platform-wide.
    public static IReadOnlyCollection<Permission> GetPermissions(Role role) =>
        RolePermissions.TryGetValue(role, out var permissions) ? permissions : [];
}
