using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TutorFlow.Modules.Identity.Domain;
using TutorFlow.Modules.Identity.Infrastructure;
using RegisterResponse = TutorFlow.Modules.Identity.Features.Register.Register.SessionResponse;
using LoginResponse = TutorFlow.Modules.Identity.Features.Login.Login.SessionResponse;
using MeResponse = TutorFlow.Modules.Identity.Features.CurrentUser.GetCurrentUser.Response;
using BecomeTutorResponse = TutorFlow.Modules.Identity.Features.BecomeTutor.BecomeTutor.SessionResponse;
using ProfileResponse = TutorFlow.Modules.Identity.Features.Profile.GetProfile.Response;

namespace TutorFlow.Modules.Identity.Tests;

// The session cookie (ADR-0023) is Secure, which real browsers set/send over http://localhost
// (treated as a "potentially trustworthy" origin) but .NET's CookieContainer does not — it has
// no loopback exception and silently drops Secure cookies on a plain-http test request, which
// is what WebApplicationFactory's default client uses. HandleCookies=false disables that
// container so Set-Cookie survives on response.Headers, and CaptureSessionCookie replays it
// manually — this tests our server's cookie contract, not a .NET HttpClient quirk.
[Collection(IdentityHostCollection.Name)]
public class IdentityEndpointsTests(IdentityApiFixture fixture) : IClassFixture<IdentityApiFixture>
{
    private readonly HttpClient _client = fixture.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

