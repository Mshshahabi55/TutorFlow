using FluentValidation;
using TutorFlow.Modules.Availability.Domain;
using TutorFlow.Modules.Availability.Infrastructure;
using TutorFlow.Modules.Availability.Persistence;

namespace TutorFlow.Modules.Availability.Features.AddTimeOff;

public static class AddTimeOff
{
    public sealed record Command(DateTimeOffset StartUtc, DateTimeOffset EndUtc, string? Reason);

    public sealed record Response(Guid Id, DateTimeOffset StartUtc, DateTimeOffset EndUtc, string? Reason);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.StartUtc).LessThan(x => x.EndUtc).WithMessage("Time off start must be before its end.");
            RuleFor(x => x.Reason).MaximumLength(500).When(x => x.Reason is not null);
        }
    }

    internal sealed class Handler(AvailabilityDbContext db, AvailabilitySignalPublisher signalPublisher)
    {
        public async Task<Result<Response>> HandleAsync(Guid tutorUserId, Command command, CancellationToken ct)
        {
            var createResult = TimeOff.Create(tutorUserId, command.StartUtc, command.EndUtc, command.Reason);
            if (createResult.IsFailure)
            {
                return Result.Failure<Response>([.. createResult.Errors]);
            }

            var timeOff = createResult.Value;
            db.TimeOffPeriods.Add(timeOff);
            await db.SaveChangesAsync(ct);

            await signalPublisher.PublishCurrentSignalAsync(tutorUserId, ct);

            return Result.Success(new Response(timeOff.Id, timeOff.StartUtc, timeOff.EndUtc, timeOff.Reason));
        }
    }
}
