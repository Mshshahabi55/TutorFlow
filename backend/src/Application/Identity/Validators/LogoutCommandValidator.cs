using TutorFlow.Application.Common;
using TutorFlow.Application.Identity.Commands;

namespace TutorFlow.Application.Identity.Validators;

public static class LogoutCommandValidator
{
    public static Result Validate(LogoutCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Token))
        {
            return Result.Failure(new Error(
                "LogoutCommand.Token.Empty", "Token is required.", ErrorType.Domain));
        }

        return Result.Success();
    }
}
