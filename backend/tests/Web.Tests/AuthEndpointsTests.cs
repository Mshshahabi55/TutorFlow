using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Application.Common;
using TutorFlow.Application.Identity.Interfaces;
using TutorFlow.Domain.Common;
using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Web.Tests;

// End-to-end tests against the real HTTP pipeline, including
// AuthenticationMiddleware and AuthorizationMiddleware
// (docs/adr/ADR-017-authentication-mechanism-decision.md;
// docs/adr/ADR-003-authentication-and-authorization.md). Most of these
// tests still verify AuthenticationMiddleware the only externally
// observable way available before WP4: the audit trail an authenticated
// action produces. The reset-password tests below are the first to
// exercise AuthorizationMiddleware's real enforcement (WP4 Priority 1,
// AUTHORIZATION_MATRIX.md §4.2), since that is the first endpoint to call
// RequirePermission(...).
public class AuthEndpointsTests : IClassFixture<TutorFlowWebApplicationFactory>
{
    private readonly TutorFlowWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthEndpointsTests(TutorFlowWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static async Task<JsonElement> ReadBodyAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        return document.RootElement.Clone();
    }

    private static string UniqueEmail() => $"auth-test-{Guid.NewGuid():N}@example.com";

    private const string TestPassword = "Test-Password-123!";

    private async Task<(Guid TutorId, string Email)> RegisterTutorAsync()
    {
        var email = UniqueEmail();
        var response = await _client.PostAsJsonAsync("/tutors", new { Email = email, Password = TestPassword });
        var body = await ReadBodyAsync(response);
        return (body.GetProperty("value").GetProperty("tutorId").GetGuid(), email);
    }

    private async Task<string> LoginAsync(string email, string password = TestPassword)
    {
        var response = await _client.PostAsJsonAsync("/auth/login", new { Email = email, Password = password });
        response.EnsureSuccessStatusCode();
        var body = await ReadBodyAsync(response);
        Assert.True(body.GetProperty("isSuccess").GetBoolean());
        return body.GetProperty("value").GetProperty("token").GetString()!;
    }

    // AdminStaff has no self-registration endpoint by design
    // (docs/adr/ADR-017-authentication-mechanism-decision.md: "manually
    // seeded/provisioned only") — seeded directly through the repository,
    // exactly the mechanism ADR-017 itself describes, not a workaround.
    private async Task<string> SeedAndLoginAdminAsync()
    {
        var email = UniqueEmail();

        using (var scope = _factory.Services.CreateScope())
        {
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var adminRepository = scope.ServiceProvider.GetRequiredService<IAdminStaffRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var admin = AdminStaff.Create(EmailAddress.Of(email), PasswordHash.Of(passwordHasher.Hash(TestPassword)));
            await adminRepository.AddAsync(admin);
            await unitOfWork.SaveChangesAsync(new IAggregateRoot[] { admin });
        }

        return await LoginAsync(email);
    }

    private async Task<HttpResponseMessage> SendWithAuthAsync(HttpMethod method, string url, object? body, string? bearerToken)
    {
        using var request = new HttpRequestMessage(method, url);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        if (bearerToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }

        return await _client.SendAsync(request);
    }

    private async Task<Guid> DeclareAvailabilityAsync(Guid tutorId, string? bearerToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/availability-slots")
        {
            Content = JsonContent.Create(new
            {
                TutorId = tutorId,
                StartTimeUtc = DateTime.UtcNow.AddDays(1),
                Duration = TimeSpan.FromHours(1),
                DeliveryMode = 0, // Online
            }),
        };
        if (bearerToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }

        var response = await _client.SendAsync(request);
        var body = await ReadBodyAsync(response);
        return body.GetProperty("value").GetProperty("availabilitySlotId").GetGuid();
    }

    private async Task<HttpResponseMessage> BookSessionAsync(Guid availabilitySlotId, string? bearerToken) =>
        await SendWithAuthAsync(
            HttpMethod.Post,
            "/sessions",
            new { AvailabilitySlotId = availabilitySlotId, StudentId = Guid.NewGuid(), ParentGuardianId = (Guid?)null },
            bearerToken);

