using TutorFlow.Modules.Identity.Domain;

namespace TutorFlow.Modules.Identity.Infrastructure;

public interface ITokenService
{
    AccessToken CreateAccessToken(ApplicationUser user, IReadOnlyCollection<string> roles);
}

public sealed record AccessToken(string Value, DateTimeOffset ExpiresAtUtc);
