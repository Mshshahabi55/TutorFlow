using Microsoft.AspNetCore.Http;

namespace TutorFlow.Modules.Identity.Infrastructure;

/// <summary>
/// httpOnly cookie transport for the JWT access token (ADR-0023) — the token is never returned
/// in a JSON response body and never read by frontend JS. Public so the Host can read
/// <see cref="Name"/> when wiring <c>JwtBearerEvents.OnMessageReceived</c>.
/// </summary>
public static class AuthCookie
{
    public const string Name = "TutorFlowAuth";

    public static void Issue(HttpContext httpContext, string accessToken, DateTimeOffset expiresAtUtc)
    {
        httpContext.Response.Cookies.Append(Name, accessToken, Options(expiresAtUtc));
    }

    public static void Clear(HttpContext httpContext)
    {
        httpContext.Response.Cookies.Delete(Name, Options(DateTimeOffset.UnixEpoch));
    }

    private static CookieOptions Options(DateTimeOffset expiresAtUtc) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Lax,
        Path = "/",
        Expires = expiresAtUtc
    };
}
