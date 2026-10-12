using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace TutorFlow.Host;

/// <summary>
/// Reads a single integer configuration value eagerly, at startup — used by the auth
/// rate-limiting policies in <c>Program.cs</c> so a malformed, zero, or negative value fails the
/// application immediately (the same moment <c>JwtOptions</c>' own <c>ValidateOnStart</c> would
/// fail it), instead of only surfacing lazily as a 500 on the first real request from whichever
/// client IP happens to be the first new rate-limiter partition.
/// </summary>
public static class RateLimitingConfigurationReader
{
    public static int ReadPositiveIntOrDefault(IConfiguration configuration, string key, int defaultValue)
    {
        var raw = configuration[key];
        if (raw is null)
        {
            return defaultValue;
        }

        if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) || value <= 0)
        {
            throw new InvalidOperationException(
                $"Configuration value '{key}' must be a positive integer, but was '{raw}'.");
        }

        return value;
    }
}
