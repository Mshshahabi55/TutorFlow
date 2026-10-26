using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TutorFlow.Modules.Availability.Domain;
using TutorFlow.Modules.Availability.Infrastructure;
using TutorFlow.Modules.Availability.Persistence;
using Wolverine;

namespace TutorFlow.Modules.Availability.Features.UpdateBookingSettings;

public static class UpdateBookingSettings
{
    public sealed record Command(string TimeZoneId, int MinNoticeHours, int MaxBookingWindowDays, int DefaultSlotDurationMinutes);

    public sealed record Response(
        string TimeZoneId,
        int MinNoticeHours,
        int MaxBookingWindowDays,
        int DefaultSlotDurationMinutes,
        DateTimeOffset UpdatedAtUtc);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TimeZoneId).NotEmpty();
            RuleFor(x => x.MinNoticeHours).GreaterThanOrEqualTo(0);
            RuleFor(x => x.MaxBookingWindowDays).GreaterThan(0);
            RuleFor(x => x.DefaultSlotDurationMinutes).GreaterThan(0);
        }
    }

    internal sealed class Handler(AvailabilityDbContext db, TimeProvider clock, IMessageBus bus)
    {
        public async Task<Result<Response>> HandleAsync(Guid tutorUserId, Command command, CancellationToken ct)
        {
            var existing = await db.BookingSettings.FirstOrDefaultAsync(s => s.TutorUserId == tutorUserId, ct);

            if (existing is null)
            {
                var createResult = BookingSettings.Create(
                    tutorUserId,
                    command.TimeZoneId,
                    command.MinNoticeHours,
                    command.MaxBookingWindowDays,
                    command.DefaultSlotDurationMinutes,
                    clock);

                if (createResult.IsFailure)
                {
                    return Result.Failure<Response>([.. createResult.Errors]);
                }

                db.BookingSettings.Add(createResult.Value);

                try
                {
                    await db.SaveChangesAsync(ct);
                }
                catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
                {
                    // Two concurrent first-time PUTs can both see no existing settings and both
                    // try to create one; the database's primary key is the real guard against
                    // that race.
                    return Result.Failure<Response>("Booking settings were already created for this user; please retry.");
                }

                await bus.InvokeAsync(new RecomputeAvailabilitySignalCommand(tutorUserId), ct);

                return Result.Success(Map(createResult.Value));
            }

            var applyResult = existing.Apply(
                command.TimeZoneId,
                command.MinNoticeHours,
                command.MaxBookingWindowDays,
                command.DefaultSlotDurationMinutes,
                clock);

            if (applyResult.IsFailure)
            {
                return Result.Failure<Response>([.. applyResult.Errors]);
            }

            await db.SaveChangesAsync(ct);

            await bus.InvokeAsync(new RecomputeAvailabilitySignalCommand(tutorUserId), ct);

            return Result.Success(Map(existing));
        }

        private static Response Map(BookingSettings settings) => new(
            settings.TimeZoneId,
            settings.MinNoticeHours,
            settings.MaxBookingWindowDays,
            settings.DefaultSlotDurationMinutes,
            settings.UpdatedAtUtc);
    }
}
