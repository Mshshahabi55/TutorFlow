using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace TutorFlow.Web.Tests;

// PHASE-025 Task 1: proves the real CORS policy — not just the config file —
// actually allows the Vite dev server origin, via a genuine preflight OPTIONS
// request through the real ASP.NET Core CORS middleware. Uses
// WithWebHostBuilder to inject the same Cors:AllowedOrigins value
// appsettings.Development.json carries (http://localhost:5173, the port
// frontend/vite.config.ts's server.port sets), rather than depending on a
// live `dotnet run` boot — this test needs no real database at all, since
// CORS preflight handling short-circuits before any endpoint or DB access.
public class CorsConfigurationTests : IClassFixture<TutorFlowWebApplicationFactory>, IAsyncLifetime
{
    private readonly TutorFlowWebApplicationFactory _factory;

    public CorsConfigurationTests(TutorFlowWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private HttpClient CreateClientWithAllowedOrigin(string origin)
    {
        // UseSetting, not ConfigureAppConfiguration: Program.cs uses the
        // minimal hosting model (WebApplication.CreateBuilder), and
        // ConfigureAppConfiguration's added sources were observed NOT to
        // reach builder.Configuration by the time Program.cs reads
        // Cors:AllowedOrigins — confirmed by re-running with the log still
        // reading "no allowed origins configured" despite the override.
        // UseSetting writes directly into the WebHostBuilderContext's
        // settings, which does take effect for this hosting model.
        var configuredFactory = _factory.WithWebHostBuilder(builder =>
            builder.UseSetting("Cors:AllowedOrigins:0", origin));

        return configuredFactory.CreateClient();
    }

    // Mirrors exactly what a real browser sends before an authenticated GET
    // from apiClient.ts (frontend/src/services/api/apiClient.ts): the
    // Authorization bearer token and Content-Type: application/json headers
    // both force a preflight, since neither is CORS-safelisted.
    [Fact]
    public async Task Preflight_from_the_configured_dev_server_origin_is_allowed()
    {
        using var client = CreateClientWithAllowedOrigin("http://localhost:5173");
        using var request = new HttpRequestMessage(HttpMethod.Options, "/tutors/pending");
        request.Headers.Add("Origin", "http://localhost:5173");
        request.Headers.Add("Access-Control-Request-Method", "GET");
        request.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");

        var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        Assert.Equal("http://localhost:5173", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.True(response.Headers.Contains("Access-Control-Allow-Methods"));
        Assert.True(response.Headers.Contains("Access-Control-Allow-Headers"));

        // apiClient.ts never sets axios's withCredentials, so the frontend
        // never sends cookies cross-origin and the bearer token is a
        // manually-attached header, not a browser-managed credential — this
        // policy must not advertise AllowCredentials, since doing so for no
        // reason would only widen the browser-enforced contract without the
        // frontend ever using it.
        Assert.False(response.Headers.Contains("Access-Control-Allow-Credentials"));
    }

    [Fact]
    public async Task Preflight_from_a_different_origin_is_not_allowed_even_when_one_origin_is_configured()
    {
        using var client = CreateClientWithAllowedOrigin("http://localhost:5173");
        using var request = new HttpRequestMessage(HttpMethod.Options, "/tutors/pending");
        request.Headers.Add("Origin", "https://not-the-configured-origin.example");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await client.SendAsync(request);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }
}
