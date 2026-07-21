using TutorFlow.Application.Common;
using TutorFlow.Application.Identity.Queries;

namespace TutorFlow.Application.Identity.Validators;

public static class GetRelationshipByIdQueryValidator
{
    public static Result Validate(GetRelationshipByIdQuery query)
    {
        if (query.RelationshipId == Guid.Empty)
        {
            return Result.Failure(new Error(
                "GetRelationshipByIdQuery.RelationshipId.Empty",
                "Relationship id is required.",
                ErrorType.Domain));
        }

        return Result.Success();
    }
}
