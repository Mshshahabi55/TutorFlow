namespace TutorFlow.Modules.Availability.Infrastructure;

// "Tutor" is the shared role-claim contract with the Identity module (owner of role grants).
// This module defines its own role/policy names rather than referencing Identity's assembly,
// per the module-boundary rule: Availability must not reference other business modules.
public static class RoleNames
{
    public const string Tutor = "Tutor";
}

public static class PolicyNames
{
    public const string TutorOnly = "Availability.TutorOnly";
}
