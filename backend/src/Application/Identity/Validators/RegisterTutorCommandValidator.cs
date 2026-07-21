using TutorFlow.Application.Common;
using TutorFlow.Application.Identity.Commands;

namespace TutorFlow.Application.Identity.Validators;

public static class RegisterTutorCommandValidator
{
    public static Result Validate(RegisterTutorCommand command)
    {
        var emailError = CredentialValidation.ValidateEmail(command.Email, nameof(RegisterTutorCommand));
        if (emailError is not null)
        {
            return Result.Failure(emailError);
        }

        var passwordError = CredentialValidation.ValidatePassword(command.Password, nameof(RegisterTutorCommand));
        if (passwordError is not null)
        {
            return Result.Failure(passwordError);
        }

        return Result.Success();
    }
}
