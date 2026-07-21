using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Domain.Identity;

// Operates and supports the marketplace (PROJECT_CONSTITUTION.md: Project
// Scope). Unlike Student, Tutor, and Parent/Guardian, no approved document
// establishes how an Admin/Staff account is created — self-registration
// (IDR-1) is only established for the other three roles
// (DOMAIN_MODEL.md Open Question 15). "Create" is used, deliberately, instead
// of "Register" to avoid presuming that unresolved mechanism. No Domain
// Event is raised: none is approved for Admin/Staff account creation
// (DOMAIN_MODEL.md: Domain Events).
public sealed class AdminStaff : Account
{
    private AdminStaff(AccountId id, EmailAddress email, PasswordHash passwordHash)
        : base(id, email, passwordHash)
    {
    }

    // Provisioned only by the business owner or an already-authenticated
    // Admin/Staff member — never self-service (docs/adr/ADR-017-authentication-mechanism-decision.md).
    public static AdminStaff Create(EmailAddress email, PasswordHash passwordHash) =>
        new(AccountId.New(), email, passwordHash);
}
