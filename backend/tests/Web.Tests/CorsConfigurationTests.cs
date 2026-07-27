using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace TutorFlow.Web.Tests;

// PHASE-025 Task 1, widened by PHASE-048 Task 3: proves the real CORS
// policy — not just the config file — actually allows every Vite dev
// server origin, via a genuine preflight OPTIONS request through the real
// ASP.NET Core CORS middleware. Uses WithWebHostBuilder to inject the same
// Cors:AllowedOrigins value appsettings.Development.json carries
// (http://localhost:5173 and :5174 — frontend/vite.config.ts pins the dev
// server to 5173 with strictPort, but 5174 is kept allowed too as a
// deliberate margin, since PHASE-048's own root cause was the dev server
// silently landing on a port CORS didn't recognize), rather than depending
// on a live `dotnet run` boot — this test needs no real database at all,
// since CORS preflight handling short-circuits before any endpoint or DB
// access. This is exactly the class of failure that passed every test
// before this phase: it only manifests in a real browser preflight, never
// in a same-origin test client or a unit test that calls a handler
// directly — so removing an allowed origin from configuration, or from
// this test's own coverage of it, is the only thing that would have
// caught PHASE-048's defect ahead of time.
public class CorsConfigurationTests : IClassFixture<TutorFlowWebApplicationFactory>, IAsyncLifetime
{
    private static readonly string[] ConfiguredDevOrigins =
    [
        "http://localhost:5173",
        "http://localhost:5174",
    ];

    private readonly TutorFlowWebApplicationFactory _factory;

    public CorsConfigurationTests(TutorFlowWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private HttpClient CreateClientWithAllowedOrigins(params string[] origins)
    {
        // UseSetting, not ConfigureAppConfiguration: Program.cs uses the
        // minimal hosting model (WebApplication.CreateBuilder), and
        // ConfigureAppConfiguration's added sources were observed NOT to
        // reach builder.Configuration by the time Program.cs reads
        // Cors:AllowedOrigins — confirmed by re-running with the log still
        // reading "no allowed origins configured" despite the override.
        // UseSetting writes directly into the WebHostBuilderContext's
        // settings, which does take effect for this hosting model. Each
        // origin is set at its own array index, mirroring how
        // appsettings.Development.json's own array is bound.
        var configuredFactory = _factory.WithWebHostBuilder(builder =>
        {
            for (var i = 0; i < origins.Length; i++)
            {
                builder.UseSetting($"Cors:AllowedOrigins:{i}", origins[i]);
            }
        });

        return configuredFactory.CreateClient();
    }

    // Mirrors exactly what a real browser sends before an authenticated GET
    // from apiClient.ts (frontend/src/services/api/apiClient.ts): the
    // Authorization bearer token and Content-Type: application/json headers
    // both force a preflight, since neither is CORS-safelisted. Runs once
    // per origin appsettings.Development.json actually allows, so dropping
    // either one from configuration fails this test, not just a browser.
    [Theory]
    [InlineData("http://localhost:5173")]
    [InlineData("http://localhost:5174")]
    public async Task Preflight_from_each_configured_dev_origin_is_allowed(string origin)
    {
        using var client = CreateClientWithAllowedOrigins(ConfiguredDevOrigins);
        using var request = new HttpRequestMessage(HttpMethod.Options, "/tutors/pending");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "GET");
        request.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");

        var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        Assert.Equal(origin, response.Headers.GetValues("Access-Control-Allow-Origin").Single());
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
    public async Task Preflight_from_a_different_origin_is_not_allowed_even_when_both_dev_origins_are_configured()
    {
        using var client = CreateClientWithAllowedOrigins(ConfiguredDevOrigins);
        using var request = new HttpRequestMessage(HttpMethod.Options, "/tutors/pending");
        request.Headers.Add("Origin", "https://not-the-configured-origin.example");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await client.SendAsync(request);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }
}
