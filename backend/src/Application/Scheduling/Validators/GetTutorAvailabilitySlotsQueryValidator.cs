using TutorFlow.Application.Common;
using TutorFlow.Application.Scheduling.Queries;

namespace TutorFlow.Application.Scheduling.Validators;

public static class GetTutorAvailabilitySlotsQueryValidator
{
    public static Result Validate(GetTutorAvailabilitySlotsQuery query)
    {
        if (query.TutorId == Guid.Empty)
        {
            return Result.Failure(new Error(
                "GetTutorAvailabilitySlotsQuery.TutorId.Empty",
                "Tutor id is required.",
                ErrorType.Domain));
        }

        return Result.Success();
    }
}
