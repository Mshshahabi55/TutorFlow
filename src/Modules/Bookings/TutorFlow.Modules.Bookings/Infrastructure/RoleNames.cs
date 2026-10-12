namespace TutorFlow.Modules.Bookings.Infrastructure;

// "Student"/"Tutor" are the shared role-claim contract with the Identity module (owner of role
// grants). This module defines its own role/policy names rather than referencing Identity's
// assembly, per the module-boundary rule — matching Identity/Tutors/Availability's identical,
// deliberate duplication (BOOKINGS_SPEC.md §10 flags this as a compounding, not-yet-addressed
// concern, out of scope to consolidate here).
public static class RoleNames
{
    public const string Student = "Student";
    public const string Tutor = "Tutor";
}

public static class PolicyNames
{
    public const string StudentOnly = "Bookings.StudentOnly";
    public const string TutorOnly = "Bookings.TutorOnly";
    public const string StudentOrTutorOnly = "Bookings.StudentOrTutorOnly";
}
