using TutorFlow.Application.Common;
using TutorFlow.Application.Identity.Interfaces;
using TutorFlow.Domain.Common;

namespace TutorFlow.Web.Middleware;

// Authentication only — establishes ICurrentUserProvider's identity from a
// bearer token, if one is presented and valid. Makes no permission decision
// (docs/adr/ADR-017-authentication-mechanism-decision.md; ADR-003:
// authentication and authorization are separate). A missing, malformed,
// expired, or revoked token leaves the request unauthenticated rather than
// rejecting it outright — endpoint-level authorization enforcement is a
// separate, not-yet-built concern (Launch Preparation Priority 2).
internal sealed class AuthenticationMiddleware
{
    private const string BearerPrefix = "Bearer ";

    private readonly RequestDelegate _next;

    public AuthenticationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IAuthTokenRepository authTokenRepository,
        ITokenGenerator tokenGenerator,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork,
        ICurrentUserWriter currentUserProvider)
    {
        var authorizationHeader = context.Request.Headers.Authorization.ToString();
        if (authorizationHeader.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var rawToken = authorizationHeader[BearerPrefix.Length..].Trim();
            if (rawToken.Length > 0)
            {
                var tokenHash = tokenGenerator.Hash(rawToken);
                var token = await authTokenRepository.GetByTokenHashAsync(tokenHash, context.RequestAborted);
                var now = dateTimeProvider.UtcNow;

                if (token is not null && token.IsValid(now))
                {
                    // Sliding expiration: every authenticated request that
                    // presents a still-valid token extends it, so an active
                    // session never expires mid-use.
                    token.ExtendExpiry(now);
                    await unitOfWork.SaveChangesAsync(new IAggregateRoot[] { token }, context.RequestAborted);

                    currentUserProvider.SetAuthenticated(token.AccountId.Value.ToString(), token.Role);
                }
            }
        }

        await _next(context);
    }
}