    [Fact]
    public async Task Login_with_correct_credentials_returns_a_token_and_role()
    {
        var (_, email) = await RegisterTutorAsync();

        var response = await _client.PostAsJsonAsync("/auth/login", new { Email = email, Password = TestPassword });

        response.EnsureSuccessStatusCode();
        var body = await ReadBodyAsync(response);
        Assert.True(body.GetProperty("isSuccess").GetBoolean());
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("value").GetProperty("token").GetString()));
        Assert.Equal("Tutor", body.GetProperty("value").GetProperty("role").GetString());
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_401_and_the_same_message_as_an_unknown_email()
    {
        var (_, email) = await RegisterTutorAsync();

        var wrongPasswordResponse = await _client.PostAsJsonAsync(
            "/auth/login", new { Email = email, Password = "WrongPassword1!" });
        var unknownEmailResponse = await _client.PostAsJsonAsync(
            "/auth/login", new { Email = UniqueEmail(), Password = TestPassword });

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPasswordResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownEmailResponse.StatusCode);

        var wrongPasswordBody = await ReadBodyAsync(wrongPasswordResponse);
        var unknownEmailBody = await ReadBodyAsync(unknownEmailResponse);
        Assert.Equal(
            wrongPasswordBody.GetProperty("error").GetProperty("message").GetString(),
            unknownEmailBody.GetProperty("error").GetProperty("message").GetString());
    }

    [Fact]
    public async Task A_bearer_token_from_login_authenticates_a_subsequent_request_end_to_end()
    {
        // Proves the full chain: login issues a token -> the Authorization
        // header carries it -> AuthenticationMiddleware resolves it back to
        // this Tutor's identity -> the resulting audit entry for an
        // authenticated action records that identity as the actor.
        var (tutorId, email) = await RegisterTutorAsync();
        var token = await LoginAsync(email);
        var adminToken = await SeedAndLoginAdminAsync();

        var slotId = await DeclareAvailabilityAsync(tutorId, token);

        var auditResponse = await SendWithAuthAsync(HttpMethod.Get, $"/audit-entries?subjectId={slotId}", body: null, adminToken);
        var auditBody = await ReadBodyAsync(auditResponse);
        var entry = auditBody.GetProperty("value").GetProperty("items").EnumerateArray().Single();
        Assert.Equal("AvailabilityDeclared", entry.GetProperty("action").GetString());
        Assert.Equal(tutorId.ToString(), entry.GetProperty("actorId").GetString());
        Assert.Equal("Tutor", entry.GetProperty("actorRole").GetString());
    }

    // BookSession is now Permission-gated (WP4 Priority 4,
    // AUTHORIZATION_MATRIX.md §4.3), so an unauthenticated or revoked-token
    // request is rejected by AuthorizationMiddleware before Domain ever
    // runs — there is no longer any audited domain event to inspect for a
    // null actor (only 6 event types are audited at all; see
    // AuditDomainEventHandler, and every one of them now sits behind a
    // protected endpoint). The 401 response itself, carrying
    // AuthenticationMiddleware's/AuthorizationMiddleware's own
    // "Authorization.Unauthenticated" code, is now the direct, first-class
    // proof that no identity was resolved for this request — a strictly
    // stronger and simpler proof than the old indirect audit-log inference.
    [Fact]
    public async Task A_request_with_no_token_is_rejected_before_any_effect()
    {
        var (tutorId, email) = await RegisterTutorAsync();
        var token = await LoginAsync(email);
        var slotId = await DeclareAvailabilityAsync(tutorId, token);

        var response = await BookSessionAsync(slotId, bearerToken: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await ReadBodyAsync(response);
        Assert.Equal("Authorization.Unauthenticated", body.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task Logout_invalidates_the_token_so_a_later_request_is_no_longer_authenticated()
    {
        var (tutorId, email) = await RegisterTutorAsync();
        var token = await LoginAsync(email);
        var slotId = await DeclareAvailabilityAsync(tutorId, token);

        var logoutResponse = await _client.PostAsJsonAsync("/auth/logout", new { Token = token });
        logoutResponse.EnsureSuccessStatusCode();

        // The revoked token no longer authenticates anything — BookSession,
        // now Permission-gated (WP4 Priority 4), rejects it directly.
        var response = await BookSessionAsync(slotId, token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await ReadBodyAsync(response);
        Assert.Equal("Authorization.Unauthenticated", body.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task Login_is_recorded_in_the_audit_trail_for_both_success_and_failure()
    {
        var (tutorId, email) = await RegisterTutorAsync();
        var adminToken = await SeedAndLoginAdminAsync();

        await _client.PostAsJsonAsync("/auth/login", new { Email = email, Password = TestPassword });
        await _client.PostAsJsonAsync("/auth/login", new { Email = email, Password = "WrongPassword1!" });

        var auditResponse = await SendWithAuthAsync(HttpMethod.Get, $"/audit-entries?subjectId={tutorId}&pageSize=50", body: null, adminToken);
        var auditBody = await ReadBodyAsync(auditResponse);
        var actions = auditBody.GetProperty("value").GetProperty("items")
            .EnumerateArray()
            .Select(e => e.GetProperty("action").GetString())
            .ToList();

        Assert.Contains("LoginSucceeded", actions);
        Assert.Contains("LoginFailed", actions);
    }

    [Fact]
    public async Task Repeated_wrong_passwords_lock_the_account_and_the_correct_password_then_fails_too()
    {
        var (_, email) = await RegisterTutorAsync();

        HttpResponseMessage lastResponse = null!;
        for (var i = 0; i < 5; i++) // Account.MaxFailedLoginAttempts
        {
            lastResponse = await _client.PostAsJsonAsync(
                "/auth/login", new { Email = email, Password = "WrongPassword1!" });
        }

        Assert.Equal(HttpStatusCode.Unauthorized, lastResponse.StatusCode);
        var lockedBody = await ReadBodyAsync(lastResponse);
        Assert.Equal("LoginCommand.AccountLocked", lockedBody.GetProperty("error").GetProperty("code").GetString());

        var correctPasswordResponse = await _client.PostAsJsonAsync(
            "/auth/login", new { Email = email, Password = TestPassword });
        var correctPasswordBody = await ReadBodyAsync(correctPasswordResponse);
        Assert.Equal(
            "LoginCommand.AccountLocked", correctPasswordBody.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task Reset_password_requires_authentication()
    {
        var (tutorId, _) = await RegisterTutorAsync();

        var response = await SendWithAuthAsync(
            HttpMethod.Post, $"/auth/accounts/{tutorId}/reset-password", new { NewPassword = "Whatever-Password-1!" }, bearerToken: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await ReadBodyAsync(response);
        Assert.Equal("Authorization.Unauthenticated", body.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task Reset_password_is_forbidden_for_a_non_admin_role()
    {
        var (tutorId, _) = await RegisterTutorAsync();
        var (_, callerEmail) = await RegisterTutorAsync();
        var tutorToken = await LoginAsync(callerEmail);

        var response = await SendWithAuthAsync(
            HttpMethod.Post, $"/auth/accounts/{tutorId}/reset-password", new { NewPassword = "Whatever-Password-1!" }, tutorToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await ReadBodyAsync(response);
        Assert.Equal("Authorization.Forbidden", body.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task Admin_reset_password_invalidates_every_existing_session_immediately()
    {
        var (tutorId, email) = await RegisterTutorAsync();
        var token = await LoginAsync(email);
        var adminToken = await SeedAndLoginAdminAsync();
        var slotId = await DeclareAvailabilityAsync(tutorId, token);
        const string newPassword = "Brand-New-Password-456!";

        var resetResponse = await SendWithAuthAsync(
            HttpMethod.Post, $"/auth/accounts/{tutorId}/reset-password", new { NewPassword = newPassword }, adminToken);
        resetResponse.EnsureSuccessStatusCode();

        // The old token no longer authenticates anything — BookSession, now
        // Permission-gated (WP4 Priority 4), rejects it directly.
        var bookResponse = await BookSessionAsync(slotId, token);
        Assert.Equal(HttpStatusCode.Unauthorized, bookResponse.StatusCode);

        // ...but the new password logs in successfully.
        var newToken = await LoginAsync(email, newPassword);
        Assert.False(string.IsNullOrWhiteSpace(newToken));
    }
}
