using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TutorFlow.Modules.Identity.Domain;

namespace TutorFlow.Modules.Identity.Infrastructure;

internal sealed class TokenService(IOptions<JwtOptions> jwtOptions, TimeProvider clock) : ITokenService
{
    private static readonly JsonWebTokenHandler TokenHandler = new();

    public AccessToken CreateAccessToken(ApplicationUser user, IReadOnlyCollection<string> roles)
    {
        var options = jwtOptions.Value;
        var now = clock.GetUtcNow();
        var expiresAtUtc = now.AddMinutes(options.ExpirationMinutes);

        var claims = new Dictionary<string, object>
        {
            [JwtRegisteredClaimNames.Sub] = user.Id.ToString(),
            [ClaimTypes.NameIdentifier] = user.Id.ToString(),
            [JwtRegisteredClaimNames.Email] = user.Email!,
            [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString(),
            [ClaimTypes.Role] = roles.ToArray()
        };

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = options.Issuer,
            Audience = options.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAtUtc.UtcDateTime,
            Claims = claims,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key)),
                SecurityAlgorithms.HmacSha256)
        };

        var token = TokenHandler.CreateToken(descriptor);
        return new AccessToken(token, expiresAtUtc);
    }
}
