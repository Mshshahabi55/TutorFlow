namespace TutorFlow.Domain.Identity.ValueObjects;

// The closed set fixed in DOMAIN_MODEL.md: Value Objects / Enumerations —
// no other state exists (PRODUCT_REQUIREMENTS.md IDR-4).
public enum RelationshipStatus
{
    Invited = 0,
    Confirmed = 1
}
