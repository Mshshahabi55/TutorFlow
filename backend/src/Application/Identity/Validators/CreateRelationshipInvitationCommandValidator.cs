using TutorFlow.Application.Common;
using TutorFlow.Application.Identity.Commands;

namespace TutorFlow.Application.Identity.Validators;

// Structural validation only — never decides relationship state; that stays
// in the Relationship aggregate (docs/adr/ADR-007-validation-strategy.md).
public static class CreateRelationshipInvitationCommandValidator
{
    public static Result Validate(CreateRelationshipInvitationCommand command)
    {
        if (command.ParentGuardianId == Guid.Empty)
        {
            return Result.Failure(new Error(
                "CreateRelationshipInvitationCommand.ParentGuardianId.Empty",
                "Parent/Guardian id is required.",
                ErrorType.Domain));
        }

        if (command.StudentId == Guid.Empty)
        {
            return Result.Failure(new Error(
                "CreateRelationshipInvitationCommand.StudentId.Empty",
                "Student id is required.",
                ErrorType.Domain));
        }

        return Result.Success();
    }
}
