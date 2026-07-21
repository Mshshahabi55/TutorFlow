using TutorFlow.Application.Common;
using TutorFlow.Application.Identity.Queries;

namespace TutorFlow.Application.Identity.Validators;

public static class GetPendingTutorsQueryValidator
{
    public static Result Validate(GetPendingTutorsQuery query)
    {
        if (query.Page < 1)
        {
            return Result.Failure(new Error(
                "GetPendingTutorsQuery.Page.InvalidRange",
                "Page must be at least 1.",
                ErrorType.Domain));
        }

        if (query.PageSize < PageRequest.MinPageSize || query.PageSize > PageRequest.MaxPageSize)
        {
            return Result.Failure(new Error(
                "GetPendingTutorsQuery.PageSize.InvalidRange",
                $"Page size must be between {PageRequest.MinPageSize} and {PageRequest.MaxPageSize}.",
                ErrorType.Domain));
        }

        return Result.Success();
    }
}
