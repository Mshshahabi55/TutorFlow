using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using TutorFlow.Modules.Identity.Domain;
using TutorFlow.Modules.Identity.Infrastructure;

namespace TutorFlow.Modules.Identity.Tests;

public class TokenServiceTests
{
    private readonly TokenService _sut;
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

    public TokenServiceTests()
    {
        var options = Options.Create(new JwtOptions
        {
            Issuer = "TutorFlow.Tests",
            Audience = "TutorFlow.Tests",
            Key = "unit-test-signing-key-at-least-32-bytes-long",
            ExpirationMinutes = 60
        });

        _sut = new TokenService(options, _clock);
    }

    [Fact]
    public void CreateAccessToken_IncludesUserIdEmailAndRoleClaims()
    {
        var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "student@example.com" };

        var token = _sut.CreateAccessToken(user, [RoleNames.Student, RoleNames.Tutor]);

        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(token.Value);

        Assert.Equal(user.Id.ToString(), jwt.GetClaim(ClaimTypes.NameIdentifier).Value);
        Assert.Equal(user.Email, jwt.GetClaim(JwtRegisteredClaimNames.Email).Value);
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Role && c.Value == RoleNames.Student);
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Role && c.Value == RoleNames.Tutor);
        Assert.Equal(_clock.GetUtcNow().AddMinutes(60), token.ExpiresAtUtc);
    }

    private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
