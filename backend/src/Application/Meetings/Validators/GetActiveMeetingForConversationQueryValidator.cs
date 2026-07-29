using TutorFlow.Application.Common;
using TutorFlow.Application.Meetings.Queries;

namespace TutorFlow.Application.Meetings.Validators;

public static class GetActiveMeetingForConversationQueryValidator
{
    public static Result Validate(GetActiveMeetingForConversationQuery query)
    {
        if (query.ConversationId == Guid.Empty)
        {
            return Result.Failure(new Error(
                "GetActiveMeetingForConversationQuery.ConversationId.Empty", "Conversation id is required.", ErrorType.Domain));
        }

        return Result.Success();
    }
}
