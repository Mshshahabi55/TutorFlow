using TutorFlow.Modules.Availability.Domain;

namespace TutorFlow.Host.Tests;

/// <summary>
/// Regression coverage for A46 (<c>docs/OPEN_QUESTIONS.md</c>, <c>ADR-0022</c>).
///
/// This is the only test project in the repo that is entitled to prove this bug: every other
/// project either stays on the solution-wide default (<see cref="TutorFlow.Modules.Availability.Tests"/>
/// is the one deliberate exception — it explicitly sets <c>InvariantGlobalization=false</c> in
/// its own .csproj specifically so its DST tests can use real IANA IDs on Windows, which means
/// it can never reproduce this bug, by design). <c>TutorFlow.Host.Tests</c> runs with
/// <c>InvariantGlobalization=true</c>, the real setting shipped in <c>TutorFlow.Host</c>'s own
/// compiled runtimeconfig.json — the same condition that threw <c>TimeZoneNotFoundException</c>
/// for "Europe/London" when manually verified against the running Host on Windows.
/// </summary>
public class AvailabilityTimeZoneRegressionTests
{
    [Fact]
    public void Environment_SanityCheck_ThisTestHostActuallyRunsUnderInvariantGlobalization()
    {
        // Guards the premise every other test in this file relies on: that this specific test
        // project's compiled runtimeconfig.json really does carry
        // "System.Globalization.Invariant": true (confirmed directly by inspecting the built
        // runtimeconfig.json for TutorFlow.Host.Tests). If this ever starts failing, every other
        // test in this file would be silently testing nothing — surface that loudly, not quietly.
        var isInvariant = AppContext.TryGetSwitch("System.Globalization.Invariant", out var value) && value;
        Assert.True(isInvariant, "Expected this test host to run with InvariantGlobalization=true (see TutorFlow.Host.Tests' inherited Directory.Build.props setting).");
    }

    [Fact]
    public void PlainFindSystemTimeZoneById_FailsForRealIanaId_OnThisInvariantWindowsHost()
    {
        // Characterizes the actual bug (A46) in the exact process configuration that exhibits
        // it on Windows — proves the problem is real before proving the fix (TZConvert, used by
        // AvailabilityCalculator/BookingSettings below) resolves it.
        if (!OperatingSystem.IsWindows())
        {
            return; // IANA IDs resolve natively via tzdata on Linux/macOS regardless of InvariantGlobalization — nothing to characterize there.
        }

        Assert.Throws<TimeZoneNotFoundException>(() => TimeZoneInfo.FindSystemTimeZoneById("Europe/London"));
    }

    [Fact]
    public void BookingSettings_Create_WithRealIanaTimeZone_SucceedsUnderInvariantGlobalization()
    {
        var result = BookingSettings.Create(Guid.NewGuid(), "Europe/London", minNoticeHours: 0, maxBookingWindowDays: 7, defaultSlotDurationMinutes: 25, TimeProvider.System);

        Assert.True(result.IsSuccess, string.Join("; ", result.Errors));
    }

    [Fact]
    public void ComputeOpenSlotStarts_InWinter_UsesGmtOffsetForEuropeLondon()
    {
        var tutorId = Guid.NewGuid();
        // 2026-01-05 is a Monday, deep in GMT (UTC+0) — no DST in effect.
        var monday = new DateOnly(2026, 1, 5);
        var nowUtc = new DateTimeOffset(monday.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var rule = WeeklyAvailabilityRule.Create(tutorId, DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(10, 0)).Value;
        var settings = BookingSettings.Create(tutorId, "Europe/London", minNoticeHours: 0, maxBookingWindowDays: 1, defaultSlotDurationMinutes: 60, TimeProvider.System).Value;

        var slots = AvailabilityCalculator.ComputeOpenSlotStarts([rule], [], settings, nowUtc);

        // GMT is UTC+0, so a 09:00 local rule starts at 09:00 UTC.
        Assert.Contains(new DateTimeOffset(monday.ToDateTime(new TimeOnly(9, 0)), TimeSpan.Zero), slots);
    }

    [Fact]
    public void ComputeOpenSlotStarts_InSummer_UsesBstOffsetForEuropeLondon()
    {
        var tutorId = Guid.NewGuid();
        // 2026-07-06 is a Monday, deep in British Summer Time (UTC+1).
        var monday = new DateOnly(2026, 7, 6);
        var nowUtc = new DateTimeOffset(monday.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var rule = WeeklyAvailabilityRule.Create(tutorId, DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(10, 0)).Value;
        var settings = BookingSettings.Create(tutorId, "Europe/London", minNoticeHours: 0, maxBookingWindowDays: 1, defaultSlotDurationMinutes: 60, TimeProvider.System).Value;

        var slots = AvailabilityCalculator.ComputeOpenSlotStarts([rule], [], settings, nowUtc);

        // BST is UTC+1, so a 09:00 local rule starts at 08:00 UTC — proves DST, not just a raw
        // offset, is actually being applied (a naive UTC+0 assumption would produce 09:00 here).
        Assert.Contains(new DateTimeOffset(monday.ToDateTime(new TimeOnly(8, 0)), TimeSpan.Zero), slots);
        Assert.DoesNotContain(new DateTimeOffset(monday.ToDateTime(new TimeOnly(9, 0)), TimeSpan.Zero), slots);
    }
}
