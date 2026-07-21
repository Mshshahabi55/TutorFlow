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
// SearchTutors_filters_by_subject_and_finds_a_matching_approved_tutor now
// approves through an authenticated Admin caller, since /tutors/{id}/approve
// is coarse-grained-protected as of WP4 Priority 2.
public class DiscoveryEndpointsTests : IClassFixture<TutorFlowWebApplicationFactory>, IAsyncLifetime
{
    private readonly TutorFlowWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public DiscoveryEndpointsTests(TutorFlowWebApplicationFactory factory)
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

    // RegisterTutorCommand now requires Email/Password (Account credentials
    // are mandatory); the shared in-memory SQLite database enforces a UNIQUE
    // index on Email, so each registration needs its own distinct address.
    private static string UniqueEmail() => $"test-{Guid.NewGuid():N}@example.com";

    private const string TestPassword = "Test-Password-123!";

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

    private async Task<HttpResponseMessage> PatchWithAuthAsync(string url, object body, string? bearerToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Patch, url) { Content = JsonContent.Create(body) };
        if (bearerToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }

        return await _client.SendAsync(request);
    }

    private async Task<string> LoginAsync(string email) =>
        (await ReadBodyAsync(await _client.PostAsJsonAsync("/auth/login", new { Email = email, Password = TestPassword })))
            .GetProperty("value").GetProperty("token").GetString()!;

    [Fact]
    public async Task SearchTutors_with_no_filters_is_reachable_and_returns_success()
    {
        var response = await _client.GetAsync("/tutors/search");

        response.EnsureSuccessStatusCode();
        var body = await ReadBodyAsync(response);
        Assert.True(body.GetProperty("isSuccess").GetBoolean());
        Assert.True(body.GetProperty("value").TryGetProperty("items", out _));
    }

    [Fact]
    public async Task SearchTutors_filters_by_subject_and_finds_a_matching_approved_tutor()
    {
        var adminToken = await SeedAndLoginAdminAsync();
        var email = UniqueEmail();
        var registerResponse = await _client.PostAsJsonAsync("/tutors", new { Email = email, Password = TestPassword });
        var registerBody = await ReadBodyAsync(registerResponse);
        var tutorId = registerBody.GetProperty("value").GetProperty("tutorId").GetGuid();
        var tutorToken = await LoginAsync(email);
        await PostWithAuthAsync($"/tutors/{tutorId}/approve", adminToken);
        await PatchWithAuthAsync($"/tutors/{tutorId}/subject", new { Subject = "Mathematics" }, tutorToken);

        var response = await _client.GetAsync("/tutors/search?subject=Mathematics");

        response.EnsureSuccessStatusCode();
        var body = await ReadBodyAsync(response);
        var ids = body.GetProperty("value").GetProperty("items").EnumerateArray().Select(t => t.GetProperty("tutorId").GetGuid());
        Assert.Contains(tutorId, ids);
    }

    [Fact]
    public async Task SearchTutors_excludes_a_pending_tutor_even_when_subject_matches()
    {
        var email = UniqueEmail();
        var registerResponse = await _client.PostAsJsonAsync("/tutors", new { Email = email, Password = TestPassword });
        var registerBody = await ReadBodyAsync(registerResponse);
        var tutorId = registerBody.GetProperty("value").GetProperty("tutorId").GetGuid();
        var tutorToken = await LoginAsync(email);
        await PatchWithAuthAsync($"/tutors/{tutorId}/subject", new { Subject = "Mathematics" }, tutorToken);

        var response = await _client.GetAsync("/tutors/search?subject=Mathematics");

        response.EnsureSuccessStatusCode();
        var body = await ReadBodyAsync(response);
        var ids = body.GetProperty("value").GetProperty("items").EnumerateArray().Select(t => t.GetProperty("tutorId").GetGuid());
        Assert.DoesNotContain(tutorId, ids);
    }
}
