using TutorFlow.Application.Common;
using TutorFlow.Application.Scheduling.Commands;

namespace TutorFlow.Application.Scheduling.Validators;

// Structural validation only — see DeclareAvailabilityCommandValidator.
public static class CompleteSessionCommandValidator
{
    public static Result Validate(CompleteSessionCommand command)
    {
        if (command.SessionId == Guid.Empty)
        {
            return Result.Failure(new Error(
                "CompleteSessionCommand.SessionId.Empty",
                "Session id is required.",
                ErrorType.Domain));
        }

        return Result.Success();
    }
}
