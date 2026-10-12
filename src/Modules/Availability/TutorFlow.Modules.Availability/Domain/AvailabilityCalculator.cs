using TimeZoneConverter;

namespace TutorFlow.Modules.Availability.Domain;

// A future Bookings-sourced interval (an active hold or confirmed booking) that should block a
// slot. Always empty today — Bookings does not exist yet and this module must not invent that
// integration — but ComputeOpenSlotStarts already accepts these so the calculator does not need
// to be redesigned when Bookings arrives.
public sealed record OccupiedInterval(DateTimeOffset StartUtc, DateTimeOffset EndUtc);

public static class AvailabilityCalculator
{
    public static IReadOnlyList<DateTimeOffset> ComputeOpenSlotStarts(
        IReadOnlyList<WeeklyAvailabilityRule> rules,
        IReadOnlyList<TimeOff> timeOff,
        BookingSettings settings,
        DateTimeOffset nowUtc,
        IReadOnlyList<OccupiedInterval>? occupiedIntervals = null,
        // ADR-0025: lets a caller ask "what 25-minute slots are open" instead of only the
        // tutor's own configured DefaultSlotDurationMinutes, and scope the response to a
        // specific window (e.g. one week) instead of the tutor's entire booking window —
        // without a second computation path. Both default to today's exact existing behavior.
        int? slotDurationMinutes = null,
        DateTimeOffset? rangeStartUtc = null,
        DateTimeOffset? rangeEndUtc = null)
    {
        occupiedIntervals ??= [];

        // TZConvert, not TimeZoneInfo.FindSystemTimeZoneById directly (A46): with
        // InvariantGlobalization enabled (Directory.Build.props), Windows loses .NET 6+'s
        // ICU-based IANA-ID resolution and falls back to registry-only lookup, so a real IANA
        // ID like "Europe/London" throws TimeZoneNotFoundException — "UTC" is a built-in
        // special case that happens to still resolve, which is why no existing test caught
        // this. TZConvert maps IANA -> the equivalent Windows ID first, then resolves that via
        // the registry — correct on Windows regardless of InvariantGlobalization, a no-op
        // pass-through on Linux/macOS (where IANA IDs already resolve natively via tzdata).
        // DST rules come from the OS's own time zone data either way, so this doesn't trade off
        // DST/offset correctness.
        var timeZone = TZConvert.GetTimeZoneInfo(settings.TimeZoneId);
        var earliestUtc = Max(nowUtc.AddHours(settings.MinNoticeHours), rangeStartUtc ?? DateTimeOffset.MinValue);
        var latestUtc = Min(nowUtc.AddDays(settings.MaxBookingWindowDays), rangeEndUtc ?? DateTimeOffset.MaxValue);
        var slotDuration = TimeSpan.FromMinutes(slotDurationMinutes ?? settings.DefaultSlotDurationMinutes);

        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(nowUtc.UtcDateTime, timeZone);
        var startDateLocal = DateOnly.FromDateTime(nowLocal);

        var slots = new List<DateTimeOffset>();

        for (var dayOffset = 0; dayOffset <= settings.MaxBookingWindowDays; dayOffset++)
        {
            var date = startDateLocal.AddDays(dayOffset);
            var rulesForDay = rules.Where(r => r.DayOfWeek == date.DayOfWeek);

            foreach (var rule in rulesForDay)
            {
                // Walk slot-start offsets as a TimeSpan from the rule's own start rather than
                // stepping TimeOnly values directly — TimeOnly.Add wraps at 24h, which would
                // silently let a slot bleed past midnight into the next calendar day for a rule
                // near the end of the day. Since Create() guarantees EndTime > StartTime within
                // the same day, ruleDuration is always a same-day span with no wraparound.
                var ruleDuration = rule.EndTime.ToTimeSpan() - rule.StartTime.ToTimeSpan();

                for (var offset = TimeSpan.Zero; offset + slotDuration <= ruleDuration; offset += slotDuration)
                {
                    var localSlotStart = rule.StartTime.Add(offset);
                    var localDateTime = date.ToDateTime(localSlotStart);

                    if (timeZone.IsInvalidTime(localDateTime))
                    {
                        // This wall-clock time does not exist on this date in this time zone
                        // (DST spring-forward gap) — there is nothing to convert. Skip this one
                        // candidate rather than throw; the rest of the rule's slots on this date
                        // (and every other date) are unaffected.
                        continue;
                    }

                    // Ambiguous local times (DST fall-back) are not skipped: TimeZoneInfo
                    // resolves them deterministically, and each local (date, time) pair is only
                    // ever enumerated once by this loop, so no duplicate slot can result.
                    var slotStartUtc = new DateTimeOffset(
                        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified), timeZone),
                        TimeSpan.Zero);

                    if (slotStartUtc < earliestUtc || slotStartUtc > latestUtc)
                    {
                        continue;
                    }

                    var slotEndUtc = slotStartUtc + slotDuration;

                    var blockedByTimeOff = timeOff.Any(t => slotStartUtc < t.EndUtc && slotEndUtc > t.StartUtc);
                    var blockedByOccupiedInterval = occupiedIntervals.Any(o => slotStartUtc < o.EndUtc && slotEndUtc > o.StartUtc);

                    if (!blockedByTimeOff && !blockedByOccupiedInterval)
                    {
                        slots.Add(slotStartUtc);
                    }
                }
            }
        }

        return slots.OrderBy(s => s).ToList();
    }

    private static DateTimeOffset Max(DateTimeOffset a, DateTimeOffset b) => a > b ? a : b;

    private static DateTimeOffset Min(DateTimeOffset a, DateTimeOffset b) => a < b ? a : b;

    // Generalized counterpart to ComputeOpenSlotStarts: validates one specific requested
    // (startUtc, durationMinutes) pair rather than enumerating every slot at the tutor's own
    // configured DefaultSlotDurationMinutes. Required by ITutorAvailabilityQuery (ADR-0014) so
    // Bookings can validate arbitrary trial durations without Availability's rule-evaluation
    // logic being duplicated inside Bookings. Deliberately does not consider Bookings' own
    // active holds/bookings (BOOKINGS_SPEC.md §5 — that is the exclusion constraint's job, not
    // this query's).
    public static (bool IsValid, string? Reason) ValidateRequestedSlot(
        IReadOnlyList<WeeklyAvailabilityRule> rules,
        IReadOnlyList<TimeOff> timeOff,
        BookingSettings settings,
        DateTimeOffset nowUtc,
        DateTimeOffset requestedStartUtc,
        int requestedDurationMinutes)
    {
        if (requestedDurationMinutes <= 0)
        {
            return (false, "duration must be positive");
        }

        var earliestUtc = nowUtc.AddHours(settings.MinNoticeHours);
        var latestUtc = nowUtc.AddDays(settings.MaxBookingWindowDays);
        var requestedEndUtc = requestedStartUtc.AddMinutes(requestedDurationMinutes);

        if (requestedStartUtc < earliestUtc)
        {
            return (false, "inside minimum notice window");
        }

        if (requestedStartUtc > latestUtc)
        {
            return (false, "beyond maximum booking window");
        }

        // TZConvert, not TimeZoneInfo.FindSystemTimeZoneById directly — see ComputeOpenSlotStarts'
        // own comment above for why (A46).
        var timeZone = TZConvert.GetTimeZoneInfo(settings.TimeZoneId);

        // Converting a fixed UTC instant to local time is always well-defined (unlike the
        // opposite direction in ComputeOpenSlotStarts) — DST-invalid/ambiguous concerns only
        // apply when deriving a UTC instant from a local wall-clock time, not here.
        var localStart = TimeZoneInfo.ConvertTimeFromUtc(requestedStartUtc.UtcDateTime, timeZone);
        var localEnd = localStart.AddMinutes(requestedDurationMinutes);

        if (localEnd.Date != localStart.Date)
        {
            // No weekly rule can span midnight — a requested slot that crosses into the next
            // calendar day in the tutor's own time zone cannot fit within any single rule.
            return (false, "outside availability");
        }

        var localStartTime = TimeOnly.FromDateTime(localStart);
        var localEndTime = TimeOnly.FromDateTime(localEnd);

        var fitsRule = rules.Any(r =>
            r.DayOfWeek == localStart.DayOfWeek &&
            localStartTime >= r.StartTime &&
            localEndTime <= r.EndTime);

        if (!fitsRule)
        {
            return (false, "outside availability");
        }

        var blockedByTimeOff = timeOff.Any(t => requestedStartUtc < t.EndUtc && requestedEndUtc > t.StartUtc);
        if (blockedByTimeOff)
        {
            return (false, "blocked by time off");
        }

        return (true, null);
    }
}
