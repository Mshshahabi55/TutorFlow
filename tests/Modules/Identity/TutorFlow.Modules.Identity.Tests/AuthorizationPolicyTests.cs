using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Modules.Identity.Infrastructure;

namespace TutorFlow.Modules.Identity.Tests;

[Collection(IdentityHostCollection.Name)]
public class AuthorizationPolicyTests(IdentityApiFixture fixture) : IClassFixture<IdentityApiFixture>
{
    [Fact]
    public async Task StudentOnlyPolicy_WithStudentRole_Succeeds()
    {
        using var scope = fixture.Services.CreateScope();
        var authorizationService = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
        var principal = CreatePrincipal(RoleNames.Student);

        var result = await authorizationService.AuthorizeAsync(principal, PolicyNames.StudentOnly);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task StudentOnlyPolicy_WithoutStudentRole_Fails()
    {
        using var scope = fixture.Services.CreateScope();
        var authorizationService = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
        var principal = CreatePrincipal(RoleNames.Tutor);

        var result = await authorizationService.AuthorizeAsync(principal, PolicyNames.StudentOnly);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task TutorOnlyPolicy_WithTutorRole_Succeeds()
    {
        using var scope = fixture.Services.CreateScope();
        var authorizationService = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
        var principal = CreatePrincipal(RoleNames.Tutor);

        var result = await authorizationService.AuthorizeAsync(principal, PolicyNames.TutorOnly);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task TutorOnlyPolicy_WithoutTutorRole_Fails()
    {
        using var scope = fixture.Services.CreateScope();
        var authorizationService = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
        var principal = CreatePrincipal(RoleNames.Student);

        var result = await authorizationService.AuthorizeAsync(principal, PolicyNames.TutorOnly);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task BothPolicies_WithDualRolePrincipal_BothSucceed()
    {
        using var scope = fixture.Services.CreateScope();
        var authorizationService = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
        var principal = CreatePrincipal(RoleNames.Student, RoleNames.Tutor);

        var studentResult = await authorizationService.AuthorizeAsync(principal, PolicyNames.StudentOnly);
        var tutorResult = await authorizationService.AuthorizeAsync(principal, PolicyNames.TutorOnly);

        Assert.True(studentResult.Succeeded);
        Assert.True(tutorResult.Succeeded);
    }

    private static ClaimsPrincipal CreatePrincipal(params string[] roles)
    {
        var claims = roles.Select(r => new Claim(ClaimTypes.Role, r)).ToList();
        claims.Add(new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()));
        var identity = new ClaimsIdentity(claims, authenticationType: "TestAuth");
        return new ClaimsPrincipal(identity);
    }
}
