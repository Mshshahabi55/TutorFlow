using TutorFlow.Application.Common;
using TutorFlow.Application.Scheduling.Commands;

namespace TutorFlow.Application.Scheduling.Validators;

// Structural validation only — see DeclareAvailabilityCommandValidator.
public static class MarkSessionNoShowCommandValidator
{
    public static Result Validate(MarkSessionNoShowCommand command)
    {
        if (command.SessionId == Guid.Empty)
        {
            return Result.Failure(new Error(
                "MarkSessionNoShowCommand.SessionId.Empty",
                "Session id is required.",
                ErrorType.Domain));
        }

        return Result.Success();
    }
}
