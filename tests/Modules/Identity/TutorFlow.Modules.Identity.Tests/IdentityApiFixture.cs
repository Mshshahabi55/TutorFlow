using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using TutorFlow.Host;
using TutorFlow.Modules.Identity.Persistence;

namespace TutorFlow.Modules.Identity.Tests;

public sealed class IdentityApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string JwtIssuer = "TutorFlow.Tests";
    public const string JwtAudience = "TutorFlow.Tests";
    public const string JwtKey = "integration-test-signing-key-at-least-32-bytes-long";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18").Build();

    private IDisposable? _wolverineConnectionStrings;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:IdentityDb"] = _postgres.GetConnectionString(),
                ["Jwt:Issuer"] = JwtIssuer,
                ["Jwt:Audience"] = JwtAudience,
                ["Jwt:Key"] = JwtKey,
                ["Jwt:ExpirationMinutes"] = "60",
                // This fixture's many register/login calls across IdentityEndpointsTests all
                // share one TestServer connection (same loopback RemoteIpAddress, same rate
                // limiter partition) — raised well above the production default so none of those
                // functional tests trip the limiter. Dedicated low-limit coverage of the
                // limiter itself lives in AuthRateLimitingApiFixture/AuthRateLimitingTests.
                ["RateLimiting:Register:PermitLimit"] = "1000",
                ["RateLimiting:Register:WindowSeconds"] = "60",
                ["RateLimiting:Login:PermitLimit"] = "1000",
                ["RateLimiting:Login:WindowSeconds"] = "60",
                ["Frontend:BaseUrl"] = "http://localhost:3000"
            });
        });
    }

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();

        _wolverineConnectionStrings = WolverineTestConnectionStrings.Configure(_postgres.GetConnectionString());

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await db.Database.MigrateAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
        _wolverineConnectionStrings?.Dispose();
    }
}
