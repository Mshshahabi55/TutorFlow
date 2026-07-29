using TutorFlow.Application.Common;
using TutorFlow.Application.Communication.Commands;
using TutorFlow.Domain.Communication;

namespace TutorFlow.Application.Communication.Validators;

public static class SendMessageCommandValidator
{
    public static Result Validate(SendMessageCommand command)
    {
        if (command.ConversationId == Guid.Empty)
        {
            return Result.Failure(new Error(
                "SendMessageCommand.ConversationId.Empty", "Conversation id is required.", ErrorType.Domain));
        }

        if (string.IsNullOrWhiteSpace(command.Body))
        {
            return Result.Failure(new Error(
                "SendMessageCommand.Body.Empty", "Message body is required.", ErrorType.Domain));
        }

        if (command.Body.Trim().Length > Message.MaxBodyLength)
        {
            return Result.Failure(new Error(
                "SendMessageCommand.Body.TooLong",
                $"Message body cannot exceed {Message.MaxBodyLength} characters.",
                ErrorType.Domain));
        }

        return Result.Success();
    }
}
