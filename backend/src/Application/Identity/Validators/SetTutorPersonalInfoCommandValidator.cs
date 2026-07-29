using TutorFlow.Application.Common;
using TutorFlow.Application.Identity.Commands;

namespace TutorFlow.Application.Identity.Validators;

// Structural validation only — field-level rules (length limits) are
// Domain's own (Tutor.SetPersonalInfo), enforced by the handler
// (docs/adr/ADR-007-validation-strategy.md).
public static class SetTutorPersonalInfoCommandValidator
{
    public static Result Validate(SetTutorPersonalInfoCommand command)
    {
        if (command.TutorId == Guid.Empty)
        {
            return Result.Failure(new Error(
                "SetTutorPersonalInfoCommand.TutorId.Empty",
                "Tutor id is required.",
                ErrorType.Domain));
        }

        return Result.Success();
    }
}
