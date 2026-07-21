using TutorFlow.Application.Common;
using TutorFlow.Application.Oversight.Queries;

namespace TutorFlow.Application.Oversight.Validators;

public static class GetAllSessionsQueryValidator
{
    public static Result Validate(GetAllSessionsQuery query)
    {
        if (query.Page < 1)
        {
            return Result.Failure(new Error(
                "GetAllSessionsQuery.Page.InvalidRange",
                "Page must be at least 1.",
                ErrorType.Domain));
        }

        if (query.PageSize < PageRequest.MinPageSize || query.PageSize > PageRequest.MaxPageSize)
        {
            return Result.Failure(new Error(
                "GetAllSessionsQuery.PageSize.InvalidRange",
                $"Page size must be between {PageRequest.MinPageSize} and {PageRequest.MaxPageSize}.",
                ErrorType.Domain));
        }

        return Result.Success();
    }
}
