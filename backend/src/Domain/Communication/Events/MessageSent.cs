using TutorFlow.Domain.Common;
using TutorFlow.Domain.Communication.ValueObjects;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Domain.Communication.Events;

// A Message is sent within a Conversation
// (docs/adr/ADR-022-communication-and-notifications-architecture.md).
// Carries RecipientId directly (denormalized off the Conversation at send
// time) so a listener creating a Notification never needs to re-load the
// Conversation just to know who to notify.
public sealed record MessageSent(
    MessageId MessageId,
    ConversationId ConversationId,
    AccountId SenderId,
    AccountId RecipientId) : DomainEvent;
