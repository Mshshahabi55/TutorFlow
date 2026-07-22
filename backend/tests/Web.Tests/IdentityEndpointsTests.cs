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

// End-to-end tests against the real HTTP pipeline (WebApplicationFactory).
// These verify only Presentation-layer orchestration — that a request
// reaches the right endpoint, is mapped into the existing Command, and the
// existing handler's Result is returned unchanged. No business rule already
// covered by Domain.Tests/Application.Tests is re-verified here. Approve/
// Suspend now also exercise AuthorizationMiddleware's real enforcement
// (WP4 Priority 2, AUTHORIZATION_MATRIX.md §4.1), since those are
// coarse-grained-protected endpoints as of this priority.
public class IdentityEndpointsTests : IClassFixture<TutorFlowWebApplicationFactory>, IAsyncLifetime
{
    private readonly TutorFlowWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public IdentityEndpointsTests(TutorFlowWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private static async Task<JsonElement> ReadBodyAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        return document.RootElement.Clone();
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

        var loginResponse = await _client.PostAsJsonAsync("/auth/login", new { Email = email, Password = TestPassword });
        var loginBody = await ReadBodyAsync(loginResponse);
        return loginBody.GetProperty("value").GetProperty("token").GetString()!;
    }

    private async Task<HttpResponseMessage> PostWithAuthAsync(string url, string? bearerToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        if (bearerToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }

        return await _client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> PostWithAuthAsync(string url, object body, string? bearerToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body) };
        if (bearerToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }

        return await _client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> PatchWithAuthAsync(string url, object body, string? bearerToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Patch, url) { Content = JsonContent.Create(body) };
        if (bearerToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }

        return await _client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> GetWithAuthAsync(string url, string? bearerToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (bearerToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }

        return await _client.SendAsync(request);
    }

    // An adult Student is the simplest valid registered caller (IDR-5).
    private async Task<(Guid StudentId, string Token)> RegisterAndLoginStudentAsync()
    {
        var email = UniqueEmail();
        var registerResponse = await _client.PostAsJsonAsync(
            "/students", new { Email = email, Password = TestPassword, IsMinor = false });
        var registerBody = await ReadBodyAsync(registerResponse);
        var studentId = registerBody.GetProperty("value").GetProperty("studentId").GetGuid();

        var loginResponse = await _client.PostAsJsonAsync("/auth/login", new { Email = email, Password = TestPassword });
        var loginBody = await ReadBodyAsync(loginResponse);
        return (studentId, loginBody.GetProperty("value").GetProperty("token").GetString()!);
    }

    private async Task<(Guid TutorId, string Token)> RegisterAndLoginTutorAsync()
    {
        var email = UniqueEmail();
        var registerResponse = await _client.PostAsJsonAsync("/tutors", new { Email = email, Password = TestPassword });
        var registerBody = await ReadBodyAsync(registerResponse);
        var tutorId = registerBody.GetProperty("value").GetProperty("tutorId").GetGuid();

        var loginResponse = await _client.PostAsJsonAsync("/auth/login", new { Email = email, Password = TestPassword });
        var loginBody = await ReadBodyAsync(loginResponse);
        return (tutorId, loginBody.GetProperty("value").GetProperty("token").GetString()!);
    }

    private async Task<string> LoginAsync(string email)
    {
        var loginResponse = await _client.PostAsJsonAsync("/auth/login", new { Email = email, Password = TestPassword });
        var loginBody = await ReadBodyAsync(loginResponse);
        return loginBody.GetProperty("value").GetProperty("token").GetString()!;
    }

    // RegisterTutorCommand/RegisterStudentCommand/RegisterParentGuardianCommand
    // now require Email/Password (Account credentials are mandatory). The
    // in-memory SQLite database backing this fixture is shared across every
    // test in this class and enforces a UNIQUE index per role's Email column,
    // so each registration needs its own distinct address.
    private static string UniqueEmail() => $"test-{Guid.NewGuid():N}@example.com";

    private const string TestPassword = "Test-Password-123!";

    [Fact]
    public async Task RegisterTutor_is_reachable_and_returns_success()
    {
        var response = await _client.PostAsJsonAsync("/tutors", new { Email = UniqueEmail(), Password = TestPassword });

        response.EnsureSuccessStatusCode();
        var body = await ReadBodyAsync(response);
        Assert.True(body.GetProperty("isSuccess").GetBoolean());
        Assert.True(body.GetProperty("value").GetProperty("tutorId").GetGuid() != Guid.Empty);
    }

