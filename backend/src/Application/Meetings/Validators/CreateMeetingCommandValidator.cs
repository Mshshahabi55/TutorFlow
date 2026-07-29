using TutorFlow.Application.Common;
using TutorFlow.Application.Meetings.Commands;

namespace TutorFlow.Application.Meetings.Validators;

public static class CreateMeetingCommandValidator
{
    public static Result Validate(CreateMeetingCommand command)
    {
        if (command.SessionId == Guid.Empty)
        {
            return Result.Failure(new Error(
                "CreateMeetingCommand.SessionId.Empty", "Session id is required.", ErrorType.Domain));
        }

        return Result.Success();
    }
}
