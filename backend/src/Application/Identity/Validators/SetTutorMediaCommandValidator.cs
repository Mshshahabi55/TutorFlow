using TutorFlow.Application.Common;
using TutorFlow.Application.Identity.Commands;

namespace TutorFlow.Application.Identity.Validators;

public static class SetTutorMediaCommandValidator
{
    public static Result Validate(SetTutorMediaCommand command)
    {
        if (command.TutorId == Guid.Empty)
        {
            return Result.Failure(new Error(
                "SetTutorMediaCommand.TutorId.Empty",
                "Tutor id is required.",
                ErrorType.Domain));
        }

        return Result.Success();
    }
}
