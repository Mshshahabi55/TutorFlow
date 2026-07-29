namespace TutorFlow.Application.Communication.Commands;

public sealed record SendMessageCommand(Guid ConversationId, string Body);
