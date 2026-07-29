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

// End-to-end tests against the real HTTP pipeline — see IdentityEndpointsTests
// for the scope discipline these follow (Presentation orchestration only).
// GetAuditEntries now also exercises AuthorizationMiddleware's real
// enforcement (WP4 Priority 2, AUTHORIZATION_MATRIX.md §4.6).
public class AuditEndpointsTests : IClassFixture<TutorFlowWebApplicationFactory>, IAsyncLifetime
{
    private readonly TutorFlowWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuditEndpointsTests(TutorFlowWebApplicationFactory factory)
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

    private const string TestPassword = "Test-Password-123!";

    private static string UniqueEmail() => $"audit-test-{Guid.NewGuid():N}@example.com";

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

    private async Task<HttpResponseMessage> GetWithAuthAsync(string url, string? bearerToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (bearerToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }

        return await _client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> PostWithAuthAsync(string url, object? body, string? bearerToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
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

    // An adult Student is the simplest valid BookSession caller (IDR-5).
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

    private async Task<Guid> BookSessionAsync()
    {
        var (tutorId, token) = await RegisterAndLoginTutorAsync();
        var slotResponse = await PostWithAuthAsync("/availability-slots", new
        {
            TutorId = tutorId,
            StartTimeUtc = DateTime.UtcNow.AddHours(25),
            Duration = TimeSpan.FromHours(1),
            DeliveryMode = 0, // Online
        }, token);
        var slotBody = await ReadBodyAsync(slotResponse);
        var slotId = slotBody.GetProperty("value").GetProperty("availabilitySlotId").GetGuid();

        var (studentId, studentToken) = await RegisterAndLoginStudentAsync();
        var sessionResponse = await PostWithAuthAsync("/sessions", new
        {
            AvailabilitySlotId = slotId,
            StudentId = studentId,
            ParentGuardianId = (Guid?)null,
        }, studentToken);
        var sessionBody = await ReadBodyAsync(sessionResponse);
        return sessionBody.GetProperty("value").GetProperty("sessionId").GetGuid();
    }

    [Fact]
    public async Task GetAuditEntries_filtered_by_subjectId_returns_only_that_subjects_entry()
    {
        var adminToken = await SeedAndLoginAdminAsync();
        var sessionId = await BookSessionAsync();

        var response = await GetWithAuthAsync($"/audit-entries?subjectId={sessionId}", adminToken);

        response.EnsureSuccessStatusCode();
        var body = await ReadBodyAsync(response);
        var items = body.GetProperty("value").GetProperty("items").EnumerateArray().ToList();
        Assert.Single(items);
        Assert.Equal(sessionId, items[0].GetProperty("subjectId").GetGuid());
        Assert.Equal("SessionBooked", items[0].GetProperty("action").GetString());
    }

    [Fact]
    public async Task GetAuditEntries_reports_a_total_count()
    {
        var adminToken = await SeedAndLoginAdminAsync();
        await BookSessionAsync();

        var response = await GetWithAuthAsync("/audit-entries?page=1&pageSize=1", adminToken);

        response.EnsureSuccessStatusCode();
        var body = await ReadBodyAsync(response);
        Assert.True(body.GetProperty("value").GetProperty("totalCount").GetInt32() >= 1);
        Assert.Equal(1, body.GetProperty("value").GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task GetAuditEntries_rejects_an_empty_subjectId()
    {
        var adminToken = await SeedAndLoginAdminAsync();

        var response = await GetWithAuthAsync($"/audit-entries?subjectId={Guid.Empty}", adminToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAuditEntries_requires_authentication()
    {
        var response = await GetWithAuthAsync("/audit-entries", bearerToken: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await ReadBodyAsync(response);
        Assert.Equal("Authorization.Unauthenticated", body.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task GetAuditEntries_is_forbidden_for_a_non_admin_role()
    {
        var email = UniqueEmail();
        var registerResponse = await _client.PostAsJsonAsync("/tutors", new { Email = email, Password = TestPassword });
        await ReadBodyAsync(registerResponse);
        var loginResponse = await _client.PostAsJsonAsync("/auth/login", new { Email = email, Password = TestPassword });
        var loginBody = await ReadBodyAsync(loginResponse);
        var tutorToken = loginBody.GetProperty("value").GetProperty("token").GetString();

        var response = await GetWithAuthAsync("/audit-entries", tutorToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await ReadBodyAsync(response);
        Assert.Equal("Authorization.Forbidden", body.GetProperty("error").GetProperty("code").GetString());
    }
}
