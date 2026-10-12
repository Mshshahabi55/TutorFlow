namespace TutorFlow.Modules.Tutors.Infrastructure;

// "Tutor"/"Admin" are shared role-claim contracts with the Identity module (owner of role
// grants). This module defines its own role/policy names rather than referencing Identity's
// assembly, per the module-boundary rule: Tutors must not reference other business modules.
public static class RoleNames
{
    public const string Tutor = "Tutor";
    public const string Admin = "Admin";
}

public static class PolicyNames
{
    public const string TutorOnly = "Tutors.TutorOnly";

    // Composed Admin (ADR-0008): Tutors owns its own admin-facing endpoints rather than a
    // 15th "Admin" module reaching in. Gated on Identity's Admin role via the shared claim
    // contract above, exactly like TutorOnly is gated on Tutor.
    public const string AdminOnly = "Tutors.AdminOnly";
}
