using TutorFlow.Application.Common;
using TutorFlow.Application.Scheduling.Queries;

namespace TutorFlow.Application.Scheduling.Validators;

public static class GetSessionByIdQueryValidator
{
    public static Result Validate(GetSessionByIdQuery query)
    {
        if (query.SessionId == Guid.Empty)
        {
            return Result.Failure(new Error(
                "GetSessionByIdQuery.SessionId.Empty",
                "Session id is required.",
                ErrorType.Domain));
        }

        return Result.Success();
    }
}
