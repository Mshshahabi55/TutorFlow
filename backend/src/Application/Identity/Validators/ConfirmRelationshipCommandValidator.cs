using TutorFlow.Application.Common;
using TutorFlow.Application.Identity.Commands;

namespace TutorFlow.Application.Identity.Validators;

// Structural validation only — see CreateRelationshipInvitationCommandValidator.
public static class ConfirmRelationshipCommandValidator
{
    public static Result Validate(ConfirmRelationshipCommand command)
    {
        if (command.RelationshipId == Guid.Empty)
        {
            return Result.Failure(new Error(
                "ConfirmRelationshipCommand.RelationshipId.Empty",
                "Relationship id is required.",
                ErrorType.Domain));
        }

        return Result.Success();
    }
}
