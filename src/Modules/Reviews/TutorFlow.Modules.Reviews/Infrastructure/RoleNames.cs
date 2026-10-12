namespace TutorFlow.Modules.Reviews.Infrastructure;

// "Student" is the shared role-claim contract with the Identity module (owner of role grants).
// This module defines its own role/policy names rather than referencing Identity's assembly, per
// the module-boundary rule — matching Identity/Tutors/Availability/Bookings/Lessons' identical,
// deliberate duplication (BOOKINGS_SPEC.md §10).
public static class RoleNames
{
    public const string Student = "Student";
}

public static class PolicyNames
{
    public const string StudentOnly = "Reviews.StudentOnly";
}
