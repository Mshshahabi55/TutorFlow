using TutorFlow.Application.Common;
using TutorFlow.Application.Scheduling.Commands;

namespace TutorFlow.Application.Scheduling.Validators;

// Structural validation only — see DeclareAvailabilityCommandValidator.
public static class CancelSessionCommandValidator
{
    public static Result Validate(CancelSessionCommand command)
    {
        if (command.SessionId == Guid.Empty)
        {
            return Result.Failure(new Error(
                "CancelSessionCommand.SessionId.Empty",
                "Session id is required.",
                ErrorType.Domain));
        }

        return Result.Success();
    }
}
