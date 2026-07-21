using TutorFlow.Application.Common;
using TutorFlow.Application.Identity.Commands;

namespace TutorFlow.Application.Identity.Validators;

public static class SetTutorOfferedDurationsCommandValidator
{
    public static Result Validate(SetTutorOfferedDurationsCommand command)
    {
        if (command.TutorId == Guid.Empty)
        {
            return Result.Failure(new Error(
                "SetTutorOfferedDurationsCommand.TutorId.Empty",
                "Tutor id is required.",
                ErrorType.Domain));
        }

        if (command.Durations is null || command.Durations.Count == 0)
        {
            return Result.Failure(new Error(
                "SetTutorOfferedDurationsCommand.Durations.Empty",
                "At least one offered duration is required.",
                ErrorType.Domain));
        }

        return Result.Success();
    }
}
