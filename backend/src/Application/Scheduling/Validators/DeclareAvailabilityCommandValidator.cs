using TutorFlow.Application.Common;
using TutorFlow.Application.Scheduling.Commands;

namespace TutorFlow.Application.Scheduling.Validators;

// Structural validation only — null/empty/unset/default. Never a business
// rule (e.g. overlap, double-booking) — that stays in Domain
// (docs/adr/ADR-007-validation-strategy.md).
public static class DeclareAvailabilityCommandValidator
{
    public static Result Validate(DeclareAvailabilityCommand command)
    {
        if (command.TutorId == Guid.Empty)
        {
            return Result.Failure(new Error(
                "DeclareAvailabilityCommand.TutorId.Empty",
                "Tutor id is required.",
                ErrorType.Domain));
        }

        if (command.StartTimeUtc == default)
        {
            return Result.Failure(new Error(
                "DeclareAvailabilityCommand.StartTimeUtc.Unset",
                "Start time is required.",
                ErrorType.Domain));
        }

        if (command.Duration == default)
        {
            return Result.Failure(new Error(
                "DeclareAvailabilityCommand.Duration.Unset",
                "Duration is required.",
                ErrorType.Domain));
        }

        return Result.Success();
    }
}
