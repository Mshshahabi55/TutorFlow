using TutorFlow.Application.Common;
using TutorFlow.Application.Identity.Commands;

namespace TutorFlow.Application.Identity.Validators;

public static class RegisterStudentCommandValidator
{
    public static Result Validate(RegisterStudentCommand command)
    {
        var emailError = CredentialValidation.ValidateEmail(command.Email, nameof(RegisterStudentCommand));
        if (emailError is not null)
        {
            return Result.Failure(emailError);
        }

        var passwordError = CredentialValidation.ValidatePassword(command.Password, nameof(RegisterStudentCommand));
        if (passwordError is not null)
        {
            return Result.Failure(passwordError);
        }

        return Result.Success();
    }
}
