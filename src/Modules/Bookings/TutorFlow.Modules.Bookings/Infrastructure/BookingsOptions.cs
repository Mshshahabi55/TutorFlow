namespace TutorFlow.Modules.Bookings.Infrastructure;

// Options-bound, sourced from configuration ("Bookings" section) — ADR-0003 requires the hold
// TTL to be configurable, not a hardcoded constant; BOOKINGS_SPEC.md §3.1 requires the allowed
// trial durations to be configuration, not a hardcoded domain assumption. 25/50 and 10 minutes
// are the current product-intended defaults, not baked into the domain model or database.
public sealed class BookingsOptions
{
    public int HoldDurationMinutes { get; set; } = 10;

    public int[] AllowedTrialDurationsMinutes { get; set; } = [25, 50];

    /// <summary>How often <see cref="BookingHoldExpirySweepService"/> scans for lapsed holds —
    /// an INTENTIONAL ENGINEERING DECISION (not a Preply-sourced timing; Preply's real
    /// abandoned-checkout email delay is NOT VERIFIED, see ADR-0029). 60s keeps detection
    /// latency low without meaningfully loading the database; safely larger than the wall-clock
    /// runtime of any single existing test fixture, so it never fires mid-test by accident.</summary>
    public int HoldExpirySweepIntervalSeconds { get; set; } = 60;
}
