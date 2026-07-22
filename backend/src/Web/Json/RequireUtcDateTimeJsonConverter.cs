using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TutorFlow.Web.Json;

// Closes a gap Phase 2.5's UtcDateTimeValueConverter deliberately left open
// (docs/phases/PHASE-03-REPORT.md Task 4): that converter relabels
// Kind=Unspecified as Utc at the EF Core boundary, which is correct for
// values this codebase itself produced, but it means the *API* boundary
// would otherwise silently accept a client-sent timestamp with no "Z" or
// offset and treat it as UTC — a silent 3.5-hour error for a Tehran caller.
//
// System.Text.Json's own default DateTime converter also has a second,
// independently dangerous gap: a timestamp with an explicit numeric offset
// that isn't "Z" (e.g. "+03:30") deserializes with Kind=Local, converted
// against *this process's own* system timezone — not the offset in the
// string. Two servers in different timezones would silently compute two
// different instants from the identical request body. Parsing every
// incoming value through DateTimeOffset instead — which is anchored to the
// offset actually present in the string, never the host's — makes the
// result deterministic regardless of where this API happens to run.
internal sealed partial class RequireUtcDateTimeJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var raw = reader.GetString();
        return DateTime.SpecifyKind(ParseRequiredUtc(raw), DateTimeKind.Utc);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }

    internal static DateTime ParseRequiredUtc(string? raw)
    {
        if (raw is null || !HasExplicitUtcDesignator(raw))
        {
            throw new JsonException(
                $"'{raw}' is not a valid timestamp. Provide an ISO 8601 date/time with an explicit " +
                "UTC designator ('Z') or numeric offset, e.g. 2026-08-01T14:00:00Z.");
        }

        if (!DateTimeOffset.TryParse(
            raw,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsed))
        {
            throw new JsonException($"'{raw}' is not a valid ISO 8601 date/time.");
        }

        return parsed.UtcDateTime;
    }

    // A trailing "Z" or a numeric "+hh:mm"/"-hh:mm" offset. The date
    // portion's own hyphens ("2026-08-01...") never match this anchored
    // end-of-string pattern.
    private static bool HasExplicitUtcDesignator(string raw) =>
        raw.EndsWith("Z", StringComparison.Ordinal) || OffsetSuffix().IsMatch(raw);

    [System.Text.RegularExpressions.GeneratedRegex(@"[+-]\d{2}:\d{2}$")]
    private static partial System.Text.RegularExpressions.Regex OffsetSuffix();
}

internal sealed class RequireUtcNullableDateTimeJsonConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        var raw = reader.GetString();
        return DateTime.SpecifyKind(RequireUtcDateTimeJsonConverter.ParseRequiredUtc(raw), DateTimeKind.Utc);
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc));
    }
}
