using TutorFlow.Domain.Common;
using TutorFlow.Domain.Communication.Events;
using TutorFlow.Domain.Communication.ValueObjects;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Domain.Communication;

// Two independent aggregate roots (Conversation, Message) rather than
// Message as a child collection — the same small-aggregate reasoning
// ADR-015 already ratified for AvailabilitySlot/Session: an ever-growing
// message list loaded on every mutation is exactly the aggregate-sizing
// problem that heuristic exists to avoid
// (docs/adr/ADR-022-communication-and-notifications-architecture.md).
public sealed class Conversation : AggregateRoot<ConversationId>
{
    private Conversation(
        ConversationId id,
        AccountId participantAId,
        AccountId participantBId,
        DateTime createdAtUtc) : base(id)
    {
        ParticipantAId = participantAId;
        ParticipantBId = participantBId;
        CreatedAtUtc = createdAtUtc;
    }

    public AccountId ParticipantAId { get; }

    public AccountId ParticipantBId { get; }

    public DateTime CreatedAtUtc { get; }

    public DateTime? LastMessageAtUtc { get; private set; }

    public static Conversation Start(AccountId participantAId, AccountId participantBId, DateTime nowUtc)
    {
        Guard.Against.Null(participantAId, nameof(participantAId));
        Guard.Against.Null(participantBId, nameof(participantBId));

        if (participantAId.Value == participantBId.Value)
        {
            throw new InvalidOperationException("A Conversation must have two distinct participants.");
        }

        var conversation = new Conversation(ConversationId.New(), participantAId, participantBId, nowUtc);
        conversation.RaiseDomainEvent(new ConversationStarted(conversation.Id, participantAId, participantBId));
        return conversation;
    }

    public bool HasParticipant(AccountId accountId) =>
        ParticipantAId.Value == accountId.Value || ParticipantBId.Value == accountId.Value;

    // Only ever called with a participant's own id (HasParticipant already
    // verified by the caller) — a non-participant has no "other" side here.
    public AccountId OtherParticipant(AccountId accountId) =>
        ParticipantAId.Value == accountId.Value ? ParticipantBId : ParticipantAId;

    // Called by the Application handler after a Message is created against
    // this Conversation, in the same transaction — mirrors how
    // BookSessionCommandHandler coordinates AvailabilitySlot and Session as
    // two aggregates touched once each, not one aggregate reaching into
    // the other.
    public void RecordActivity(DateTime nowUtc) => LastMessageAtUtc = nowUtc;
}
