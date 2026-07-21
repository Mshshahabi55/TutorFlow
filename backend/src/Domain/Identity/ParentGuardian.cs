using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Domain.Identity;

// A Parent/Guardian may act on behalf of one or more Students, via a
// confirmed Relationship (PRODUCT_REQUIREMENTS.md IDR-1, IDR-3). Linked
// Students are not held here — Relationship is its own aggregate, referencing
// both parties by identity only (docs/database/DOMAIN_DATA_MODEL.md, Section 9:
// Aggregate Boundaries). No Domain Event is raised: only Tutor registration
// is an approved event (DOMAIN_MODEL.md: Domain Events).
public sealed class ParentGuardian : Account
{
    private ParentGuardian(AccountId id, EmailAddress email, PasswordHash passwordHash)
        : base(id, email, passwordHash)
    {
    }

    public static ParentGuardian Register(EmailAddress email, PasswordHash passwordHash) =>
        new(AccountId.New(), email, passwordHash);
}
