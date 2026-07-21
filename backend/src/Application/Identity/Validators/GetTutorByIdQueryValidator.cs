using TutorFlow.Application.Common;
using TutorFlow.Application.Identity.Queries;

namespace TutorFlow.Application.Identity.Validators;

public static class GetTutorByIdQueryValidator
{
    public static Result Validate(GetTutorByIdQuery query)
    {
        if (query.TutorId == Guid.Empty)
        {
            return Result.Failure(new Error(
                "GetTutorByIdQuery.TutorId.Empty",
                "Tutor id is required.",
                ErrorType.Domain));
        }

        return Result.Success();
    }
}
