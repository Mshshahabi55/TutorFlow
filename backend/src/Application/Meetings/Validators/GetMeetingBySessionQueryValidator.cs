using TutorFlow.Application.Common;
using TutorFlow.Application.Meetings.Queries;

namespace TutorFlow.Application.Meetings.Validators;

public static class GetMeetingBySessionQueryValidator
{
    public static Result Validate(GetMeetingBySessionQuery query)
    {
        if (query.SessionId == Guid.Empty)
        {
            return Result.Failure(new Error(
                "GetMeetingBySessionQuery.SessionId.Empty", "Session id is required.", ErrorType.Domain));
        }

        return Result.Success();
    }
}
