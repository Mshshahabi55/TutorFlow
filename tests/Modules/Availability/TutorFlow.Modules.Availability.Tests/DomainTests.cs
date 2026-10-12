using TutorFlow.Modules.Availability.Domain;

namespace TutorFlow.Modules.Availability.Tests;

public class WeeklyAvailabilityRuleTests
{
    [Fact]
    public void Create_WithStartBeforeEnd_Succeeds()
    {
        var result = WeeklyAvailabilityRule.Create(Guid.NewGuid(), DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_WithStartAfterEnd_Fails()
    {
        var result = WeeklyAvailabilityRule.Create(Guid.NewGuid(), DayOfWeek.Monday, new TimeOnly(17, 0), new TimeOnly(9, 0));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Create_WithEqualStartAndEnd_Fails()
    {
        var result = WeeklyAvailabilityRule.Create(Guid.NewGuid(), DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(9, 0));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void ValidateNoOverlaps_WithNonOverlappingRulesSameDay_Succeeds()
    {
        var tutorId = Guid.NewGuid();
        var rules = new[]
        {
            WeeklyAvailabilityRule.Create(tutorId, DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(12, 0)).Value,
            WeeklyAvailabilityRule.Create(tutorId, DayOfWeek.Monday, new TimeOnly(13, 0), new TimeOnly(17, 0)).Value
        };

        var result = WeeklyAvailabilityRule.ValidateNoOverlaps(rules);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void ValidateNoOverlaps_WithAdjacentTouchingRulesSameDay_Succeeds()
    {
        var tutorId = Guid.NewGuid();
        var rules = new[]
        {
            WeeklyAvailabilityRule.Create(tutorId, DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(12, 0)).Value,
            WeeklyAvailabilityRule.Create(tutorId, DayOfWeek.Monday, new TimeOnly(12, 0), new TimeOnly(15, 0)).Value
        };

        var result = WeeklyAvailabilityRule.ValidateNoOverlaps(rules);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void ValidateNoOverlaps_WithOverlappingRulesSameDay_Fails()
    {
        var tutorId = Guid.NewGuid();
        var rules = new[]
        {
            WeeklyAvailabilityRule.Create(tutorId, DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(12, 0)).Value,
            WeeklyAvailabilityRule.Create(tutorId, DayOfWeek.Monday, new TimeOnly(11, 0), new TimeOnly(14, 0)).Value
        };

        var result = WeeklyAvailabilityRule.ValidateNoOverlaps(rules);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void ValidateNoOverlaps_WithOverlappingRulesDifferentDays_Succeeds()
    {
        var tutorId = Guid.NewGuid();
        var rules = new[]
        {
            WeeklyAvailabilityRule.Create(tutorId, DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0)).Value,
            WeeklyAvailabilityRule.Create(tutorId, DayOfWeek.Tuesday, new TimeOnly(9, 0), new TimeOnly(17, 0)).Value
        };

        var result = WeeklyAvailabilityRule.ValidateNoOverlaps(rules);

        Assert.True(result.IsSuccess);
    }
}

public class TimeOffTests
{
    [Fact]
    public void Create_WithStartBeforeEnd_Succeeds()
    {
        var now = DateTimeOffset.UtcNow;
        var result = TimeOff.Create(Guid.NewGuid(), now, now.AddDays(1), "Vacation");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_WithStartAfterEnd_Fails()
    {
        var now = DateTimeOffset.UtcNow;
        var result = TimeOff.Create(Guid.NewGuid(), now.AddDays(1), now, null);

        Assert.False(result.IsSuccess);
    }
}

public class BookingSettingsTests
{
    private static readonly TimeProvider Clock = TimeProvider.System;

    [Fact]
    public void Create_WithValidValues_Succeeds()
    {
        var result = BookingSettings.Create(Guid.NewGuid(), "Europe/Berlin", 24, 30, 60, Clock);

        Assert.True(result.IsSuccess);
        Assert.Equal("Europe/Berlin", result.Value.TimeZoneId);
    }

    [Fact]
    public void Create_WithUnknownTimeZone_Fails()
    {
        var result = BookingSettings.Create(Guid.NewGuid(), "Not/ARealZone", 24, 30, 60, Clock);

        Assert.False(result.IsSuccess);
    }

    [Theory]
    [InlineData(-1, 30, 60)]
    [InlineData(24, 0, 60)]
    [InlineData(24, 30, 0)]
    [InlineData(24, 200, 60)]
    public void Create_WithOutOfBoundsValues_Fails(int minNoticeHours, int maxBookingWindowDays, int slotDurationMinutes)
    {
        var result = BookingSettings.Create(Guid.NewGuid(), "UTC", minNoticeHours, maxBookingWindowDays, slotDurationMinutes, Clock);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Apply_WithValidValues_UpdatesFieldsAndTimestamp()
    {
        var settings = BookingSettings.Create(Guid.NewGuid(), "UTC", 24, 30, 60, Clock).Value;

        var result = settings.Apply("Europe/Berlin", 12, 14, 30, Clock);

        Assert.True(result.IsSuccess);
        Assert.Equal("Europe/Berlin", settings.TimeZoneId);
        Assert.Equal(12, settings.MinNoticeHours);
        Assert.Equal(14, settings.MaxBookingWindowDays);
        Assert.Equal(30, settings.DefaultSlotDurationMinutes);
    }
}

public class TutorBookabilityTests
{
    private static readonly TimeProvider Clock = TimeProvider.System;

    [Fact]
    public void Create_SetsInitialState()
    {
        var bookability = TutorBookability.Create(Guid.NewGuid(), true, Clock);

        Assert.True(bookability.IsBookable);
    }

    [Fact]
    public void Apply_UpdatesStateAndTimestamp()
    {
        var bookability = TutorBookability.Create(Guid.NewGuid(), false, Clock);

        bookability.Apply(true, Clock);

        Assert.True(bookability.IsBookable);
    }
}
