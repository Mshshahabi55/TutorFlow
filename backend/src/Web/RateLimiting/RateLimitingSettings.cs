namespace TutorFlow.Web.RateLimiting;

// Pure Web-layer/transport concern — HTTP request throttling is not a Domain
// or Application rule (PROJECT_CONSTITUTION.md: "Technology stack, hosting,
// infrastructure... are implementation decisions made later"), so this and
// its registration live entirely in TutorFlow.Web, never referenced from
// Application or Domain. Config-driven (RateLimitingRegistration reads this
// from the "RateLimiting" section), matching the existing house style for
// CORS/Meeting provider settings — ops can retune limits without a code
// change or redeploy of anything but configuration.
public sealed class RateLimitingSettings
{
    public const string SectionName = "RateLimiting";

    /// <summary>Applies to every request, partitioned by client IP.</summary>
    public RateLimitPolicySettings General { get; init; } = new() { PermitLimit = 100, WindowSeconds = 10 };

    /// <summary>
    /// Applies in addition to <see cref="General"/> on POST /auth/login only —
    /// the one unauthenticated, credential-guessing-shaped endpoint in this
    /// API. Deliberately not applied to the Admin-assisted password reset
    /// endpoint: that one already requires an authenticated Admin/Staff
    /// bearer token (Permission.ManageUserAccounts), so it is not a
    /// brute-force surface the same way an anonymous login attempt is.
    /// </summary>
    public RateLimitPolicySettings Auth { get; init; } = new() { PermitLimit = 5, WindowSeconds = 60 };
}

public sealed class RateLimitPolicySettings
{
    public int PermitLimit { get; init; }
    public int WindowSeconds { get; init; }
}
