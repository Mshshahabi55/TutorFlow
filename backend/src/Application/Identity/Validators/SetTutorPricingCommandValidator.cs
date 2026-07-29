using TutorFlow.Application.Common;
using TutorFlow.Application.Identity.Commands;

namespace TutorFlow.Application.Identity.Validators;

public static class SetTutorPricingCommandValidator
{
    public static Result Validate(SetTutorPricingCommand command)
    {
        if (command.TutorId == Guid.Empty)
        {
            return Result.Failure(new Error(
                "SetTutorPricingCommand.TutorId.Empty",
                "Tutor id is required.",
                ErrorType.Domain));
        }

        if (command.TrialLessonAvailable && command.TrialLessonPriceAmount is null)
        {
            return Result.Failure(new Error(
                "SetTutorPricingCommand.TrialLessonPrice.Required",
                "A trial lesson price is required when trial lessons are available.",
                ErrorType.Domain));
        }

        return Result.Success();
    }
}
