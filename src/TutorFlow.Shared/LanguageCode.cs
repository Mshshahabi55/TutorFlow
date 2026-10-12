namespace TutorFlow.Shared;

/// <summary>
/// ISO 639-1 language code validation/normalization — a primitive, not a wrapped value type
/// (ADR-0020). Entities store the plain, lowercased <c>string</c> directly (same convention as
/// <c>HourlyRateCurrency</c>'s plain 3-char string for ISO 4217), not a custom EF-mapped struct.
/// </summary>
public static class LanguageCode
{
    public static bool IsValid(string? value) =>
        value is { Length: 2 } && value[0] is >= 'a' and <= 'z' or >= 'A' and <= 'Z'
                                && value[1] is >= 'a' and <= 'z' or >= 'A' and <= 'Z';

    public static string Normalize(string value) => value.Trim().ToLowerInvariant();
}
