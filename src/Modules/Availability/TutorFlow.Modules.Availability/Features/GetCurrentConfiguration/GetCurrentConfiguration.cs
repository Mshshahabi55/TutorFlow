using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Availability.Persistence;

namespace TutorFlow.Modules.Availability.Features.GetCurrentConfiguration;

public static class GetCurrentConfiguration
{
    public sealed record WeeklyRuleResponse(Guid Id, DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime);

    public sealed record TimeOffResponse(Guid Id, DateTimeOffset StartUtc, DateTimeOffset EndUtc, string? Reason);

    public sealed record BookingSettingsResponse(
        string TimeZoneId,
        int MinNoticeHours,
        int MaxBookingWindowDays,
        int DefaultSlotDurationMinutes,
        bool IsConfigured);

    public sealed record Response(
        Guid TutorUserId,
        IReadOnlyList<WeeklyRuleResponse> WeeklyRules,
        IReadOnlyList<TimeOffResponse> TimeOff,
        BookingSettingsResponse BookingSettings,
        bool IsBookable);

    internal sealed class Handler(AvailabilityDbContext db)
    {
        public async Task<Response> HandleAsync(Guid tutorUserId, CancellationToken ct)
        {
            var rules = await db.WeeklyAvailabilityRules
                .AsNoTracking()
                .Where(r => r.TutorUserId == tutorUserId)
                .ToListAsync(ct);

            var timeOff = await db.TimeOffPeriods
                .AsNoTracking()
                .Where(t => t.TutorUserId == tutorUserId)
                .ToListAsync(ct);

            var settings = await db.BookingSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.TutorUserId == tutorUserId, ct);

            var bookability = await db.TutorBookabilityRecords
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.TutorUserId == tutorUserId, ct);

            var settingsResponse = settings is not null
                ? new BookingSettingsResponse(settings.TimeZoneId, settings.MinNoticeHours, settings.MaxBookingWindowDays, settings.DefaultSlotDurationMinutes, IsConfigured: true)
                : new BookingSettingsResponse("UTC", 24, 30, 60, IsConfigured: false);

            return new Response(
                tutorUserId,
                [.. rules.Select(r => new WeeklyRuleResponse(r.Id, r.DayOfWeek, r.StartTime, r.EndTime))],
                [.. timeOff.Select(t => new TimeOffResponse(t.Id, t.StartUtc, t.EndUtc, t.Reason))],
                settingsResponse,
                bookability?.IsBookable ?? false);
        }
    }
}
