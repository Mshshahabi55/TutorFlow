namespace TutorFlow.Modules.Messaging.Infrastructure;

// "Student"/"Tutor" are the shared role-claim contract with the Identity module (owner of role
// grants). This module defines its own role/policy names rather than referencing Identity's
// assembly, per the module-boundary rule — matching Bookings/Reviews/Availability's identical,
// deliberate duplication (BOOKINGS_SPEC.md §10).
public static class RoleNames
{
    public const string Student = "Student";
    public const string Tutor = "Tutor";
}

public static class PolicyNames
{
    public const string StudentOnly = "Messaging.StudentOnly";
    public const string StudentOrTutorOnly = "Messaging.StudentOrTutorOnly";
}
