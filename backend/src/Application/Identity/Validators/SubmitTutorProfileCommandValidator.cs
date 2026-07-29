using TutorFlow.Application.Common;
using TutorFlow.Application.Identity.Commands;

namespace TutorFlow.Application.Identity.Validators;

// Structural validation only — the actual "must have a Subject and an
// Hourly Rate" rule is Domain's own (Tutor.SubmitProfile), enforced by the
// handler (docs/adr/ADR-007-validation-strategy.md).
public static class SubmitTutorProfileCommandValidator
{
    public static Result Validate(SubmitTutorProfileCommand command)
    {
        if (command.TutorId == Guid.Empty)
        {
            return Result.Failure(new Error(
                "SubmitTutorProfileCommand.TutorId.Empty",
                "Tutor id is required.",
                ErrorType.Domain));
        }

        return Result.Success();
    }
}
