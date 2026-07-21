using TutorFlow.Application.Common;
using TutorFlow.Application.Identity.Queries;

namespace TutorFlow.Application.Identity.Validators;

public static class GetRelationshipsByAccountIdQueryValidator
{
    public static Result Validate(GetRelationshipsByAccountIdQuery query)
    {
        if (query.AccountId == Guid.Empty)
        {
            return Result.Failure(new Error(
                "GetRelationshipsByAccountIdQuery.AccountId.Empty",
                "Account id is required.",
                ErrorType.Domain));
        }

        return Result.Success();
    }
}
