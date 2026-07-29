using TutorFlow.Domain.Common;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Domain.Identity.Events;

// The invited party confirms the Relationship (PRODUCT_REQUIREMENTS.md IDR-4;
// DOMAIN_MODEL.md: Domain Events).
//
// InvitedByAccountId: additive, trailing, defaulted
// (docs/adr/ADR-022-communication-and-notifications-architecture.md) —
// Relationship already holds this on `this`, it just wasn't on the event.
// Lets a "Parent confirmed" Notification be sent to whichever party issued
// the original invitation.
public sealed record RelationshipConfirmed(
    RelationshipId RelationshipId,
    AccountId ParentGuardianId,
    AccountId StudentId,
    AccountId? InvitedByAccountId = null) : DomainEvent;
