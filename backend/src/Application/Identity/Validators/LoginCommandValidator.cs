using TutorFlow.Application.Common;
using TutorFlow.Application.Identity.Commands;

namespace TutorFlow.Application.Identity.Validators;

public static class LoginCommandValidator
{
    public static Result Validate(LoginCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Email))
        {
            return Result.Failure(new Error(
                "LoginCommand.Email.Empty", "Email is required.", ErrorType.Domain));
        }

        if (string.IsNullOrWhiteSpace(command.Password))
        {
            return Result.Failure(new Error(
                "LoginCommand.Password.Empty", "Password is required.", ErrorType.Domain));
        }

        return Result.Success();
    }
}
