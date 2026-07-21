using TutorFlow.Application.Common;
using TutorFlow.Application.Scheduling.Queries;

namespace TutorFlow.Application.Scheduling.Validators;

public static class GetTutorScheduleQueryValidator
{
    public static Result Validate(GetTutorScheduleQuery query)
    {
        if (query.TutorId == Guid.Empty)
        {
            return Result.Failure(new Error(
                "GetTutorScheduleQuery.TutorId.Empty",
                "Tutor id is required.",
                ErrorType.Domain));
        }

        return Result.Success();
    }
}
