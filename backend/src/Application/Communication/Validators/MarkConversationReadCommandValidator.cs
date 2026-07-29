using TutorFlow.Application.Common;
using TutorFlow.Application.Communication.Commands;

namespace TutorFlow.Application.Communication.Validators;

public static class MarkConversationReadCommandValidator
{
    public static Result Validate(MarkConversationReadCommand command)
    {
        if (command.ConversationId == Guid.Empty)
        {
            return Result.Failure(new Error(
                "MarkConversationReadCommand.ConversationId.Empty", "Conversation id is required.", ErrorType.Domain));
        }

        return Result.Success();
    }
}
