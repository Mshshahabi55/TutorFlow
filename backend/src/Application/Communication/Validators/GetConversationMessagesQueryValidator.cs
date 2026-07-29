using TutorFlow.Application.Common;
using TutorFlow.Application.Communication.Queries;

namespace TutorFlow.Application.Communication.Validators;

public static class GetConversationMessagesQueryValidator
{
    public static Result Validate(GetConversationMessagesQuery query)
    {
        if (query.ConversationId == Guid.Empty)
        {
            return Result.Failure(new Error(
                "GetConversationMessagesQuery.ConversationId.Empty", "Conversation id is required.", ErrorType.Domain));
        }

        return Result.Success();
    }
}