    private static void CaptureSessionCookie(HttpResponseMessage response, HttpClient client)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            return;
        }

        var pair = cookies.First().Split(';')[0];
        client.DefaultRequestHeaders.Remove("Cookie");
        if (!pair.EndsWith('='))
        {
            client.DefaultRequestHeaders.Add("Cookie", pair);
        }
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsCreatedWithDefaultStudentRole()
    {
        var email = $"{Guid.NewGuid():N}@example.com";

        var response = await _client.PostAsJsonAsync(
            "/api/identity/register",
            new { email, password = "P@ssword123" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<RegisterResponse>(TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.Equal(email, body!.Email);
        Assert.Contains(RoleNames.Student, body.Roles);
    }

    [Fact]
    public async Task Register_AutoCreatesSession_MeSucceedsWithoutAnyExplicitAuth()
    {
        var email = $"{Guid.NewGuid():N}@example.com";

        var registerResponse = await _client.PostAsJsonAsync(
            "/api/identity/register",
            new { email, password = "P@ssword123" },
            TestContext.Current.CancellationToken);
        var registered = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>(TestContext.Current.CancellationToken);
        CaptureSessionCookie(registerResponse, _client);

        var meResponse = await _client.GetAsync("/api/identity/me", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
        var me = await meResponse.Content.ReadFromJsonAsync<MeResponse>(TestContext.Current.CancellationToken);
        Assert.Equal(registered!.UserId, me!.UserId);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsBadRequest()
    {
        var email = $"{Guid.NewGuid():N}@example.com";
        var payload = new { email, password = "P@ssword123" };
        await _client.PostAsJsonAsync("/api/identity/register", payload, TestContext.Current.CancellationToken);

        var response = await _client.PostAsJsonAsync("/api/identity/register", payload, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsSessionAndNeverTheRawToken()
    {
        var email = $"{Guid.NewGuid():N}@example.com";
        const string password = "P@ssword123";
        await _client.PostAsJsonAsync("/api/identity/register", new { email, password }, TestContext.Current.CancellationToken);

        var response = await _client.PostAsJsonAsync(
            "/api/identity/login",
            new { email, password },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var raw = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>(TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.Contains(RoleNames.Student, body!.Roles);
        Assert.DoesNotContain("accessToken", raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_WithValidCredentials_SessionCookieLetsMeSucceedWithoutAnyExplicitAuth()
    {
        var email = $"{Guid.NewGuid():N}@example.com";
        const string password = "P@ssword123";
        await _client.PostAsJsonAsync("/api/identity/register", new { email, password }, TestContext.Current.CancellationToken);

        var loginResponse = await _client.PostAsJsonAsync(
            "/api/identity/login",
            new { email, password },
            TestContext.Current.CancellationToken);
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(TestContext.Current.CancellationToken);
        CaptureSessionCookie(loginResponse, _client);

        var meResponse = await _client.GetAsync("/api/identity/me", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
        var me = await meResponse.Content.ReadFromJsonAsync<MeResponse>(TestContext.Current.CancellationToken);
        Assert.Equal(login!.UserId, me!.UserId);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        var email = $"{Guid.NewGuid():N}@example.com";
        await _client.PostAsJsonAsync(
            "/api/identity/register",
            new { email, password = "P@ssword123" },
            TestContext.Current.CancellationToken);

        var response = await _client.PostAsJsonAsync(
            "/api/identity/login",
            new { email, password = "WrongPassword1" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/identity/login",
            new { email = $"{Guid.NewGuid():N}@example.com", password = "P@ssword123" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithBearerHeaderToken_StillWorks()
    {
        var token = CreateTokenSignedWithFixtureKey(
            userId: Guid.NewGuid(),
            email: "header-auth@example.com",
            expires: DateTime.UtcNow.AddHours(1));

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/identity/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/identity/me", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithTamperedSignature_ReturnsUnauthorized()
    {
        var validToken = CreateTokenSignedWithFixtureKey(
            userId: Guid.NewGuid(),
            email: "tampered@example.com",
            expires: DateTime.UtcNow.AddHours(1));

        var segments = validToken.Split('.');
        var tamperedToken = $"{segments[0]}.{segments[1]}.{new string(segments[2].Reverse().ToArray())}";

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/identity/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tamperedToken);
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithExpiredToken_ReturnsUnauthorized()
    {
        var expiredToken = CreateTokenSignedWithFixtureKey(
            userId: Guid.NewGuid(),
            email: "expired@example.com",
            expires: DateTime.UtcNow.AddHours(-1));

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/identity/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithConcurrentDuplicateEmail_OnlyOneSucceeds()
    {
        var email = $"{Guid.NewGuid():N}@example.com";
        var payload = new { email, password = "P@ssword123" };

        var responses = await Task.WhenAll(
            _client.PostAsJsonAsync("/api/identity/register", payload, TestContext.Current.CancellationToken),
            _client.PostAsJsonAsync("/api/identity/register", payload, TestContext.Current.CancellationToken));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Logout_ClearsSessionCookie_MeThenReturnsUnauthorized()
    {
        var email = $"{Guid.NewGuid():N}@example.com";
        var registerResponse = await _client.PostAsJsonAsync(
            "/api/identity/register",
            new { email, password = "P@ssword123" },
            TestContext.Current.CancellationToken);
        CaptureSessionCookie(registerResponse, _client);
        Assert.Equal(
            HttpStatusCode.OK,
            (await _client.GetAsync("/api/identity/me", TestContext.Current.CancellationToken)).StatusCode);

        var logoutResponse = await _client.PostAsync("/api/identity/logout", content: null, TestContext.Current.CancellationToken);
        CaptureSessionCookie(logoutResponse, _client);

        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);
        var meAfterLogout = await _client.GetAsync("/api/identity/me", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, meAfterLogout.StatusCode);
    }

    [Fact]
    public async Task Logout_WithNoSession_StillReturnsNoContent()
    {
        var response = await _client.PostAsync("/api/identity/logout", content: null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private static string CreateTokenSignedWithFixtureKey(Guid userId, string email, DateTime expires)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = IdentityApiFixture.JwtIssuer,
            Audience = IdentityApiFixture.JwtAudience,
            Expires = expires,
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = userId.ToString(),
                [System.Security.Claims.ClaimTypes.NameIdentifier] = userId.ToString(),
                [JwtRegisteredClaimNames.Email] = email
            },
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(IdentityApiFixture.JwtKey)),
                SecurityAlgorithms.HmacSha256)
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    [Fact]
    public async Task User_CanHoldBothStudentAndTutorRoles_AndBothAppearOnSubsequentLogin()
    {
        var email = $"{Guid.NewGuid():N}@example.com";
        const string password = "P@ssword123";
        var registerResponse = await _client.PostAsJsonAsync(
            "/api/identity/register",
            new { email, password },
            TestContext.Current.CancellationToken);
        var registered = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>(TestContext.Current.CancellationToken);

        using (var scope = fixture.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByIdAsync(registered!.UserId.ToString());
            Assert.NotNull(user);
            var addToRoleResult = await userManager.AddToRoleAsync(user!, RoleNames.Tutor);
            Assert.True(addToRoleResult.Succeeded);
        }

        var loginResponse = await _client.PostAsJsonAsync(
            "/api/identity/login",
            new { email, password },
            TestContext.Current.CancellationToken);
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(TestContext.Current.CancellationToken);

        Assert.NotNull(login);
        Assert.Contains(RoleNames.Student, login!.Roles);
        Assert.Contains(RoleNames.Tutor, login.Roles);
    }

    [Fact]
    public async Task BecomeTutor_ForAStudent_GrantsTutorRole_AndKeepsStudent()
    {
        var email = $"{Guid.NewGuid():N}@example.com";
        var registerResponse = await _client.PostAsJsonAsync(
            "/api/identity/register",
            new { email, password = "P@ssword123" },
            TestContext.Current.CancellationToken);
        CaptureSessionCookie(registerResponse, _client);

        var response = await _client.PostAsync("/api/identity/me/become-tutor", content: null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<BecomeTutorResponse>(TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.Contains(RoleNames.Student, body!.Roles);
        Assert.Contains(RoleNames.Tutor, body.Roles);
    }

    [Fact]
    public async Task BecomeTutor_RefreshesTheSessionCookie_SoSubsequentRequestsSeeTheNewRole()
    {
        var email = $"{Guid.NewGuid():N}@example.com";
        var registerResponse = await _client.PostAsJsonAsync(
            "/api/identity/register",
            new { email, password = "P@ssword123" },
            TestContext.Current.CancellationToken);
        CaptureSessionCookie(registerResponse, _client);

        var becomeTutorResponse = await _client.PostAsync("/api/identity/me/become-tutor", content: null, TestContext.Current.CancellationToken);
        CaptureSessionCookie(becomeTutorResponse, _client);

        var meResponse = await _client.GetAsync("/api/identity/me", TestContext.Current.CancellationToken);
        var me = await meResponse.Content.ReadFromJsonAsync<MeResponse>(TestContext.Current.CancellationToken);

        Assert.Contains(RoleNames.Tutor, me!.Roles);
    }

    [Fact]
    public async Task BecomeTutor_CalledTwice_IsIdempotent()
    {
        var email = $"{Guid.NewGuid():N}@example.com";
        var registerResponse = await _client.PostAsJsonAsync(
            "/api/identity/register",
            new { email, password = "P@ssword123" },
            TestContext.Current.CancellationToken);
        CaptureSessionCookie(registerResponse, _client);
        await _client.PostAsync("/api/identity/me/become-tutor", content: null, TestContext.Current.CancellationToken);

        var response = await _client.PostAsync("/api/identity/me/become-tutor", content: null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<BecomeTutorResponse>(TestContext.Current.CancellationToken);
        Assert.Equal(1, body!.Roles.Count(r => r == RoleNames.Tutor));
    }

    [Fact]
    public async Task BecomeTutor_Unauthenticated_ReturnsUnauthorized()
    {
        var response = await _client.PostAsync("/api/identity/me/become-tutor", content: null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProfile_ForANewAccount_ReturnsNullNamesAndTheRealEmail()
    {
        var email = $"{Guid.NewGuid():N}@example.com";
        var registerResponse = await _client.PostAsJsonAsync(
            "/api/identity/register",
            new { email, password = "P@ssword123" },
            TestContext.Current.CancellationToken);
        CaptureSessionCookie(registerResponse, _client);

        var response = await _client.GetAsync("/api/identity/me/profile", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ProfileResponse>(TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.Null(body!.FirstName);
        Assert.Null(body.LastName);
        Assert.Equal(email, body.Email);
    }

    [Fact]
    public async Task UpdateProfile_SetsFirstAndLastName_AndGetProfileReflectsThem()
    {
        var email = $"{Guid.NewGuid():N}@example.com";
        var registerResponse = await _client.PostAsJsonAsync(
            "/api/identity/register",
            new { email, password = "P@ssword123" },
            TestContext.Current.CancellationToken);
        CaptureSessionCookie(registerResponse, _client);

        var updateResponse = await _client.PatchAsJsonAsync(
            "/api/identity/me/profile",
            new { firstName = "Ada", lastName = "Lovelace" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updateBody = await updateResponse.Content.ReadFromJsonAsync<ProfileResponse>(TestContext.Current.CancellationToken);
        Assert.Equal("Ada", updateBody!.FirstName);
        Assert.Equal("Lovelace", updateBody.LastName);

        var getResponse = await _client.GetAsync("/api/identity/me/profile", TestContext.Current.CancellationToken);
        var getBody = await getResponse.Content.ReadFromJsonAsync<ProfileResponse>(TestContext.Current.CancellationToken);
        Assert.Equal("Ada", getBody!.FirstName);
        Assert.Equal("Lovelace", getBody.LastName);
    }

    [Fact]
    public async Task UpdateProfile_WithOnlyFirstName_LeavesLastNameUnchanged()
    {
        var email = $"{Guid.NewGuid():N}@example.com";
        var registerResponse = await _client.PostAsJsonAsync(
            "/api/identity/register",
            new { email, password = "P@ssword123" },
            TestContext.Current.CancellationToken);
        CaptureSessionCookie(registerResponse, _client);

        await _client.PatchAsJsonAsync(
            "/api/identity/me/profile",
            new { firstName = "Ada", lastName = "Lovelace" },
            TestContext.Current.CancellationToken);

        var secondUpdate = await _client.PatchAsJsonAsync(
            "/api/identity/me/profile",
            new { firstName = "Grace" },
            TestContext.Current.CancellationToken);

        var body = await secondUpdate.Content.ReadFromJsonAsync<ProfileResponse>(TestContext.Current.CancellationToken);
        Assert.Equal("Grace", body!.FirstName);
        Assert.Equal("Lovelace", body.LastName);
    }

    [Fact]
    public async Task GetProfile_Unauthenticated_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/identity/me/profile", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_Unauthenticated_ReturnsUnauthorized()
    {
        var response = await _client.PatchAsJsonAsync(
            "/api/identity/me/profile",
            new { firstName = "Ada" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
