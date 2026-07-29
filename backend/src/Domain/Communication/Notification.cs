using TutorFlow.Domain.Common;
using TutorFlow.Domain.Communication.Events;
using TutorFlow.Domain.Communication.ValueObjects;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Domain.Communication;

public sealed class Notification : AggregateRoot<NotificationId>
{
    public const int MaxSummaryLength = 280;

    private Notification(
        NotificationId id,
        AccountId recipientId,
        NotificationType type,
        string summary,
        Guid? relatedEntityId,
        DateTime createdAtUtc) : base(id)
    {
        RecipientId = recipientId;
        Type = type;
        Summary = summary;
        RelatedEntityId = relatedEntityId;
        CreatedAtUtc = createdAtUtc;
    }

    public AccountId RecipientId { get; }

    public NotificationType Type { get; }

    // A short, pre-composed, honest sentence — never the raw content of
    // whatever triggered it (e.g. never a Message's own Body; see ADR-022's
    // Audit & privacy section for the same discipline applied to the audit
    // trail).
    public string Summary { get; }

    // Whatever the notification concerns (a SessionId, a ConversationId) —
    // for deep-linking only, no invariant of its own.
    public Guid? RelatedEntityId { get; }

    public DateTime CreatedAtUtc { get; }

    public DateTime? ReadAtUtc { get; private set; }

    public static Notification Create(
        AccountId recipientId,
        NotificationType type,
        string summary,
        Guid? relatedEntityId,
        DateTime nowUtc)
    {
        Guard.Against.Null(recipientId, nameof(recipientId));

        if (string.IsNullOrWhiteSpace(summary))
        {
            throw new ArgumentException("Notification summary cannot be empty.", nameof(summary));
        }

        var trimmed = summary.Trim();
        if (trimmed.Length > MaxSummaryLength)
        {
            trimmed = trimmed[..MaxSummaryLength];
        }

        var notification = new Notification(NotificationId.New(), recipientId, type, trimmed, relatedEntityId, nowUtc);
        notification.RaiseDomainEvent(new NotificationCreated(notification.Id, recipientId, type));
        return notification;
    }

    public void MarkRead(DateTime nowUtc)
    {
        if (ReadAtUtc is not null)
        {
            return;
        }

        ReadAtUtc = nowUtc;
    }
}
