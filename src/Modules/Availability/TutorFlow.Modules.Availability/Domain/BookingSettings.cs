using TimeZoneConverter;

namespace TutorFlow.Modules.Availability.Domain;

public sealed class BookingSettings
{
    // Structural sanity bounds only — not an approved platform policy. Exact min-notice /
    // max-booking-window / slot-duration business rules remain open (see docs/OPEN_QUESTIONS.md).
    private const int MinNoticeHoursLowerBound = 0;
    private const int MinNoticeHoursUpperBound = 720;
    private const int MaxBookingWindowDaysLowerBound = 1;
    private const int MaxBookingWindowDaysUpperBound = 180;
    private const int SlotDurationMinutesLowerBound = 15;
    private const int SlotDurationMinutesUpperBound = 240;

    private BookingSettings()
    {
    }

    public Guid TutorUserId { get; private init; }

    public string TimeZoneId { get; private set; } = "UTC";

    public int MinNoticeHours { get; private set; }

    public int MaxBookingWindowDays { get; private set; }

    public int DefaultSlotDurationMinutes { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static Result<BookingSettings> Create(
        Guid tutorUserId,
        string timeZoneId,
        int minNoticeHours,
        int maxBookingWindowDays,
        int defaultSlotDurationMinutes,
        TimeProvider clock)
    {
        var settings = new BookingSettings { TutorUserId = tutorUserId };
        var result = settings.Apply(timeZoneId, minNoticeHours, maxBookingWindowDays, defaultSlotDurationMinutes, clock);
        return result.IsSuccess ? Result.Success(settings) : Result.Failure<BookingSettings>([.. result.Errors]);
    }

    public Result Apply(
        string timeZoneId,
        int minNoticeHours,
        int maxBookingWindowDays,
        int defaultSlotDurationMinutes,
        TimeProvider clock)
    {
        List<string> errors = [];

        if (!TryValidateTimeZone(timeZoneId))
        {
            errors.Add($"'{timeZoneId}' is not a recognized time zone identifier.");
        }

        if (minNoticeHours < MinNoticeHoursLowerBound || minNoticeHours > MinNoticeHoursUpperBound)
        {
            errors.Add($"Minimum notice hours must be between {MinNoticeHoursLowerBound} and {MinNoticeHoursUpperBound}.");
        }

        if (maxBookingWindowDays < MaxBookingWindowDaysLowerBound || maxBookingWindowDays > MaxBookingWindowDaysUpperBound)
        {
            errors.Add($"Maximum booking window days must be between {MaxBookingWindowDaysLowerBound} and {MaxBookingWindowDaysUpperBound}.");
        }

        if (defaultSlotDurationMinutes < SlotDurationMinutesLowerBound || defaultSlotDurationMinutes > SlotDurationMinutesUpperBound)
        {
            errors.Add($"Default slot duration minutes must be between {SlotDurationMinutesLowerBound} and {SlotDurationMinutesUpperBound}.");
        }

        if (errors.Count > 0)
        {
            return Result.Failure([.. errors]);
        }

        TimeZoneId = timeZoneId;
        MinNoticeHours = minNoticeHours;
        MaxBookingWindowDays = maxBookingWindowDays;
        DefaultSlotDurationMinutes = defaultSlotDurationMinutes;
        UpdatedAtUtc = clock.GetUtcNow();
        return Result.Success();
    }

    // TZConvert, not TimeZoneInfo.FindSystemTimeZoneById/TryFindSystemTimeZoneById (A46): with
    // InvariantGlobalization enabled (Directory.Build.props), Windows loses .NET 6+'s ICU-based
    // IANA-ID resolution and falls back to registry-only lookup, so a real IANA ID like
    // "Europe/London" would be rejected here as "not recognized" even though it's perfectly
    // valid — this was the very first place a tutor entering a real time zone during onboarding
    // would have hit the bug. TZConvert's own Try-pattern avoids exception-driven control flow
    // for an expected "not a valid time zone" input, consistent with this module's Result
    // pattern.
    private static bool TryValidateTimeZone(string timeZoneId) => TZConvert.TryGetTimeZoneInfo(timeZoneId, out _);
}
