using TutorFlow.Application.Audit.Queries;
using TutorFlow.Application.Common;

namespace TutorFlow.Application.Audit.Validators;

public static class GetAuditEntriesQueryValidator
{
    public static Result Validate(GetAuditEntriesQuery query)
    {
        if (query.SubjectId.HasValue && query.SubjectId.Value == Guid.Empty)
        {
            return Result.Failure(new Error(
                "GetAuditEntriesQuery.SubjectId.Empty",
                "Subject id, when provided, cannot be empty.",
                ErrorType.Domain));
        }

        if (query.Page < 1)
        {
            return Result.Failure(new Error(
                "GetAuditEntriesQuery.Page.InvalidRange",
                "Page must be at least 1.",
                ErrorType.Domain));
        }

        if (query.PageSize < PageRequest.MinPageSize || query.PageSize > PageRequest.MaxPageSize)
        {
            return Result.Failure(new Error(
                "GetAuditEntriesQuery.PageSize.InvalidRange",
                $"Page size must be between {PageRequest.MinPageSize} and {PageRequest.MaxPageSize}.",
                ErrorType.Domain));
        }

        return Result.Success();
    }
}
