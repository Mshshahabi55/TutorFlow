using TutorFlow.Web.Json;

namespace TutorFlow.Web.Tests.Json;

// Unit-level coverage of RequireUtcDateTimeJsonConverter.ParseRequiredUtc,
// complementing SchedulingEndpointsTests' end-to-end proof that this
// converter is actually wired into the real HTTP pipeline
// (docs/phases/PHASE-03-REPORT.md Task 4).
public class RequireUtcDateTimeJsonConverterTests
{
    [Fact]
    public void Accepts_a_trailing_Z_and_returns_Kind_Utc()
    {
        var result = RequireUtcDateTimeJsonConverter.ParseRequiredUtc("2026-08-01T14:00:00Z");

        Assert.Equal(DateTimeKind.Utc, result.Kind);
        Assert.Equal(new DateTime(2026, 8, 1, 14, 0, 0, DateTimeKind.Utc), result);
    }

    [Fact]
    public void Accepts_fractional_seconds_with_a_trailing_Z()
    {
        var result = RequireUtcDateTimeJsonConverter.ParseRequiredUtc("2026-08-01T14:00:00.1234567Z");

        Assert.Equal(DateTimeKind.Utc, result.Kind);
    }

    [Theory]
    [InlineData("2026-08-01T17:30:00+03:30", "2026-08-01T14:00:00Z")] // Tehran offset
    [InlineData("2026-08-01T14:00:00+00:00", "2026-08-01T14:00:00Z")] // zero offset, not "Z"
    [InlineData("2026-08-01T09:00:00-05:00", "2026-08-01T14:00:00Z")] // negative offset
    public void Accepts_an_explicit_numeric_offset_and_converts_to_the_correct_instant(
        string raw, string expectedUtcIso)
    {
        var result = RequireUtcDateTimeJsonConverter.ParseRequiredUtc(raw);

        Assert.Equal(DateTimeKind.Utc, result.Kind);
        Assert.Equal(DateTime.Parse(expectedUtcIso).ToUniversalTime(), result);
    }

    [Theory]
    [InlineData("2026-08-01T14:00:00")] // no designator at all
    [InlineData("2026-08-01")] // date only
    [InlineData(null)]
    [InlineData("not-a-timestamp")]
    public void Rejects_a_timestamp_without_an_explicit_UTC_designator_or_offset(string? raw)
    {
        Assert.Throws<System.Text.Json.JsonException>(() => RequireUtcDateTimeJsonConverter.ParseRequiredUtc(raw));
    }
}
