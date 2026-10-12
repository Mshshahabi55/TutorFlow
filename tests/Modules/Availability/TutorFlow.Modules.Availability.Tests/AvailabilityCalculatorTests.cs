using TutorFlow.Modules.Availability.Domain;

namespace TutorFlow.Modules.Availability.Tests;

public class AvailabilityCalculatorTests
{
    private static DateOnly NextDayOfWeek(DayOfWeek dayOfWeek)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var offset = ((int)dayOfWeek - (int)today.DayOfWeek + 7) % 7;
        return today.AddDays(offset);
    }

    private static BookingSettings Settings(string timeZoneId, int minNoticeHours, int maxBookingWindowDays, int slotMinutes) =>
        BookingSettings.Create(Guid.NewGuid(), timeZoneId, minNoticeHours, maxBookingWindowDays, slotMinutes, TimeProvider.System).Value;

    // Finds a real transition date from the time zone's own data (not hand-calculated), so the
    // test is self-verifying regardless of which year it runs in.
    private static DateOnly FindDstTransitionDate(TimeZoneInfo timeZone, int year, int month, bool invalid)
    {
        for (var day = 1; day <= DateTime.DaysInMonth(year, month); day++)
        {
            var probeHour = invalid ? 2 : 1;
            var probe = new DateTime(year, month, day, probeHour, 30, 0, DateTimeKind.Unspecified);
            var matches = invalid ? timeZone.IsInvalidTime(probe) : timeZone.IsAmbiguousTime(probe);
            if (matches)
            {
                return new DateOnly(year, month, day);
            }
        }

        throw new InvalidOperationException($"No DST transition found in {timeZone.Id} {year}-{month:D2}.");
    }

    private static DateTimeOffset LocalMidnightUtc(DateOnly date, TimeZoneInfo timeZone) =>
        new(TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified), timeZone), TimeSpan.Zero);

    [Fact]
    public void ComputeOpenSlotStarts_WithSingleUtcRule_ReturnsHourlySlotsWithinWindow()
    {
        var tutorId = Guid.NewGuid();
        var monday = NextDayOfWeek(DayOfWeek.Monday);
        var nowUtc = new DateTimeOffset(monday.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var rule = WeeklyAvailabilityRule.Create(tutorId, DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0)).Value;
        var settings = Settings("UTC", minNoticeHours: 0, maxBookingWindowDays: 7, slotMinutes: 60);

        var slots = AvailabilityCalculator.ComputeOpenSlotStarts([rule], [], settings, nowUtc);

        var mondaySlots = slots.Where(s => s.Date == nowUtc.Date).ToList();
        Assert.Equal(8, mondaySlots.Count);
        Assert.Contains(new DateTimeOffset(monday.ToDateTime(new TimeOnly(9, 0)), TimeSpan.Zero), mondaySlots);
        Assert.Contains(new DateTimeOffset(monday.ToDateTime(new TimeOnly(16, 0)), TimeSpan.Zero), mondaySlots);
        Assert.DoesNotContain(new DateTimeOffset(monday.ToDateTime(new TimeOnly(17, 0)), TimeSpan.Zero), mondaySlots);
    }

    [Fact]
    public void ComputeOpenSlotStarts_RespectsMinNoticeHours()
    {
        var tutorId = Guid.NewGuid();
        var monday = NextDayOfWeek(DayOfWeek.Monday);
        var nowUtc = new DateTimeOffset(monday.ToDateTime(new TimeOnly(9, 30)), TimeSpan.Zero);
        var rule = WeeklyAvailabilityRule.Create(tutorId, DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0)).Value;
        var settings = Settings("UTC", minNoticeHours: 2, maxBookingWindowDays: 1, slotMinutes: 60);

        var slots = AvailabilityCalculator.ComputeOpenSlotStarts([rule], [], settings, nowUtc);

        Assert.DoesNotContain(new DateTimeOffset(monday.ToDateTime(new TimeOnly(11, 0)), TimeSpan.Zero), slots);
        Assert.Contains(new DateTimeOffset(monday.ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero), slots);
    }

    [Fact]
    public void ComputeOpenSlotStarts_ExcludesSlotsCoveredByTimeOff()
    {
        var tutorId = Guid.NewGuid();
        var monday = NextDayOfWeek(DayOfWeek.Monday);
        var nowUtc = new DateTimeOffset(monday.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var rule = WeeklyAvailabilityRule.Create(tutorId, DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0)).Value;
        var settings = Settings("UTC", minNoticeHours: 0, maxBookingWindowDays: 1, slotMinutes: 60);
        var timeOffStart = new DateTimeOffset(monday.ToDateTime(new TimeOnly(13, 0)), TimeSpan.Zero);
        var timeOff = TimeOff.Create(tutorId, timeOffStart, timeOffStart.AddHours(1), "Break").Value;

        var slots = AvailabilityCalculator.ComputeOpenSlotStarts([rule], [timeOff], settings, nowUtc);

        Assert.DoesNotContain(timeOffStart, slots);
        Assert.Contains(new DateTimeOffset(monday.ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero), slots);
        Assert.Contains(new DateTimeOffset(monday.ToDateTime(new TimeOnly(14, 0)), TimeSpan.Zero), slots);
    }

    [Fact]
    public void ComputeOpenSlotStarts_RespectsMaxBookingWindowDays()
    {
        var tutorId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var nowUtc = new DateTimeOffset(today.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        // Available every day, so the window boundary (not day-of-week matching) is what's under test.
        var rules = Enum.GetValues<DayOfWeek>()
            .Select(d => WeeklyAvailabilityRule.Create(tutorId, d, new TimeOnly(9, 0), new TimeOnly(10, 0)).Value)
            .ToList();
        var settings = Settings("UTC", minNoticeHours: 0, maxBookingWindowDays: 1, slotMinutes: 60);

        var slots = AvailabilityCalculator.ComputeOpenSlotStarts(rules, [], settings, nowUtc);

        Assert.All(slots, s => Assert.True(s.Date == today.ToDateTime(TimeOnly.MinValue) || s.Date == today.AddDays(1).ToDateTime(TimeOnly.MinValue)));
        Assert.DoesNotContain(slots, s => s.Date == today.AddDays(2).ToDateTime(TimeOnly.MinValue));
    }

    [Fact]
    public void ComputeOpenSlotStarts_ExcludesSlotsCoveredByOccupiedIntervals()
    {
        var tutorId = Guid.NewGuid();
        var monday = NextDayOfWeek(DayOfWeek.Monday);
        var nowUtc = new DateTimeOffset(monday.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var rule = WeeklyAvailabilityRule.Create(tutorId, DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(11, 0)).Value;
        var settings = Settings("UTC", minNoticeHours: 0, maxBookingWindowDays: 1, slotMinutes: 60);
        var occupiedStart = new DateTimeOffset(monday.ToDateTime(new TimeOnly(9, 0)), TimeSpan.Zero);
        var occupied = new OccupiedInterval(occupiedStart, occupiedStart.AddHours(1));

        var slots = AvailabilityCalculator.ComputeOpenSlotStarts([rule], [], settings, nowUtc, [occupied]);

        Assert.DoesNotContain(occupiedStart, slots);
        Assert.Contains(occupiedStart.AddHours(1), slots);
    }

    [Fact]
    public void ComputeOpenSlotStarts_ConvertsNonUtcTimeZoneCorrectly()
    {
        var tutorId = Guid.NewGuid();
        var monday = NextDayOfWeek(DayOfWeek.Monday);
        var nowUtc = new DateTimeOffset(monday.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var rule = WeeklyAvailabilityRule.Create(tutorId, DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(10, 0)).Value;
        var settings = Settings("Europe/Berlin", minNoticeHours: 0, maxBookingWindowDays: 1, slotMinutes: 60);

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");
        var localStart = monday.ToDateTime(new TimeOnly(9, 0));
        var expectedUtc = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localStart, DateTimeKind.Unspecified), timeZone), TimeSpan.Zero);

        var slots = AvailabilityCalculator.ComputeOpenSlotStarts([rule], [], settings, nowUtc);

        Assert.Single(slots);
        Assert.Equal(expectedUtc, slots[0]);
    }

    [Fact]
    public void ComputeOpenSlotStarts_OnDstSpringForward_SkipsInvalidSlotWithoutThrowing()
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        var year = DateTime.UtcNow.Year;
        var transitionDate = FindDstTransitionDate(timeZone, year, month: 3, invalid: true);
        var tutorId = Guid.NewGuid();
        // Spans the 02:00-03:00 gap that does not exist on the spring-forward day.
        var rule = WeeklyAvailabilityRule.Create(tutorId, transitionDate.DayOfWeek, new TimeOnly(1, 0), new TimeOnly(4, 0)).Value;
        var settings = Settings("America/New_York", minNoticeHours: 0, maxBookingWindowDays: 1, slotMinutes: 60);
        var nowUtc = LocalMidnightUtc(transitionDate, timeZone);

        var slots = AvailabilityCalculator.ComputeOpenSlotStarts([rule], [], settings, nowUtc);

        var localStarts = slots.Select(s => TimeZoneInfo.ConvertTimeFromUtc(s.UtcDateTime, timeZone))
            .Where(d => DateOnly.FromDateTime(d) == transitionDate)
            .Select(d => d.Hour)
            .OrderBy(h => h)
            .ToList();
        Assert.Equal([1, 3], localStarts);
    }

    [Fact]
    public void ComputeOpenSlotStarts_OnDstFallBack_DoesNotCreateDuplicateOrInvalidSlots()
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        var year = DateTime.UtcNow.Year;
        var transitionDate = FindDstTransitionDate(timeZone, year, month: 11, invalid: false);
        var tutorId = Guid.NewGuid();
        // Spans the 01:00-02:00 hour that occurs twice on the fall-back day.
        var rule = WeeklyAvailabilityRule.Create(tutorId, transitionDate.DayOfWeek, new TimeOnly(0, 0), new TimeOnly(3, 0)).Value;
        var settings = Settings("America/New_York", minNoticeHours: 0, maxBookingWindowDays: 1, slotMinutes: 60);
        var nowUtc = LocalMidnightUtc(transitionDate, timeZone);

        var slots = AvailabilityCalculator.ComputeOpenSlotStarts([rule], [], settings, nowUtc);

        var transitionDaySlots = slots
            .Where(s => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(s.UtcDateTime, timeZone)) == transitionDate)
            .ToList();
        Assert.Equal(3, transitionDaySlots.Count);
        Assert.Equal(transitionDaySlots.Count, transitionDaySlots.Distinct().Count());
    }

    [Fact]
    public void ComputeOpenSlotStarts_WithNonEvenlyDivisibleWindow_TruncatesRemainder()
    {
        var tutorId = Guid.NewGuid();
        var monday = NextDayOfWeek(DayOfWeek.Monday);
        var nowUtc = new DateTimeOffset(monday.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        // 90-minute window with 60-minute slots: only one full slot fits, the trailing 30
        // minutes are not offered.
        var rule = WeeklyAvailabilityRule.Create(tutorId, DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(10, 30)).Value;
        var settings = Settings("UTC", minNoticeHours: 0, maxBookingWindowDays: 1, slotMinutes: 60);

        var slots = AvailabilityCalculator.ComputeOpenSlotStarts([rule], [], settings, nowUtc);

        var mondaySlots = slots.Where(s => s.Date == nowUtc.Date).ToList();
        Assert.Single(mondaySlots);
        Assert.Equal(new DateTimeOffset(monday.ToDateTime(new TimeOnly(9, 0)), TimeSpan.Zero), mondaySlots[0]);
    }
}
