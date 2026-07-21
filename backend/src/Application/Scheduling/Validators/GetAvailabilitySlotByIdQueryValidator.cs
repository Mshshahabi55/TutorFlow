using TutorFlow.Application.Common;
using TutorFlow.Application.Scheduling.Queries;

namespace TutorFlow.Application.Scheduling.Validators;

public static class GetAvailabilitySlotByIdQueryValidator
{
    public static Result Validate(GetAvailabilitySlotByIdQuery query)
    {
        if (query.AvailabilitySlotId == Guid.Empty)
        {
            return Result.Failure(new Error(
                "GetAvailabilitySlotByIdQuery.AvailabilitySlotId.Empty",
                "Availability slot id is required.",
                ErrorType.Domain));
        }

        return Result.Success();
    }
}
