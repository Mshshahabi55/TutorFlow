using TutorFlow.Application.Common;
using TutorFlow.Application.Identity.Queries;

namespace TutorFlow.Application.Identity.Validators;

public static class GetStudentByIdQueryValidator
{
    public static Result Validate(GetStudentByIdQuery query)
    {
        if (query.StudentId == Guid.Empty)
        {
            return Result.Failure(new Error(
                "GetStudentByIdQuery.StudentId.Empty",
                "Student id is required.",
                ErrorType.Domain));
        }

        return Result.Success();
    }
}
