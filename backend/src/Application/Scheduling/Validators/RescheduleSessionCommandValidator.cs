using TutorFlow.Application.Common;
using TutorFlow.Application.Scheduling.Commands;

namespace TutorFlow.Application.Scheduling.Validators;

// Structural validation only — see DeclareAvailabilityCommandValidator.
public static class RescheduleSessionCommandValidator
{
    public static Result Validate(RescheduleSessionCommand command)
    {
        if (command.SessionId == Guid.Empty)
        {
            return Result.Failure(new Error(
                "RescheduleSessionCommand.SessionId.Empty",
                "Session id is required.",
                ErrorType.Domain));
        }

        if (command.NewAvailabilitySlotId == Guid.Empty)
        {
            return Result.Failure(new Error(
                "RescheduleSessionCommand.NewAvailabilitySlotId.Empty",
                "New availability slot id is required.",
                ErrorType.Domain));
        }

        return Result.Success();
    }
}
