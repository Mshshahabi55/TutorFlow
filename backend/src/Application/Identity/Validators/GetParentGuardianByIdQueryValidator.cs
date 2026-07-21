using TutorFlow.Application.Common;
using TutorFlow.Application.Identity.Queries;

namespace TutorFlow.Application.Identity.Validators;

public static class GetParentGuardianByIdQueryValidator
{
    public static Result Validate(GetParentGuardianByIdQuery query)
    {
        if (query.ParentGuardianId == Guid.Empty)
        {
            return Result.Failure(new Error(
                "GetParentGuardianByIdQuery.ParentGuardianId.Empty",
                "Parent/Guardian id is required.",
                ErrorType.Domain));
        }

        return Result.Success();
    }
}
