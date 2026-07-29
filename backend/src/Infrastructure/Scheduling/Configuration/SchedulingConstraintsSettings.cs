using Microsoft.Extensions.Options;

namespace TutorFlow.Infrastructure.Scheduling.Configuration;

// Bound from the "SchedulingConstraints" configuration section
// (docs/adr/ADR-025-... Addendum — Booking Notice & Horizon, Accepted
// 2026-07-29). Platform-wide, not per-Tutor — the owner's decision was
// explicit that these two limits are Admin-configured for the whole
// platform, matching the existing RateLimitingSettings/MeetingProviderSettings
// convention: ops can retune either value without a code change.
public sealed class SchedulingConstraintsSettings
{
    public const string SectionName = "SchedulingConstraints";

    public int MinimumBookingNoticeHours { get; init; } = 24;

    public int MaximumBookingHorizonDays { get; init; } = 90;
}

// Registered via .ValidateOnStart() (Infrastructure.DependencyInjection) —
// a misconfigured value (zero, negative, or a horizon shorter than the
// notice floor) would otherwise only surface as a confusing rejection of
// every booking attempt on the first real request. Failing at startup
// instead follows the same "fail fast" principle RateLimitingSettingsValidator
// already applies.
public sealed class SchedulingConstraintsSettingsValidator : IValidateOptions<SchedulingConstraintsSettings>
{
    public ValidateOptionsResult Validate(string? name, SchedulingConstraintsSettings options)
    {
        var failures = new List<string>();

        if (options.MinimumBookingNoticeHours <= 0)
        {
            failures.Add(
                $"SchedulingConstraints:MinimumBookingNoticeHours must be a positive integer (was {options.MinimumBookingNoticeHours}).");
        }

        if (options.MaximumBookingHorizonDays <= 0)
        {
            failures.Add(
                $"SchedulingConstraints:MaximumBookingHorizonDays must be a positive integer (was {options.MaximumBookingHorizonDays}).");
        }

        if (options.MinimumBookingNoticeHours > 0 && options.MaximumBookingHorizonDays > 0
            && TimeSpan.FromHours(options.MinimumBookingNoticeHours) >= TimeSpan.FromDays(options.MaximumBookingHorizonDays))
        {
            failures.Add(
                "SchedulingConstraints:MinimumBookingNoticeHours must be strictly less than " +
                "MaximumBookingHorizonDays converted to hours — otherwise no booking time would ever satisfy both.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
