using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace TutorFlow.Infrastructure.Persistence;

// EF Core materializes/writes DateTime values whose Kind tag can be lost
// crossing a boundary this project doesn't fully control (a prior SQLite
// round-trip during a test, some future deserialization path) — Phase 2
// proved Npgsql throws outright on Kind=Unspecified rather than guessing
// (docs/phases/PHASE-02-REPORT.md Section 4; docs/phases/PHASE-025-REPORT.md
// Section 3). Every DateTime value this codebase ever produces is already
// semantically UTC — Phase 0.5's audit found zero uses of DateTime.Now/
// .Today anywhere in backend/src — so Kind=Unspecified here means "lost its
// tag," not "might be local," and relabeling it as Utc (not converting it)
// restores the value this codebase always meant.
//
// Kind=Local is a different, genuinely dangerous case: it is a real,
// non-UTC instant, and no code path in this project is known to ever
// produce one. Silently reinterpreting it as UTC would shift the instant
// it represents and hide a real bug instead of exposing one, so this
// throws (PROJECT_CONSTITUTION.md Engineering Principle 4: fail safely and
// visibly) rather than "fixing" it quietly.
internal static class UtcDateTime
{
    public static DateTime EnsureUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        DateTimeKind.Local => throw new InvalidOperationException(
            $"A DateTime with Kind=Local ('{value:O}') reached EF Core persistence. Every DateTime in " +
            "this codebase is expected to already be UTC (DateTime.UtcNow) — a genuinely local value " +
            "here indicates a real bug upstream, not a Kind tag lost in transit, and must not be " +
            "silently reinterpreted as UTC."),
        _ => value,
    };
}

internal sealed class UtcDateTimeValueConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeValueConverter()
        : base(v => UtcDateTime.EnsureUtc(v), v => UtcDateTime.EnsureUtc(v))
    {
    }
}

internal sealed class UtcNullableDateTimeValueConverter : ValueConverter<DateTime?, DateTime?>
{
    public UtcNullableDateTimeValueConverter()
        : base(
            v => v.HasValue ? UtcDateTime.EnsureUtc(v.Value) : v,
            v => v.HasValue ? UtcDateTime.EnsureUtc(v.Value) : v)
    {
    }
}
