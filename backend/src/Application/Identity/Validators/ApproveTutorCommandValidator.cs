using TutorFlow.Application.Common;
using TutorFlow.Application.Identity.Commands;

namespace TutorFlow.Application.Identity.Validators;

// Structural validation only — null/empty/format. Never a business rule
// (e.g. approval state) — that stays in Domain (docs/adr/ADR-007-validation-strategy.md).
public static class ApproveTutorCommandValidator
{
    public static Result Validate(ApproveTutorCommand command)
    {
        if (command.TutorId == Guid.Empty)
        {
            return Result.Failure(new Error(
                "ApproveTutorCommand.TutorId.Empty",
                "Tutor id is required.",
                ErrorType.Domain));
        }

        return Result.Success();
    }
}
