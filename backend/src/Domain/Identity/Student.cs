using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Domain.Identity;

// An adult Student books independently; a minor Student requires an
// associated Parent/Guardian Relationship (PRODUCT_REQUIREMENTS.md IDR-5,
// IDR-6). How "minor" is determined (e.g. an age threshold) is not
// established (DOMAIN_MODEL.md Open Question 1) — that fact is supplied at
// registration rather than computed here. No Domain Event is raised: only
// Tutor registration is an approved event (DOMAIN_MODEL.md: Domain Events).
public sealed class Student : Account
{
    private Student(AccountId id, EmailAddress email, PasswordHash passwordHash, bool isMinor)
        : base(id, email, passwordHash) => IsMinor = isMinor;

    public bool IsMinor { get; }

    public static Student Register(EmailAddress email, PasswordHash passwordHash, bool isMinor) =>
        new(AccountId.New(), email, passwordHash, isMinor);
}
