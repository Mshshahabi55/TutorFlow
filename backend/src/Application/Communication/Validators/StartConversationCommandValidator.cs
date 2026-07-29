using TutorFlow.Application.Common;
using TutorFlow.Application.Communication.Commands;

namespace TutorFlow.Application.Communication.Validators;

public static class StartConversationCommandValidator
{
    public static Result Validate(StartConversationCommand command)
    {
        if (command.TargetAccountId == Guid.Empty)
        {
            return Result.Failure(new Error(
                "StartConversationCommand.TargetAccountId.Empty", "Target account id is required.", ErrorType.Domain));
        }

        return Result.Success();
    }
}
