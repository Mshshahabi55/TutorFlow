using TutorFlow.Domain.Communication;

namespace TutorFlow.Application.Communication.DTOs;

public sealed record MessageDto(
    Guid MessageId,
    Guid ConversationId,
    Guid SenderId,
    Guid RecipientId,
    string Body,
    DateTime SentAtUtc,
    DateTime? ReadAtUtc)
{
    public static MessageDto FromDomain(Message message) => new(
        message.Id.Value,
        message.ConversationId.Value,
        message.SenderId.Value,
        message.RecipientId.Value,
        message.Body,
        message.SentAtUtc,
        message.ReadAtUtc);
}
