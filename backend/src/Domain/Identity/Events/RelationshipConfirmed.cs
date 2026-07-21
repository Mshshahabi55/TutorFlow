using TutorFlow.Domain.Common;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Domain.Identity.Events;

// The invited party confirms the Relationship (PRODUCT_REQUIREMENTS.md IDR-4;
// DOMAIN_MODEL.md: Domain Events).
public sealed record RelationshipConfirmed(
    RelationshipId RelationshipId,
    AccountId ParentGuardianId,
    AccountId StudentId) : DomainEvent;
