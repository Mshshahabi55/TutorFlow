using TutorFlow.Application.Common;
using TutorFlow.Application.Scheduling.Commands;

namespace TutorFlow.Application.Scheduling.Validators;

// Structural validation only — see DeclareAvailabilityCommandValidator.
public static class BookSessionCommandValidator
{
    public static Result Validate(BookSessionCommand command)
    {
        if (command.AvailabilitySlotId == Guid.Empty)
        {
            return Result.Failure(new Error(
                "BookSessionCommand.AvailabilitySlotId.Empty",
                "Availability slot id is required.",
                ErrorType.Domain));
        }

        if (command.StudentId == Guid.Empty)
        {
            return Result.Failure(new Error(
                "BookSessionCommand.StudentId.Empty",
                "Student id is required.",
                ErrorType.Domain));
        }

        if (command.ParentGuardianId.HasValue && command.ParentGuardianId.Value == Guid.Empty)
        {
            return Result.Failure(new Error(
                "BookSessionCommand.ParentGuardianId.Empty",
                "Parent/Guardian id, when provided, cannot be empty.",
                ErrorType.Domain));
        }

        return Result.Success();
    }
}
