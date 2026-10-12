using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Availability.Domain;
using TutorFlow.Modules.Availability.Persistence;
using TutorFlow.Shared;

namespace TutorFlow.Modules.Availability.Features.GetTutorSlots;

// Public, non-self-service counterpart to GetComputedSlots (BOOKINGS_SPEC.md §5 prerequisite
// #2/#3) — any authenticated user may browse a different tutor's open slots, unlike the
// existing /me/slots endpoint. Unlike GetComputedSlots, this excludes Bookings' active
// Held/PendingPayment/Confirmed intervals via the reciprocal IActiveBookingIntervalsQuery
// (ADR-0014 correction), satisfying ADR-0003's original requirement that Availability treat
// Bookings' active holds as unavailable.
//
// ADR-0025: duration- and range-aware — durationMinutes/fromUtc/toUtc are all optional and
// default to today's exact prior behavior (tutor's own DefaultSlotDurationMinutes, the tutor's
// full booking window) so this remains the one slot-listing endpoint, not a second API. The
// 25/50-minute trial-duration *policy* stays owned by Bookings (BookingsOptions); this endpoint
// only validates durationMinutes against Availability's own structural sanity bounds.
public static class GetTutorSlots
{
    // Mirrors BookingSettings' own private SlotDurationMinutesLowerBound/UpperBound — duplicated
    // rather than widened to internal, since these are structural sanity bounds, not a policy
    // this endpoint should reach into the domain type to share.
    private const int SlotDurationMinutesLowerBound = 15;
    private const int SlotDurationMinutesUpperBound = 240;

    public sealed record Query(int? DurationMinutes, DateTimeOffset? FromUtc, DateTimeOffset? ToUtc);

    public sealed record Slot(DateTimeOffset StartUtc, DateTimeOffset EndUtc);

    public sealed record Response(
        string TimeZoneId,
        int DurationMinutes,
        int MaxBookingWindowDays,
        IReadOnlyList<Slot> Slots);

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.DurationMinutes!.Value)
                .InclusiveBetween(SlotDurationMinutesLowerBound, SlotDurationMinutesUpperBound)
                .When(q => q.DurationMinutes is not null);

            RuleFor(q => q.ToUtc!.Value)
                .GreaterThan(q => q.FromUtc!.Value)
                .When(q => q.FromUtc is not null && q.ToUtc is not null)
                .WithMessage("toUtc must be after fromUtc.");
        }
    }

    internal sealed class Handler(AvailabilityDbContext db, TimeProvider clock, IActiveBookingIntervalsQuery activeBookingIntervals)
    {
        public async Task<Result<Response>> HandleAsync(Guid tutorUserId, Query query, CancellationToken ct)
        {
            var settings = await db.BookingSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.TutorUserId == tutorUserId, ct);

            if (settings is null)
            {
                return Result.Failure<Response>("Booking settings must be configured before availability can be computed.");
            }

            var bookability = await db.TutorBookabilityRecords
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.TutorUserId == tutorUserId, ct);

            if (bookability is null || !bookability.IsBookable)
            {
                return Result.Failure<Response>("Tutor is not currently bookable.");
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
            var rangeStartUtc = query.FromUtc ?? now;
            var rangeEndUtc = query.ToUtc ?? rangeStartUtc.AddDays(7);

            // Scope the Bookings-side query to the requested window, not the tutor's entire
            // booking window — this is the one real DB-query cost the range narrows (the
            // in-memory day-iteration below is cheap regardless of range).
            var activeIntervals = await activeBookingIntervals.GetActiveIntervalsAsync(tutorUserId, rangeStartUtc, rangeEndUtc, ct);
            var occupied = activeIntervals.Select(i => new OccupiedInterval(i.StartUtc, i.EndUtc)).ToList();

            var slotStarts = AvailabilityCalculator.ComputeOpenSlotStarts(
                rules, timeOff, settings, now, occupied,
                slotDurationMinutes: query.DurationMinutes,
                rangeStartUtc: rangeStartUtc,
                rangeEndUtc: rangeEndUtc);

            var slotDuration = TimeSpan.FromMinutes(query.DurationMinutes ?? settings.DefaultSlotDurationMinutes);
            var slots = slotStarts.Select(s => new Slot(s, s + slotDuration)).ToList();

            return Result.Success(new Response(settings.TimeZoneId, (int)slotDuration.TotalMinutes, settings.MaxBookingWindowDays, slots));
        }
    }
}
