using TutorFlow.Domain.Common;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Domain.Identity.Events;

// A Parent/Guardian or Student invites the other into a Relationship
// (PRODUCT_REQUIREMENTS.md IDR-4; DOMAIN_MODEL.md: Domain Events).
public sealed record RelationshipInvited(
    RelationshipId RelationshipId,
    AccountId ParentGuardianId,
    AccountId StudentId) : DomainEvent;
