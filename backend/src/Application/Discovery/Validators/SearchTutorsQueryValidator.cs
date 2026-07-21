using TutorFlow.Application.Common;
using TutorFlow.Application.Discovery.Queries;

namespace TutorFlow.Application.Discovery.Validators;

public static class SearchTutorsQueryValidator
{
    public static Result Validate(SearchTutorsQuery query)
    {
        if (query.Page < 1)
        {
            return Result.Failure(new Error(
                "SearchTutorsQuery.Page.InvalidRange",
                "Page must be at least 1.",
                ErrorType.Domain));
        }

        if (query.PageSize < PageRequest.MinPageSize || query.PageSize > PageRequest.MaxPageSize)
        {
            return Result.Failure(new Error(
                "SearchTutorsQuery.PageSize.InvalidRange",
                $"Page size must be between {PageRequest.MinPageSize} and {PageRequest.MaxPageSize}.",
                ErrorType.Domain));
        }

        return Result.Success();
    }
}
