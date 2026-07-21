using TutorFlow.Application.Common;
using TutorFlow.Application.Identity.Commands;

namespace TutorFlow.Application.Identity.Validators;

// Structural validation only — the actual positivity rule is Domain's own
// (HourlyRate.Of), enforced by the handler (docs/adr/ADR-007-validation-strategy.md).
public static class SetTutorHourlyRateCommandValidator
{
    public static Result Validate(SetTutorHourlyRateCommand command)
    {
        if (command.TutorId == Guid.Empty)
        {
            return Result.Failure(new Error(
                "SetTutorHourlyRateCommand.TutorId.Empty",
                "Tutor id is required.",
                ErrorType.Domain));
        }

        return Result.Success();
    }
}
