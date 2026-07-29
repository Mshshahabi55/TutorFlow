using TutorFlow.Domain.Communication;
using TutorFlow.Domain.Communication.ValueObjects;

namespace TutorFlow.Application.Communication.DTOs;

public sealed record NotificationDto(
    Guid NotificationId,
    NotificationType Type,
    string Summary,
    Guid? RelatedEntityId,
    DateTime CreatedAtUtc,
    DateTime? ReadAtUtc)
{
    public static NotificationDto FromDomain(Notification notification) => new(
        notification.Id.Value,
        notification.Type,
        notification.Summary,
        notification.RelatedEntityId,
        notification.CreatedAtUtc,
        notification.ReadAtUtc);
}
