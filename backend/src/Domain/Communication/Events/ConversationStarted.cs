using TutorFlow.Domain.Common;
using TutorFlow.Domain.Communication.ValueObjects;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Domain.Communication.Events;

// A Conversation is started between two participants
// (docs/adr/ADR-022-communication-and-notifications-architecture.md).
public sealed record ConversationStarted(
    ConversationId ConversationId,
    AccountId ParticipantAId,
    AccountId ParticipantBId) : DomainEvent;
