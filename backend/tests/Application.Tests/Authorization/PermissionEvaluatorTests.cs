using TutorFlow.Application.Authorization;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Tests.Authorization;

public class PermissionEvaluatorTests
{
    private readonly IPermissionEvaluator _evaluator = new PermissionEvaluator();

    [Theory]
    [InlineData(Role.Student, Permission.BookSession)]
    [InlineData(Role.Student, Permission.CancelSession)]
    [InlineData(Role.Student, Permission.RescheduleSession)]
    // ADR-003 Second Addendum, Correction 1 (2026-07-21): IDR-4 permits
    // either party to invite/confirm a Relationship, not Parent-Guardian
    // only.
    [InlineData(Role.Student, Permission.InviteRelationship)]
    [InlineData(Role.Student, Permission.ConfirmRelationship)]
    [InlineData(Role.Tutor, Permission.ManageTutorOffering)]
    [InlineData(Role.Tutor, Permission.DeclareAvailability)]
    [InlineData(Role.Tutor, Permission.CancelSession)]
    [InlineData(Role.Tutor, Permission.RescheduleSession)]
    [InlineData(Role.ParentGuardian, Permission.InviteRelationship)]
    [InlineData(Role.ParentGuardian, Permission.ConfirmRelationship)]
    [InlineData(Role.ParentGuardian, Permission.BookSession)]
    [InlineData(Role.ParentGuardian, Permission.CancelSession)]
    [InlineData(Role.ParentGuardian, Permission.RescheduleSession)]
    [InlineData(Role.AdminStaff, Permission.ApproveTutor)]
    [InlineData(Role.AdminStaff, Permission.SuspendTutor)]
    [InlineData(Role.AdminStaff, Permission.ViewAllSchedules)]
    [InlineData(Role.AdminStaff, Permission.ResolveBookingConflict)]
    [InlineData(Role.AdminStaff, Permission.ManageUserAccounts)]
    // ADR-003 Second Addendum, Decision 6 (2026-07-21).
    [InlineData(Role.AdminStaff, Permission.ViewAuditEntries)]
    [InlineData(Role.AdminStaff, Permission.CancelSession)]
    [InlineData(Role.AdminStaff, Permission.RescheduleSession)]
    // ADR-003 Addendum, Decision 1 (2026-07-21) — the coarse-grained
    // (role-only) half; the fine-grained "assigned to that Session" half is
    // not expressible here and is checked separately, downstream.
    [InlineData(Role.Tutor, Permission.CompleteSession)]
    [InlineData(Role.Tutor, Permission.MarkSessionNoShow)]
    [InlineData(Role.AdminStaff, Permission.CompleteSession)]
    [InlineData(Role.AdminStaff, Permission.MarkSessionNoShow)]
    public void HasPermission_grants_every_permission_ADR003_assigns_to_the_role(Role role, Permission permission)
    {
        Assert.True(_evaluator.HasPermission(role, permission));
    }

    [Theory]
    [InlineData(Role.Student, Permission.ApproveTutor)]
    [InlineData(Role.Student, Permission.ManageTutorOffering)]
    [InlineData(Role.Tutor, Permission.BookSession)]
    [InlineData(Role.Tutor, Permission.ApproveTutor)]
    [InlineData(Role.ParentGuardian, Permission.DeclareAvailability)]
    [InlineData(Role.AdminStaff, Permission.DeclareAvailability)]
    // ADR-003 Addendum, Decision 1: "Student and Parent/Guardian cannot
    // complete or mark No-Show" — explicit, not merely absent.
    [InlineData(Role.Student, Permission.CompleteSession)]
    [InlineData(Role.Student, Permission.MarkSessionNoShow)]
    [InlineData(Role.ParentGuardian, Permission.CompleteSession)]
    [InlineData(Role.ParentGuardian, Permission.MarkSessionNoShow)]
    // ADR-003 Second Addendum, Decision 6: audit trail read access is
    // Admin/Staff only.
    [InlineData(Role.Student, Permission.ViewAuditEntries)]
    [InlineData(Role.Tutor, Permission.ViewAuditEntries)]
    [InlineData(Role.ParentGuardian, Permission.ViewAuditEntries)]
    public void HasPermission_denies_a_permission_the_role_was_not_assigned(Role role, Permission permission)
    {
        Assert.False(_evaluator.HasPermission(role, permission));
    }

    [Fact]
    public void GetPermissions_returns_exactly_the_permissions_HasPermission_grants()
    {
        foreach (Role role in Enum.GetValues<Role>())
        {
            var granted = _evaluator.GetPermissions(role);
            foreach (Permission permission in Enum.GetValues<Permission>())
            {
                Assert.Equal(granted.Contains(permission), _evaluator.HasPermission(role, permission));
            }
        }
    }
}
