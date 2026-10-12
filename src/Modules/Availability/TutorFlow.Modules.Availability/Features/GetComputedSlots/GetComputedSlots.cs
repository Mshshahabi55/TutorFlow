using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Availability.Domain;
using TutorFlow.Modules.Availability.Persistence;

namespace TutorFlow.Modules.Availability.Features.GetComputedSlots;

public static class GetComputedSlots
{
    public sealed record Slot(DateTimeOffset StartUtc, DateTimeOffset EndUtc);

    public sealed record Response(IReadOnlyList<Slot> Slots);

    internal sealed class Handler(AvailabilityDbContext db, TimeProvider clock)
    {
        public async Task<Result<Response>> HandleAsync(Guid tutorUserId, CancellationToken ct)
        {
            var settings = await db.BookingSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.TutorUserId == tutorUserId, ct);

            if (settings is null)
            {
                return Result.Failure<Response>("Booking settings must be configured before availability can be computed.");
            }

            var rules = await db.WeeklyAvailabilityRules
                .AsNoTracking()
                .Where(r => r.TutorUserId == tutorUserId)
                .ToListAsync(ct);

            var timeOff = await db.TimeOffPeriods
                .AsNoTracking()
                .Where(t => t.TutorUserId == tutorUserId)
                .ToListAsync(ct);

            var slotStarts = AvailabilityCalculator.ComputeOpenSlotStarts(rules, timeOff, settings, clock.GetUtcNow());
            var slotDuration = TimeSpan.FromMinutes(settings.DefaultSlotDurationMinutes);
            var slots = slotStarts.Select(s => new Slot(s, s + slotDuration)).ToList();

            return Result.Success(new Response(slots));
        }
    }
}
