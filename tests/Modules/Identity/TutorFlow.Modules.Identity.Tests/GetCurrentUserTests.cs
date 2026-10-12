using System.Security.Claims;
using TutorFlow.Modules.Identity.Features.CurrentUser;
using TutorFlow.Modules.Identity.Infrastructure;

namespace TutorFlow.Modules.Identity.Tests;

public class GetCurrentUserTests
{
    [Fact]
    public void FromPrincipal_WithValidClaims_ReturnsSuccessWithUserIdEmailAndRoles()
    {
        var userId = Guid.NewGuid();
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, "student@example.com"),
            new Claim(ClaimTypes.Role, RoleNames.Student),
            new Claim(ClaimTypes.Role, RoleNames.Tutor)
        ]);
        var principal = new ClaimsPrincipal(identity);

        var result = GetCurrentUser.FromPrincipal(principal);

        Assert.True(result.IsSuccess);
        Assert.Equal(userId, result.Value.UserId);
        Assert.Equal("student@example.com", result.Value.Email);
        Assert.Contains(RoleNames.Student, result.Value.Roles);
        Assert.Contains(RoleNames.Tutor, result.Value.Roles);
    }

    [Fact]
    public void FromPrincipal_WithoutUserIdClaim_ReturnsFailure()
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Email, "student@example.com")]);
        var principal = new ClaimsPrincipal(identity);

        var result = GetCurrentUser.FromPrincipal(principal);

        Assert.False(result.IsSuccess);
    }
}
