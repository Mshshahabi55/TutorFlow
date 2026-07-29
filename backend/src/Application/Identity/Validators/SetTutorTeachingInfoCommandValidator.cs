using TutorFlow.Application.Common;
using TutorFlow.Application.Identity.Commands;

namespace TutorFlow.Application.Identity.Validators;

public static class SetTutorTeachingInfoCommandValidator
{
    public static Result Validate(SetTutorTeachingInfoCommand command)
    {
        if (command.TutorId == Guid.Empty)
        {
            return Result.Failure(new Error(
                "SetTutorTeachingInfoCommand.TutorId.Empty",
                "Tutor id is required.",
                ErrorType.Domain));
        }

        if (command.TutorSubjects?.Any(entry => string.IsNullOrWhiteSpace(entry.Subject)) == true)
        {
            return Result.Failure(new Error(
                "SetTutorTeachingInfoCommand.TutorSubjects.SubjectRequired",
                "Each declared subject must have a non-empty name.",
                ErrorType.Domain));
        }

        return Result.Success();
    }
}
