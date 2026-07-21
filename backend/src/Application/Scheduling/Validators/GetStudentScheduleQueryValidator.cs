using TutorFlow.Application.Common;
using TutorFlow.Application.Scheduling.Queries;

namespace TutorFlow.Application.Scheduling.Validators;

public static class GetStudentScheduleQueryValidator
{
    public static Result Validate(GetStudentScheduleQuery query)
    {
        if (query.StudentId == Guid.Empty)
        {
            return Result.Failure(new Error(
                "GetStudentScheduleQuery.StudentId.Empty",
                "Student id is required.",
                ErrorType.Domain));
        }

        return Result.Success();
    }
}
