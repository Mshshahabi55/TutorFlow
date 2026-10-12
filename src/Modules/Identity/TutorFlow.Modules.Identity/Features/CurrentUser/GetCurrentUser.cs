using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using TutorFlow.Modules.Identity.Infrastructure;

namespace TutorFlow.Modules.Identity.Features.CurrentUser;

public static class GetCurrentUser
{
    public sealed record Response(Guid UserId, string Email, IReadOnlyCollection<string> Roles);

    public static Result<Response> FromPrincipal(ClaimsPrincipal principal)
    {
        var idClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = principal.FindFirstValue(JwtRegisteredClaimNames.Email) ?? principal.FindFirstValue(ClaimTypes.Email);

        if (idClaim is null || !Guid.TryParse(idClaim, out var userId) || email is null)
        {
            return Result.Failure<Response>("Unable to resolve the current user from the access token.");
        }

        var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();

        return Result.Success(new Response(userId, email, roles));
    }
}
