namespace TutorFlow.Modules.Payments.Infrastructure;

// Deliberate duplication of Identity's role-claim contract, matching every other module's
// identical convention (Bookings/Tutors/Availability) — not consolidated here (BOOKINGS_SPEC.md
// §10 already flags this as a compounding, not-yet-addressed concern).
public static class RoleNames
{
    public const string Student = "Student";
}

public static class PolicyNames
{
    public const string StudentOnly = "Payments.StudentOnly";
}
