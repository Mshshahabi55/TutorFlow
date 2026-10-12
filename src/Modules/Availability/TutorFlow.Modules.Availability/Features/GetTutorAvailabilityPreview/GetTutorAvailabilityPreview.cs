using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Availability.Domain;
using TutorFlow.Modules.Availability.Persistence;
using TutorFlow.Shared;

namespace TutorFlow.Modules.Availability.Features.GetTutorAvailabilityPreview;

/// <summary>
/// Anonymous-access counterpart to GetTutorSlots — Guests see a coarse availability signal on
/// the public Tutor Profile page without authenticating (PREPLY_TUTOR_PROFILE_PARITY_SPEC.md
/// §6/§13, ADR-0022), while the full slot-level grid (GetTutorSlots) stays behind auth. Reuses
/// the same AvailabilityCalculator the full endpoint uses — only the amount of detail returned
/// differs, never the underlying rule evaluation. Never fails: a tutor who hasn't finished
/// configuring booking settings simply has no upcoming availability to report, not an error.
/// </summary>
public static class GetTutorAvailabilityPreview
{
    public sealed record Response(bool HasUpcomingAvailability, DateTimeOffset? NextAvailableStartUtc, string? TimeZoneId);

    // Public (not internal, the usual convention) — reused by AvailabilitySignalPublisher
    // (ADR-0024), which is itself public because Wolverine's convention-discovered consumer
    // classes that depend on it must be public, and a public type can't take a less-accessible
    // type as a constructor parameter.
    public sealed class Handler(AvailabilityDbContext db, TimeProvider clock, IActiveBookingIntervalsQuery activeBookingIntervals)
    {
        public async Task<Response> HandleAsync(Guid tutorUserId, CancellationToken ct)
        {
            var settings = await db.BookingSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.TutorUserId == tutorUserId, ct);

            if (settings is null)
            {
                return new Response(false, null, null);
            }

            var bookability = await db.TutorBookabilityRecords
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.TutorUserId == tutorUserId, ct);

            if (bookability is null || !bookability.IsBookable)
            {
                return new Response(false, null, settings.TimeZoneId);
            }

            var rules = await db.WeeklyAvailabilityRules
                .AsNoTracking()
                .Where(r => r.TutorUserId == tutorUserId)
                .ToListAsync(ct);

            var timeOff = await db.TimeOffPeriods
                .AsNoTracking()
                .Where(t => t.TutorUserId == tutorUserId)
                .ToListAsync(ct);

            var now = clock.GetUtcNow();
            var activeIntervals = await activeBookingIntervals.GetActiveIntervalsAsync(
                tutorUserId, now, now.AddDays(settings.MaxBookingWindowDays), ct);

            var occupied = activeIntervals.Select(i => new OccupiedInterval(i.StartUtc, i.EndUtc)).ToList();

            var slotStarts = AvailabilityCalculator.ComputeOpenSlotStarts(rules, timeOff, settings, now, occupied);

            return new Response(
                slotStarts.Count > 0,
                slotStarts.Count > 0 ? slotStarts[0] : null,
                settings.TimeZoneId);
        }
    }
}
