namespace TutorFlow.Modules.Identity.Infrastructure;

/// <summary>
/// Backend mirror of the frontend's <c>sanitizeReturnTo</c> (apps/web/src/features/auth/returnTo.ts)
/// — same rule (same-origin, path-relative only), applied where the external-login callback must
/// build a same-origin frontend redirect itself, server-side, from a value that round-trips
/// through an OAuth provider and is never trustworthy just because this app's own links always
/// construct a safe one.
/// </summary>
public static class ExternalLoginReturnTo
{
    public static string Sanitize(string? raw)
    {
        if (string.IsNullOrEmpty(raw))
        {
            return "/";
        }

        if (!raw.StartsWith('/'))
        {
            return "/";
        }

        if (raw.StartsWith("//", StringComparison.Ordinal) || raw.StartsWith("/\\", StringComparison.Ordinal))
        {
            return "/";
        }

        return raw;
    }
}
