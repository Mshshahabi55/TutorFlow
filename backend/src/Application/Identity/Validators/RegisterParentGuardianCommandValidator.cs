using TutorFlow.Application.Common;
using TutorFlow.Application.Identity.Commands;

namespace TutorFlow.Application.Identity.Validators;

public static class RegisterParentGuardianCommandValidator
{
    public static Result Validate(RegisterParentGuardianCommand command)
    {
        var emailError = CredentialValidation.ValidateEmail(command.Email, nameof(RegisterParentGuardianCommand));
        if (emailError is not null)
        {
            return Result.Failure(emailError);
        }

        var passwordError = CredentialValidation.ValidatePassword(command.Password, nameof(RegisterParentGuardianCommand));
        if (passwordError is not null)
        {
            return Result.Failure(passwordError);
        }

        return Result.Success();
    }
}
