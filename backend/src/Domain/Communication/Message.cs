using TutorFlow.Domain.Common;
using TutorFlow.Domain.Communication.Events;
using TutorFlow.Domain.Communication.ValueObjects;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Domain.Communication;

public sealed class Message : AggregateRoot<MessageId>
{
    public const int MaxBodyLength = 4000;

    private Message(
        MessageId id,
        ConversationId conversationId,
        AccountId senderId,
        AccountId recipientId,
        string body,
        DateTime sentAtUtc) : base(id)
    {
        ConversationId = conversationId;
        SenderId = senderId;
        RecipientId = recipientId;
        Body = body;
        SentAtUtc = sentAtUtc;
    }

    public ConversationId ConversationId { get; }

    public AccountId SenderId { get; }

    public AccountId RecipientId { get; }

    // Plain text only — no HTML/markup, no attachments (not requested, not
    // built). Length-limited defensively, the same "structural validation
    // in the Domain aggregate" pattern every other Domain method here uses.
    public string Body { get; }

    public DateTime SentAtUtc { get; }

    // The only honest "delivery state" this request/response system has —
    // see ADR-022's Message aggregate section for why a fabricated
    // "delivered" tick is not added.
    public DateTime? ReadAtUtc { get; private set; }

    public static Message Send(
        ConversationId conversationId,
        AccountId senderId,
        AccountId recipientId,
        string body,
        DateTime nowUtc)
    {
        Guard.Against.Null(conversationId, nameof(conversationId));
        Guard.Against.Null(senderId, nameof(senderId));
        Guard.Against.Null(recipientId, nameof(recipientId));

        if (string.IsNullOrWhiteSpace(body))
        {
            throw new ArgumentException("Message body cannot be empty.", nameof(body));
        }

        var trimmed = body.Trim();
        if (trimmed.Length > MaxBodyLength)
        {
            throw new ArgumentException($"Message body cannot exceed {MaxBodyLength} characters.", nameof(body));
        }

        var message = new Message(MessageId.New(), conversationId, senderId, recipientId, trimmed, nowUtc);
        message.RaiseDomainEvent(new MessageSent(message.Id, conversationId, senderId, recipientId));
        return message;
    }

    public void MarkRead(DateTime nowUtc)
    {
        // Idempotent — re-marking an already-read Message is a no-op, not
        // an error, since "mark this conversation read" naturally revisits
        // every message regardless of its current state.
        if (ReadAtUtc is not null)
        {
            return;
        }

        ReadAtUtc = nowUtc;
    }
}
