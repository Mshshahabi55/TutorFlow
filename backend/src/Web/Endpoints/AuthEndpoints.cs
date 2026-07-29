using Microsoft.AspNetCore.RateLimiting;
using TutorFlow.Application.Authorization;
using TutorFlow.Application.Identity.Commands;
using TutorFlow.Application.Identity.DTOs;
using TutorFlow.Application.Identity.Handlers;
using TutorFlow.Web.Authorization;
using TutorFlow.Web.DependencyInjection;

namespace TutorFlow.Web.Endpoints;

// Presentation-layer endpoints only, exactly like IdentityEndpoints.cs
// (docs/adr/ADR-010-api-boundary.md). Own file/tag, not merged into
// IdentityEndpoints — authentication is a distinct concern from Account
// registration/management, even though both are owned by the same Identity
// & Relationship bounded context (docs/adr/ADR-017-authentication-mechanism-decision.md).
public static class AuthEndpoints
{
    private const string Tag = "Auth";

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/login", async (
            LoginCommand command,
            LoginCommandHandler handler,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
            (await handler.Handle(command, cancellationToken))
                .ToApiResult(logger, nameof(LoginCommandHandler)))
            .WithApiResultMetadata<LoginResultDto>("Login", Tag, "Authenticates with an email and password, returning a bearer token.")
            // RateLimitingSettings.Auth — the one anonymous, credential-guessing-shaped
            // endpoint in this API; enforced in addition to the general
            // per-IP limit every other endpoint already gets.
            .RequireRateLimiting(RateLimitingRegistration.AuthPolicyName);

        app.MapPost("/auth/logout", async (
            LogoutRequest request,
            LogoutCommandHandler handler,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new LogoutCommand(request.Token), cancellationToken))
                .ToApiResult(logger, nameof(LogoutCommandHandler)))
            .WithApiResultMetadata("Logout", Tag, "Invalidates the presented bearer token.");

        // Admin-assisted reset only — no self-service recovery flow exists
        // (docs/adr/ADR-017-authentication-mechanism-decision.md). Coarse-grained
        // role check only (Permission.ManageUserAccounts, ADM-5); no
        // resource-instance qualifier applies — an Admin/Staff member may
        // reset any Account's password, per ADR-017's own "administrator
        // password reset" design (AUTHORIZATION_MATRIX.md §4.2, WP4 Priority 1).
        app.MapPost("/auth/accounts/{accountId:guid}/reset-password", async (
            Guid accountId,
            ResetPasswordRequest request,
            AdminResetPasswordCommandHandler handler,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new AdminResetPasswordCommand(accountId, request.NewPassword), cancellationToken))
                .ToApiResult(logger, nameof(AdminResetPasswordCommandHandler)))
            .WithApiResultMetadata("AdminResetPassword", Tag, "Resets an Account's password and revokes every active session for it.")
            .RequirePermission(Permission.ManageUserAccounts);

        return app;
    }

    // The token is taken from the request body, not the Authorization
    // header, so logout works the same way regardless of whether
    // AuthenticationMiddleware already resolved it into ICurrentUserProvider
    // for this request.
    internal sealed record LogoutRequest(string Token);

    internal sealed record ResetPasswordRequest(string NewPassword);
}
