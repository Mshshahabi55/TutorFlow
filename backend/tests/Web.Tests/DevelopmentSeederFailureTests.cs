using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TutorFlow.Web.Tests;

// Phase 3.5 Task 3: a database failure during Development-only seeding (an
// unreachable/misconfigured database — a wrong password is exactly how this
// was first discovered, Phase 2.5) must not crash the host before it even
// starts listening. This factory runs the real Development branch of
// Program.cs — unlike TutorFlowWebApplicationFactory's "Testing" environment,
// which skips Npgsql/seeding entirely via IsEnvironment("Testing") — against
// a connection string that refuses instantly, to prove SeedAsync's failure
// is caught, logged, and the host still starts.
public sealed class DevelopmentSeederFailureWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string ConnectionStringEnvVar = "ConnectionStrings__TutorFlow";
    private const string AdminPasswordEnvVar = "Seed__AdminPassword";

    private readonly string? _originalConnectionString;
    private readonly string? _originalAdminPassword;

    public DevelopmentSeederFailureWebApplicationFactory()
    {
        _originalConnectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvVar);
        _originalAdminPassword = Environment.GetEnvironmentVariable(AdminPasswordEnvVar);

        // A plain IWebHostBuilder.ConfigureAppConfiguration override is not
        // enough here: WebApplicationBuilder.CreateBuilder(args) (Program.cs's
        // very first line) already materializes its ConfigurationManager —
        // including any real ConnectionStrings:TutorFlow configured via
        // dotnet user-secrets for local development, exactly what this
        // machine has — before this factory gets a chance to layer anything
        // on top. Environment variables outrank user secrets in ASP.NET
        // Core's own default configuration precedence, so setting them
        // (restored in Dispose below) reliably wins instead.
        //
        // Port 1 on loopback: nothing listens there, so Npgsql's connection
        // attempt is refused immediately by the OS rather than timing out —
        // keeps this test fast and deterministic instead of waiting out a
        // real timeout against a merely-slow-to-fail host.
        Environment.SetEnvironmentVariable(
            ConnectionStringEnvVar,
            "Host=127.0.0.1;Port=1;Database=tutorflow_unreachable;Username=test;Password=test;Timeout=2");
        Environment.SetEnvironmentVariable(AdminPasswordEnvVar, "Test-Password-123!");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            Environment.SetEnvironmentVariable(ConnectionStringEnvVar, _originalConnectionString);
            Environment.SetEnvironmentVariable(AdminPasswordEnvVar, _originalAdminPassword);
        }
    }
}

public class DevelopmentSeederFailureTests : IClassFixture<DevelopmentSeederFailureWebApplicationFactory>
{
    private readonly HttpClient _client;

    public DevelopmentSeederFailureTests(DevelopmentSeederFailureWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Host_starts_and_health_is_reachable_when_the_seed_database_is_unreachable()
    {
        // The point being proven is that GetAsync completes at all: before
        // this phase's fix, DevelopmentSeeder.SeedAsync's unhandled
        // exception would have propagated out of Program.cs's top-level
        // statements during host startup, and this call would throw
        // instead of completing.
        var response = await _client.GetAsync("/health");

        // The database really is unreachable (by construction, above), so
        // /health itself correctly reports Unhealthy (503) — the health
        // check is not, and should not be, fooled into reporting otherwise.
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal("Unhealthy", body);
    }
}
