namespace TutorFlow.Modules.Messaging.Domain;

// ADR-0030: SenderUserId is set exactly once, at creation, directly from the authenticated
// caller — never from a request body field (SendMessage.Command carries no sender field at all).
// SequenceNumber is a PostgreSQL-generated IDENTITY column (see MessageConfiguration), never an
// application-computed MAX()+1 — that would race under concurrent inserts. Id stays an opaque
// Guid (this codebase's universal public-identifier convention); SequenceNumber is the ordering/
// pagination cursor only, never exposed as "the" message identifier.
public sealed class Message
{
    private Message()
    {
    }

    public Guid Id { get; private init; }

    public Guid ConversationId { get; private init; }

    public Guid SenderUserId { get; private init; }

    public string Body { get; private init; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private init; }

    public long SequenceNumber { get; private init; }

    public static Message Create(Guid conversationId, Guid senderUserId, string body, TimeProvider clock) => new()
    {
        Id = Guid.NewGuid(),
        ConversationId = conversationId,
        SenderUserId = senderUserId,
        Body = body,
        CreatedAtUtc = clock.GetUtcNow()
    };
}
