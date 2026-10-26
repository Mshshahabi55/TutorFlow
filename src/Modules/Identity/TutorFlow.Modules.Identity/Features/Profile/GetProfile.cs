using Microsoft.AspNetCore.Identity;
using TutorFlow.Modules.Identity.Domain;
using TutorFlow.Modules.Identity.Infrastructure;

namespace TutorFlow.Modules.Identity.Features.Profile;

/// <summary>Deliberately separate from <c>CurrentUser.GetCurrentUser</c> (the existing, claims-only,
/// app-wide "am I logged in" endpoint used by <c>useAuth()</c>) — that endpoint resolves entirely
/// from JWT claims and is used broadly across the app; adding a DB read to it would change its
/// performance/staleness characteristics for every caller. This is a dedicated, DB-backed read for
/// the one place that actually needs First/Last name: the About-stage wizard.</summary>
public static class GetProfile
{
    public sealed record Response(string? FirstName, string? LastName, string Email);

    internal sealed class Handler(UserManager<ApplicationUser> userManager)
    {
        public async Task<Result<Response>> HandleAsync(Guid userId, CancellationToken ct)
        {
            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user is null)
            {
                return Result.Failure<Response>("Unable to resolve the current user.");
            }

            return Result.Success(new Response(user.FirstName, user.LastName, user.Email!));
        }
    }
}
