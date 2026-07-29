using TutorFlow.Application.Common;
using TutorFlow.Application.Communication.Commands;

namespace TutorFlow.Application.Communication.Validators;

public static class MarkNotificationReadCommandValidator
{
    public static Result Validate(MarkNotificationReadCommand command)
    {
        if (command.NotificationId == Guid.Empty)
        {
            return Result.Failure(new Error(
                "MarkNotificationReadCommand.NotificationId.Empty", "Notification id is required.", ErrorType.Domain));
        }

        return Result.Success();
    }
}
