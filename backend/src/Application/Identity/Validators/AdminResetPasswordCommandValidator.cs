using TutorFlow.Application.Common;
using TutorFlow.Application.Identity.Commands;

namespace TutorFlow.Application.Identity.Validators;

public static class AdminResetPasswordCommandValidator
{
    public static Result Validate(AdminResetPasswordCommand command)
    {
        if (command.AccountId == Guid.Empty)
        {
            return Result.Failure(new Error(
                "AdminResetPasswordCommand.AccountId.Empty", "Account id is required.", ErrorType.Domain));
        }

        var passwordError = CredentialValidation.ValidatePassword(command.NewPassword, nameof(AdminResetPasswordCommand));
        if (passwordError is not null)
        {
            return Result.Failure(passwordError);
        }

        return Result.Success();
    }
}
