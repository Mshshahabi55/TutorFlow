namespace TutorFlow.Modules.Identity.Infrastructure;

public static class RoleNames
{
    public const string Student = "Student";
    public const string Tutor = "Tutor";

    // Internal provisioning only, never self-service (ROLES_AND_PERMISSIONS.md) — unlike
    // Tutor (self-granted via BecomeTutor), nothing in this codebase ever assigns Admin to an
    // account. The role exists so AdminOnly policies and seed/ops scripts have a stable target;
    // actually granting it to a real account is an explicit out-of-band provisioning step.
    public const string Admin = "Admin";
}

internal static class RoleIds
{
    public static readonly Guid Student = new("8f14e45f-ceea-467e-bd7b-f7a1b1d5e4a1");
    public static readonly Guid Tutor = new("3c59dc04-8d53-459f-8b3a-a3b2b9c8e2f0");
    public static readonly Guid Admin = new("c7b1a6e2-4d9a-4b6a-9f2e-1a7c5e8b6d30");
}

public static class PolicyNames
{
    public const string StudentOnly = "StudentOnly";
    public const string TutorOnly = "TutorOnly";
    public const string AdminOnly = "AdminOnly";
}
