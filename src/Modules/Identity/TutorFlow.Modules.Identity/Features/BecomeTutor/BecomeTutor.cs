using Microsoft.AspNetCore.Identity;
using TutorFlow.Modules.Identity.Domain;
using TutorFlow.Modules.Identity.Infrastructure;

namespace TutorFlow.Modules.Identity.Features.BecomeTutor;

/// <summary>
/// The only way any account ever gains the <see cref="RoleNames.Tutor"/> role (ADR-0026) —
/// <c>Register</c> only ever assigns <see cref="RoleNames.Student"/>. Self-service, idempotent,
/// Identity-owned (not <c>Tutors</c>-owned) because "who may hold which role" is Identity's own
/// concern, the same place <c>Login</c>/<c>Register</c> already issue sessions.
/// </summary>
public static class BecomeTutor
{
    public sealed record Response(
        Guid UserId,
        string Email,
        IReadOnlyCollection<string> Roles,
        string AccessToken,
        DateTimeOffset ExpiresAtUtc);

    public sealed record SessionResponse(
        Guid UserId,
        string Email,
        IReadOnlyCollection<string> Roles,
        DateTimeOffset ExpiresAtUtc);

    internal sealed class Handler(UserManager<ApplicationUser> userManager, ITokenService tokenService)
    {
        public async Task<Result<Response>> HandleAsync(Guid userId, CancellationToken ct)
        {
            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user is null)
            {
                return Result.Failure<Response>("Unable to resolve the current user.");
            }

            var roles = (await userManager.GetRolesAsync(user)).ToList();
            if (!roles.Contains(RoleNames.Tutor))
            {
                var addResult = await userManager.AddToRoleAsync(user, RoleNames.Tutor);
                if (!addResult.Succeeded)
                {
                    return Result.Failure<Response>(addResult.Errors.Select(e => e.Description).ToArray());
                }

                roles.Add(RoleNames.Tutor);
            }

            var accessToken = tokenService.CreateAccessToken(user, roles);

            return Result.Success(new Response(user.Id, user.Email!, roles, accessToken.Value, accessToken.ExpiresAtUtc));
        }
    }
}