    [Fact]
    public async Task RegisterTutor_returns_conflict_for_a_duplicate_email()
    {
        var email = UniqueEmail();
        await _client.PostAsJsonAsync("/tutors", new { Email = email, Password = TestPassword });

        var response = await _client.PostAsJsonAsync("/tutors", new { Email = email, Password = TestPassword });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await ReadBodyAsync(response);
        Assert.Equal("RegisterTutorCommand.EmailAlreadyRegistered", body.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task ApproveTutor_returns_success_for_registered_tutor()
    {
        var adminToken = await SeedAndLoginAdminAsync();
        var registerResponse = await _client.PostAsJsonAsync("/tutors", new { Email = UniqueEmail(), Password = TestPassword });
        var registerBody = await ReadBodyAsync(registerResponse);
        var tutorId = registerBody.GetProperty("value").GetProperty("tutorId").GetGuid();

        var response = await PostWithAuthAsync($"/tutors/{tutorId}/approve", adminToken);

        response.EnsureSuccessStatusCode();
        var body = await ReadBodyAsync(response);
        Assert.True(body.GetProperty("isSuccess").GetBoolean());
    }

    [Fact]
    public async Task ApproveTutor_returns_failure_for_unknown_tutor()
    {
        var adminToken = await SeedAndLoginAdminAsync();

        var response = await PostWithAuthAsync($"/tutors/{Guid.NewGuid()}/approve", adminToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await ReadBodyAsync(response);
        Assert.True(body.GetProperty("isFailure").GetBoolean());
    }

    [Fact]
    public async Task ApproveTutor_requires_authentication()
    {
        var registerResponse = await _client.PostAsJsonAsync("/tutors", new { Email = UniqueEmail(), Password = TestPassword });
        var registerBody = await ReadBodyAsync(registerResponse);
        var tutorId = registerBody.GetProperty("value").GetProperty("tutorId").GetGuid();

        var response = await PostWithAuthAsync($"/tutors/{tutorId}/approve", bearerToken: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await ReadBodyAsync(response);
        Assert.Equal("Authorization.Unauthenticated", body.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task ApproveTutor_is_forbidden_for_a_non_admin_role()
    {
        var registerResponse = await _client.PostAsJsonAsync("/tutors", new { Email = UniqueEmail(), Password = TestPassword });
        var registerBody = await ReadBodyAsync(registerResponse);
        var tutorId = registerBody.GetProperty("value").GetProperty("tutorId").GetGuid();

        // Registration does not require approval to log in — approval only
        // gates discoverability/bookability (IDR-2) — so a second, unrelated
        // Tutor's own token is a valid, non-Admin caller for this check.
        var callerEmail = UniqueEmail();
        var callerRegisterResponse = await _client.PostAsJsonAsync("/tutors", new { Email = callerEmail, Password = TestPassword });
        await ReadBodyAsync(callerRegisterResponse);
        var callerLoginResponse = await _client.PostAsJsonAsync("/auth/login", new { Email = callerEmail, Password = TestPassword });
        var callerLoginBody = await ReadBodyAsync(callerLoginResponse);
        var tutorToken = callerLoginBody.GetProperty("value").GetProperty("token").GetString();

        var response = await PostWithAuthAsync($"/tutors/{tutorId}/approve", tutorToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await ReadBodyAsync(response);
        Assert.Equal("Authorization.Forbidden", body.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task SuspendTutor_returns_success_for_approved_tutor()
    {
        var adminToken = await SeedAndLoginAdminAsync();
        var registerResponse = await _client.PostAsJsonAsync("/tutors", new { Email = UniqueEmail(), Password = TestPassword });
        var registerBody = await ReadBodyAsync(registerResponse);
        var tutorId = registerBody.GetProperty("value").GetProperty("tutorId").GetGuid();
        await PostWithAuthAsync($"/tutors/{tutorId}/approve", adminToken);

        var response = await PostWithAuthAsync($"/tutors/{tutorId}/suspend", adminToken);

        response.EnsureSuccessStatusCode();
        var body = await ReadBodyAsync(response);
        Assert.True(body.GetProperty("isSuccess").GetBoolean());
    }

    [Fact]
    public async Task SuspendTutor_returns_failure_for_unknown_tutor()
    {
        var adminToken = await SeedAndLoginAdminAsync();

        var response = await PostWithAuthAsync($"/tutors/{Guid.NewGuid()}/suspend", adminToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await ReadBodyAsync(response);
        Assert.True(body.GetProperty("isFailure").GetBoolean());
    }

    [Fact]
    public async Task SuspendTutor_requires_authentication()
    {
        var response = await PostWithAuthAsync($"/tutors/{Guid.NewGuid()}/suspend", bearerToken: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await ReadBodyAsync(response);
        Assert.Equal("Authorization.Unauthenticated", body.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task RegisterStudent_maps_request_into_command_correctly()
    {
        var response = await _client.PostAsJsonAsync(
            "/students", new { Email = UniqueEmail(), Password = TestPassword, IsMinor = true });

        response.EnsureSuccessStatusCode();
        var body = await ReadBodyAsync(response);
        Assert.True(body.GetProperty("isSuccess").GetBoolean());
        Assert.True(body.GetProperty("value").GetProperty("isMinor").GetBoolean());
    }

    [Fact]
    public async Task RegisterStudent_returns_conflict_for_a_duplicate_email()
    {
        var email = UniqueEmail();
        await _client.PostAsJsonAsync("/students", new { Email = email, Password = TestPassword, IsMinor = false });

        var response = await _client.PostAsJsonAsync("/students", new { Email = email, Password = TestPassword, IsMinor = false });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await ReadBodyAsync(response);
        Assert.Equal("RegisterStudentCommand.EmailAlreadyRegistered", body.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task RegisterParentGuardian_is_reachable_and_returns_success()
    {
        var response = await _client.PostAsJsonAsync("/parent-guardians", new { Email = UniqueEmail(), Password = TestPassword });

        response.EnsureSuccessStatusCode();
        var body = await ReadBodyAsync(response);
        Assert.True(body.GetProperty("isSuccess").GetBoolean());
        Assert.True(body.GetProperty("value").GetProperty("parentGuardianId").GetGuid() != Guid.Empty);
    }

    [Fact]
    public async Task RegisterParentGuardian_returns_conflict_for_a_duplicate_email()
    {
        var email = UniqueEmail();
        await _client.PostAsJsonAsync("/parent-guardians", new { Email = email, Password = TestPassword });

        var response = await _client.PostAsJsonAsync("/parent-guardians", new { Email = email, Password = TestPassword });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await ReadBodyAsync(response);
        Assert.Equal("RegisterParentGuardianCommand.EmailAlreadyRegistered", body.GetProperty("error").GetProperty("code").GetString());
    }

    private async Task<(Guid ParentGuardianId, string ParentGuardianToken, Guid StudentId, string StudentToken)>
        RegisterParentGuardianAndStudentAsync()
    {
        var pgEmail = UniqueEmail();
        var pgResponse = await _client.PostAsJsonAsync("/parent-guardians", new { Email = pgEmail, Password = TestPassword });
        var pgBody = await ReadBodyAsync(pgResponse);
        var parentGuardianId = pgBody.GetProperty("value").GetProperty("parentGuardianId").GetGuid();
        var pgLoginResponse = await _client.PostAsJsonAsync("/auth/login", new { Email = pgEmail, Password = TestPassword });
        var pgToken = (await ReadBodyAsync(pgLoginResponse)).GetProperty("value").GetProperty("token").GetString()!;

        var studentEmail = UniqueEmail();
        var studentResponse = await _client.PostAsJsonAsync(
            "/students", new { Email = studentEmail, Password = TestPassword, IsMinor = false });
        var studentBody = await ReadBodyAsync(studentResponse);
        var studentId = studentBody.GetProperty("value").GetProperty("studentId").GetGuid();
        var studentLoginResponse = await _client.PostAsJsonAsync("/auth/login", new { Email = studentEmail, Password = TestPassword });
        var studentToken = (await ReadBodyAsync(studentLoginResponse)).GetProperty("value").GetProperty("token").GetString()!;

        return (parentGuardianId, pgToken, studentId, studentToken);
    }

    [Fact]
    public async Task CreateRelationshipInvitation_returns_success_when_both_accounts_exist()
    {
        var (parentGuardianId, parentGuardianToken, studentId, _) = await RegisterParentGuardianAndStudentAsync();

        var response = await PostWithAuthAsync(
            "/relationships",
            new { ParentGuardianId = parentGuardianId, StudentId = studentId },
            parentGuardianToken);

        response.EnsureSuccessStatusCode();
        var body = await ReadBodyAsync(response);
        Assert.True(body.GetProperty("isSuccess").GetBoolean());
        Assert.True(body.GetProperty("value").GetProperty("relationshipId").GetGuid() != Guid.Empty);
    }

    [Fact]
    public async Task CreateRelationshipInvitation_returns_failure_when_parent_guardian_missing()
    {
        var (_, parentGuardianToken, _, _) = await RegisterParentGuardianAndStudentAsync();
        var bogusParentGuardianId = Guid.NewGuid();

        var response = await PostWithAuthAsync(
            "/relationships",
            new { ParentGuardianId = bogusParentGuardianId, StudentId = Guid.NewGuid() },
            parentGuardianToken);

        // The caller must be a named party to the invitation (WP4 Priority
        // 4 Layer 2) — since the caller's own id doesn't match the bogus
        // ParentGuardianId here, ownership is rejected before the
        // "ParentGuardian not found" lookup is ever reached.
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await ReadBodyAsync(response);
        Assert.True(body.GetProperty("isFailure").GetBoolean());
    }

    [Fact]
    public async Task ConfirmRelationship_returns_success_for_invited_relationship()
    {
        var (parentGuardianId, parentGuardianToken, studentId, studentToken) = await RegisterParentGuardianAndStudentAsync();
        var inviteResponse = await PostWithAuthAsync(
            "/relationships",
            new { ParentGuardianId = parentGuardianId, StudentId = studentId },
            parentGuardianToken);
        var inviteBody = await ReadBodyAsync(inviteResponse);
        var relationshipId = inviteBody.GetProperty("value").GetProperty("relationshipId").GetGuid();

        // The counterparty (Student) — not the inviter (Parent/Guardian) —
        // confirms (AUTHORIZATION_MATRIX.md §4.1).
        var response = await PostWithAuthAsync($"/relationships/{relationshipId}/confirm", studentToken);

        response.EnsureSuccessStatusCode();
        var body = await ReadBodyAsync(response);
        Assert.True(body.GetProperty("isSuccess").GetBoolean());
    }

    [Fact]
    public async Task ConfirmRelationship_returns_failure_for_unknown_relationship()
    {
        var (_, _, _, studentToken) = await RegisterParentGuardianAndStudentAsync();

        var response = await PostWithAuthAsync($"/relationships/{Guid.NewGuid()}/confirm", studentToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await ReadBodyAsync(response);
        Assert.True(body.GetProperty("isFailure").GetBoolean());
    }

    [Fact]
    public async Task ConfirmRelationship_requires_authentication()
    {
        var response = await PostWithAuthAsync($"/relationships/{Guid.NewGuid()}/confirm", bearerToken: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await ReadBodyAsync(response);
        Assert.Equal("Authorization.Unauthenticated", body.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task ConfirmRelationship_is_forbidden_for_the_inviter()
    {
        var (parentGuardianId, parentGuardianToken, studentId, _) = await RegisterParentGuardianAndStudentAsync();
        var inviteResponse = await PostWithAuthAsync(
            "/relationships",
            new { ParentGuardianId = parentGuardianId, StudentId = studentId },
            parentGuardianToken);
        var inviteBody = await ReadBodyAsync(inviteResponse);
        var relationshipId = inviteBody.GetProperty("value").GetProperty("relationshipId").GetGuid();

        // The Parent/Guardian invited; the inviter may not also confirm.
        var response = await PostWithAuthAsync($"/relationships/{relationshipId}/confirm", parentGuardianToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await ReadBodyAsync(response);
        Assert.Equal("ConfirmRelationshipCommand.Forbidden", body.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task ConfirmRelationship_is_forbidden_for_an_unrelated_account()
    {
        var (parentGuardianId, parentGuardianToken, studentId, _) = await RegisterParentGuardianAndStudentAsync();
        var inviteResponse = await PostWithAuthAsync(
            "/relationships",
            new { ParentGuardianId = parentGuardianId, StudentId = studentId },
            parentGuardianToken);
        var inviteBody = await ReadBodyAsync(inviteResponse);
        var relationshipId = inviteBody.GetProperty("value").GetProperty("relationshipId").GetGuid();
        var (_, _, _, unrelatedStudentToken) = await RegisterParentGuardianAndStudentAsync();

        var response = await PostWithAuthAsync($"/relationships/{relationshipId}/confirm", unrelatedStudentToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await ReadBodyAsync(response);
        Assert.Equal("ConfirmRelationshipCommand.Forbidden", body.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task SetTutorHourlyRate_returns_success_for_registered_tutor()
    {
        var (tutorId, token) = await RegisterAndLoginTutorAsync();

        var response = await PatchWithAuthAsync($"/tutors/{tutorId}/hourly-rate", new { Amount = 50m }, token);

        response.EnsureSuccessStatusCode();
        var body = await ReadBodyAsync(response);
        Assert.True(body.GetProperty("isSuccess").GetBoolean());
    }

    [Fact]
    public async Task SetTutorHourlyRate_returns_failure_for_unknown_tutor()
    {
        var (_, token) = await RegisterAndLoginTutorAsync();

        var response = await PatchWithAuthAsync($"/tutors/{Guid.NewGuid()}/hourly-rate", new { Amount = 50m }, token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await ReadBodyAsync(response);
        Assert.True(body.GetProperty("isFailure").GetBoolean());
    }

    [Fact]
    public async Task SetTutorHourlyRate_returns_failure_for_non_positive_amount()
    {
        var (tutorId, token) = await RegisterAndLoginTutorAsync();

        var response = await PatchWithAuthAsync($"/tutors/{tutorId}/hourly-rate", new { Amount = 0m }, token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ADR-019/Phase 4 Task 5: HourlyRate.Of now also rejects a negative
    // amount (distinct from "zero", above), a fractional Rial (no minor
    // unit), and an amount exceeding MaxAmount — all surfaced as a clear
    // 400 through the same existing catch(ArgumentException) path, at the
    // real API boundary.
    [Fact]
    public async Task SetTutorHourlyRate_returns_failure_for_a_negative_amount()
    {
        var (tutorId, token) = await RegisterAndLoginTutorAsync();

        var response = await PatchWithAuthAsync($"/tutors/{tutorId}/hourly-rate", new { Amount = -500_000m }, token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await ReadBodyAsync(response);
        Assert.Equal("SetTutorHourlyRateCommand.Amount.Invalid", body.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task SetTutorHourlyRate_returns_failure_for_a_fractional_amount()
    {
        var (tutorId, token) = await RegisterAndLoginTutorAsync();

        var response = await PatchWithAuthAsync($"/tutors/{tutorId}/hourly-rate", new { Amount = 500_000.5m }, token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await ReadBodyAsync(response);
        Assert.Equal("SetTutorHourlyRateCommand.Amount.Invalid", body.GetProperty("error").GetProperty("code").GetString());
        Assert.Contains("whole number", body.GetProperty("error").GetProperty("message").GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SetTutorHourlyRate_returns_failure_for_an_amount_exceeding_the_maximum()
    {
        var (tutorId, token) = await RegisterAndLoginTutorAsync();

        var response = await PatchWithAuthAsync(
            $"/tutors/{tutorId}/hourly-rate", new { Amount = HourlyRate.MaxAmount + 10m }, token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await ReadBodyAsync(response);
        Assert.Equal("SetTutorHourlyRateCommand.Amount.Invalid", body.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task SetTutorHourlyRate_returns_success_for_the_maximum_amount()
    {
        var (tutorId, token) = await RegisterAndLoginTutorAsync();

        var response = await PatchWithAuthAsync(
            $"/tutors/{tutorId}/hourly-rate", new { Amount = HourlyRate.MaxAmount }, token);

        response.EnsureSuccessStatusCode();
        var body = await ReadBodyAsync(response);
        Assert.True(body.GetProperty("isSuccess").GetBoolean());

        var getResponse = await GetWithAuthAsync($"/tutors/{tutorId}", token);
        var getBody = await ReadBodyAsync(getResponse);
        Assert.Equal(HourlyRate.MaxAmount, getBody.GetProperty("value").GetProperty("hourlyRate").GetDecimal());
    }

    [Fact]
    public async Task SetTutorHourlyRate_requires_authentication()
    {
        var (tutorId, _) = await RegisterAndLoginTutorAsync();

        var response = await PatchWithAuthAsync($"/tutors/{tutorId}/hourly-rate", new { Amount = 50m }, bearerToken: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await ReadBodyAsync(response);
        Assert.Equal("Authorization.Unauthenticated", body.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task SetTutorHourlyRate_is_forbidden_for_a_non_tutor_role()
    {
        var (tutorId, _) = await RegisterAndLoginTutorAsync();
        var adminToken = await SeedAndLoginAdminAsync();

        var response = await PatchWithAuthAsync($"/tutors/{tutorId}/hourly-rate", new { Amount = 50m }, adminToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await ReadBodyAsync(response);
        Assert.Equal("Authorization.Forbidden", body.GetProperty("error").GetProperty("code").GetString());
    }

    // Layer 2 (ownership) — distinct from the Layer 1 (role) check above:
    // the caller here IS a Tutor (passes RequirePermission(ManageTutorOffering)
    // fine), but is a *different* Tutor than the one named by the route, so
    // the Application-layer ownership check (WP4 Priority 3 Layer 2) must
    // still reject it. The error code is handler-specific
    // ("SetTutorHourlyRateCommand.Forbidden"), not AuthorizationMiddleware's
    // "Authorization.Forbidden" — a different layer produced this 403.
    [Fact]
    public async Task SetTutorHourlyRate_is_forbidden_for_a_different_tutor()
    {
        var (ownerTutorId, _) = await RegisterAndLoginTutorAsync();
        var (_, otherTutorToken) = await RegisterAndLoginTutorAsync();

        var response = await PatchWithAuthAsync($"/tutors/{ownerTutorId}/hourly-rate", new { Amount = 50m }, otherTutorToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await ReadBodyAsync(response);
        Assert.Equal("SetTutorHourlyRateCommand.Forbidden", body.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task SetTutorSubject_returns_success_for_registered_tutor()
    {
        var (tutorId, token) = await RegisterAndLoginTutorAsync();

        var response = await PatchWithAuthAsync($"/tutors/{tutorId}/subject", new { Subject = "Mathematics" }, token);

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task SetTutorLanguage_returns_success_for_registered_tutor()
    {
        var (tutorId, token) = await RegisterAndLoginTutorAsync();

        var response = await PatchWithAuthAsync($"/tutors/{tutorId}/language", new { Language = "English" }, token);

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task SetTutorLocation_returns_success_for_registered_tutor()
    {
        var (tutorId, token) = await RegisterAndLoginTutorAsync();

        var response = await PatchWithAuthAsync($"/tutors/{tutorId}/location", new { Location = "Berlin" }, token);

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task SetTutorOfferedDurations_returns_success_for_registered_tutor()
    {
        var (tutorId, token) = await RegisterAndLoginTutorAsync();

        var response = await PatchWithAuthAsync(
            $"/tutors/{tutorId}/offered-durations",
            new { Durations = new[] { "00:30:00", "01:00:00" } },
            token);

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task SetTutorOfferedDurations_returns_failure_for_empty_durations()
    {
        var (tutorId, token) = await RegisterAndLoginTutorAsync();

        var response = await PatchWithAuthAsync(
            $"/tutors/{tutorId}/offered-durations",
            new { Durations = Array.Empty<string>() },
            token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetPendingTutors_includes_a_freshly_registered_tutor()
    {
        var adminToken = await SeedAndLoginAdminAsync();
        var registerResponse = await _client.PostAsJsonAsync("/tutors", new { Email = UniqueEmail(), Password = TestPassword });
        var registerBody = await ReadBodyAsync(registerResponse);
        var tutorId = registerBody.GetProperty("value").GetProperty("tutorId").GetGuid();

        var response = await GetWithAuthAsync("/tutors/pending", adminToken);

        response.EnsureSuccessStatusCode();
        var body = await ReadBodyAsync(response);
        var ids = body.GetProperty("value").GetProperty("items").EnumerateArray().Select(t => t.GetProperty("tutorId").GetGuid());
        Assert.Contains(tutorId, ids);
    }

    [Fact]
    public async Task GetPendingTutors_excludes_an_approved_tutor()
    {
        var adminToken = await SeedAndLoginAdminAsync();
        var registerResponse = await _client.PostAsJsonAsync("/tutors", new { Email = UniqueEmail(), Password = TestPassword });
        var registerBody = await ReadBodyAsync(registerResponse);
        var tutorId = registerBody.GetProperty("value").GetProperty("tutorId").GetGuid();
        await PostWithAuthAsync($"/tutors/{tutorId}/approve", adminToken);

        var response = await GetWithAuthAsync("/tutors/pending", adminToken);

        response.EnsureSuccessStatusCode();
        var body = await ReadBodyAsync(response);
        var ids = body.GetProperty("value").GetProperty("items").EnumerateArray().Select(t => t.GetProperty("tutorId").GetGuid());
        Assert.DoesNotContain(tutorId, ids);
    }

    [Fact]
    public async Task GetPendingTutors_requires_authentication()
    {
        var response = await GetWithAuthAsync("/tutors/pending", bearerToken: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetPendingTutors_is_forbidden_for_a_non_admin_caller()
    {
        var (_, token) = await RegisterAndLoginStudentAsync();

        var response = await GetWithAuthAsync("/tutors/pending", token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetParentGuardianById_returns_success_for_the_parent_guardian_themselves()
    {
        var email = UniqueEmail();
        var registerResponse = await _client.PostAsJsonAsync("/parent-guardians", new { Email = email, Password = TestPassword });
        var registerBody = await ReadBodyAsync(registerResponse);
        var parentGuardianId = registerBody.GetProperty("value").GetProperty("parentGuardianId").GetGuid();
        var token = await LoginAsync(email);

        var response = await GetWithAuthAsync($"/parent-guardians/{parentGuardianId}", token);

        response.EnsureSuccessStatusCode();
        var body = await ReadBodyAsync(response);
        Assert.Equal(parentGuardianId, body.GetProperty("value").GetProperty("parentGuardianId").GetGuid());
    }

    [Fact]
    public async Task GetParentGuardianById_returns_success_for_admin()
    {
        var registerResponse = await _client.PostAsJsonAsync("/parent-guardians", new { Email = UniqueEmail(), Password = TestPassword });
        var parentGuardianId = (await ReadBodyAsync(registerResponse)).GetProperty("value").GetProperty("parentGuardianId").GetGuid();
        var adminToken = await SeedAndLoginAdminAsync();

        var response = await GetWithAuthAsync($"/parent-guardians/{parentGuardianId}", adminToken);

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetParentGuardianById_returns_success_for_the_linked_student_with_a_confirmed_relationship()
    {
        var (parentGuardianId, parentGuardianToken, studentId, studentToken) = await RegisterParentGuardianAndStudentAsync();
        var inviteResponse = await PostWithAuthAsync(
            "/relationships", new { ParentGuardianId = parentGuardianId, StudentId = studentId }, parentGuardianToken);
        var relationshipId = (await ReadBodyAsync(inviteResponse)).GetProperty("value").GetProperty("relationshipId").GetGuid();
        await PostWithAuthAsync($"/relationships/{relationshipId}/confirm", studentToken);

        var response = await GetWithAuthAsync($"/parent-guardians/{parentGuardianId}", studentToken);

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetParentGuardianById_requires_authentication()
    {
        var registerResponse = await _client.PostAsJsonAsync("/parent-guardians", new { Email = UniqueEmail(), Password = TestPassword });
        var parentGuardianId = (await ReadBodyAsync(registerResponse)).GetProperty("value").GetProperty("parentGuardianId").GetGuid();

        var response = await GetWithAuthAsync($"/parent-guardians/{parentGuardianId}", bearerToken: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetParentGuardianById_is_forbidden_for_an_unrelated_student()
    {
        var registerResponse = await _client.PostAsJsonAsync("/parent-guardians", new { Email = UniqueEmail(), Password = TestPassword });
        var parentGuardianId = (await ReadBodyAsync(registerResponse)).GetProperty("value").GetProperty("parentGuardianId").GetGuid();
        var (_, studentToken) = await RegisterAndLoginStudentAsync();

        var response = await GetWithAuthAsync($"/parent-guardians/{parentGuardianId}", studentToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await ReadBodyAsync(response);
        Assert.Equal("GetParentGuardianByIdQuery.Forbidden", body.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task GetParentGuardianById_returns_failure_for_unknown_parent_guardian()
    {
        var adminToken = await SeedAndLoginAdminAsync();

        var response = await GetWithAuthAsync($"/parent-guardians/{Guid.NewGuid()}", adminToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetStudentById_returns_success_for_the_student_themselves()
    {
        var (studentId, token) = await RegisterAndLoginStudentAsync();

        var response = await GetWithAuthAsync($"/students/{studentId}", token);

        response.EnsureSuccessStatusCode();
        var body = await ReadBodyAsync(response);
        Assert.Equal(studentId, body.GetProperty("value").GetProperty("studentId").GetGuid());
    }

    [Fact]
    public async Task GetStudentById_requires_authentication()
    {
        var (studentId, _) = await RegisterAndLoginStudentAsync();

        var response = await GetWithAuthAsync($"/students/{studentId}", bearerToken: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetStudentById_is_forbidden_for_an_unrelated_parent_guardian()
    {
        var (studentId, _) = await RegisterAndLoginStudentAsync();
        var otherEmail = UniqueEmail();
        await _client.PostAsJsonAsync("/parent-guardians", new { Email = otherEmail, Password = TestPassword });
        var otherToken = await LoginAsync(otherEmail);

        var response = await GetWithAuthAsync($"/students/{studentId}", otherToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await ReadBodyAsync(response);
        Assert.Equal("GetStudentByIdQuery.Forbidden", body.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task GetTutorById_returns_success_for_an_unauthenticated_caller_when_discoverable()
    {
        var adminToken = await SeedAndLoginAdminAsync();
        var (tutorId, _) = await RegisterAndLoginTutorAsync();
        await PostWithAuthAsync($"/tutors/{tutorId}/approve", adminToken);

        var response = await GetWithAuthAsync($"/tutors/{tutorId}", bearerToken: null);

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetTutorById_requires_authentication_when_not_discoverable()
    {
        var (tutorId, _) = await RegisterAndLoginTutorAsync();

        var response = await GetWithAuthAsync($"/tutors/{tutorId}", bearerToken: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetTutorById_is_forbidden_for_an_unrelated_caller_when_not_discoverable()
    {
        var (tutorId, _) = await RegisterAndLoginTutorAsync();
        var (_, otherToken) = await RegisterAndLoginTutorAsync();

        var response = await GetWithAuthAsync($"/tutors/{tutorId}", otherToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await ReadBodyAsync(response);
        Assert.Equal("GetTutorByIdQuery.Forbidden", body.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task GetTutorById_returns_success_for_the_tutor_themselves_when_not_discoverable()
    {
        var (tutorId, token) = await RegisterAndLoginTutorAsync();

        var response = await GetWithAuthAsync($"/tutors/{tutorId}", token);

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetTutorById_returns_success_for_admin_when_not_discoverable()
    {
        var (tutorId, _) = await RegisterAndLoginTutorAsync();
        var adminToken = await SeedAndLoginAdminAsync();

        var response = await GetWithAuthAsync($"/tutors/{tutorId}", adminToken);

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetRelationshipsByAccountId_returns_the_relationship_for_the_student_themselves()
    {
        var (parentGuardianId, parentGuardianToken, studentId, studentToken) = await RegisterParentGuardianAndStudentAsync();
        var inviteResponse = await PostWithAuthAsync(
            "/relationships",
            new { ParentGuardianId = parentGuardianId, StudentId = studentId },
            parentGuardianToken);
        var inviteBody = await ReadBodyAsync(inviteResponse);
        var relationshipId = inviteBody.GetProperty("value").GetProperty("relationshipId").GetGuid();

        var response = await GetWithAuthAsync($"/accounts/{studentId}/relationships", studentToken);

        response.EnsureSuccessStatusCode();
        var body = await ReadBodyAsync(response);
        var ids = body.GetProperty("value").EnumerateArray().Select(r => r.GetProperty("relationshipId").GetGuid());
        Assert.Contains(relationshipId, ids);
    }

    [Fact]
    public async Task GetRelationshipsByAccountId_returns_relationships_for_admin()
    {
        var (_, _, studentId, _) = await RegisterParentGuardianAndStudentAsync();
        var adminToken = await SeedAndLoginAdminAsync();

        var response = await GetWithAuthAsync($"/accounts/{studentId}/relationships", adminToken);

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetRelationshipsByAccountId_requires_authentication()
    {
        var response = await GetWithAuthAsync($"/accounts/{Guid.NewGuid()}/relationships", bearerToken: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetRelationshipsByAccountId_is_forbidden_for_a_different_account()
    {
        var (_, studentToken) = await RegisterAndLoginStudentAsync();

        var response = await GetWithAuthAsync($"/accounts/{Guid.NewGuid()}/relationships", studentToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetRelationshipsByAccountId_returns_empty_collection_for_an_account_with_no_relationships()
    {
        var (studentId, studentToken) = await RegisterAndLoginStudentAsync();

        var response = await GetWithAuthAsync($"/accounts/{studentId}/relationships", studentToken);

        response.EnsureSuccessStatusCode();
        var body = await ReadBodyAsync(response);
        Assert.Empty(body.GetProperty("value").EnumerateArray());
    }

    [Fact]
    public async Task GetRelationshipById_returns_success_for_the_parent_guardian_party()
    {
        var (parentGuardianId, parentGuardianToken, studentId, _) = await RegisterParentGuardianAndStudentAsync();
        var inviteResponse = await PostWithAuthAsync(
            "/relationships",
            new { ParentGuardianId = parentGuardianId, StudentId = studentId },
            parentGuardianToken);
        var relationshipId = (await ReadBodyAsync(inviteResponse)).GetProperty("value").GetProperty("relationshipId").GetGuid();

        var response = await GetWithAuthAsync($"/relationships/{relationshipId}", parentGuardianToken);

        response.EnsureSuccessStatusCode();
        var body = await ReadBodyAsync(response);
        Assert.Equal(relationshipId, body.GetProperty("value").GetProperty("relationshipId").GetGuid());
    }

    [Fact]
    public async Task GetRelationshipById_returns_success_for_the_student_party_while_still_invited()
    {
        var (parentGuardianId, parentGuardianToken, studentId, studentToken) = await RegisterParentGuardianAndStudentAsync();
        var inviteResponse = await PostWithAuthAsync(
            "/relationships",
            new { ParentGuardianId = parentGuardianId, StudentId = studentId },
            parentGuardianToken);
        var relationshipId = (await ReadBodyAsync(inviteResponse)).GetProperty("value").GetProperty("relationshipId").GetGuid();

        var response = await GetWithAuthAsync($"/relationships/{relationshipId}", studentToken);

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetRelationshipById_returns_success_for_admin()
    {
        var (parentGuardianId, parentGuardianToken, studentId, _) = await RegisterParentGuardianAndStudentAsync();
        var inviteResponse = await PostWithAuthAsync(
            "/relationships",
            new { ParentGuardianId = parentGuardianId, StudentId = studentId },
            parentGuardianToken);
        var relationshipId = (await ReadBodyAsync(inviteResponse)).GetProperty("value").GetProperty("relationshipId").GetGuid();
        var adminToken = await SeedAndLoginAdminAsync();

        var response = await GetWithAuthAsync($"/relationships/{relationshipId}", adminToken);

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetRelationshipById_requires_authentication()
    {
        var (parentGuardianId, parentGuardianToken, studentId, _) = await RegisterParentGuardianAndStudentAsync();
        var inviteResponse = await PostWithAuthAsync(
            "/relationships",
            new { ParentGuardianId = parentGuardianId, StudentId = studentId },
            parentGuardianToken);
        var relationshipId = (await ReadBodyAsync(inviteResponse)).GetProperty("value").GetProperty("relationshipId").GetGuid();

        var response = await GetWithAuthAsync($"/relationships/{relationshipId}", bearerToken: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetRelationshipById_is_forbidden_for_an_unrelated_caller()
    {
        var (parentGuardianId, parentGuardianToken, studentId, _) = await RegisterParentGuardianAndStudentAsync();
        var inviteResponse = await PostWithAuthAsync(
            "/relationships",
            new { ParentGuardianId = parentGuardianId, StudentId = studentId },
            parentGuardianToken);
        var relationshipId = (await ReadBodyAsync(inviteResponse)).GetProperty("value").GetProperty("relationshipId").GetGuid();
        var (_, otherToken) = await RegisterAndLoginStudentAsync();

        var response = await GetWithAuthAsync($"/relationships/{relationshipId}", otherToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
