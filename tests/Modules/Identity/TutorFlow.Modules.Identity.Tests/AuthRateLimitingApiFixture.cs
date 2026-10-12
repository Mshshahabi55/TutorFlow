using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using TutorFlow.Modules.Identity.Persistence;

namespace TutorFlow.Modules.Identity.Tests;

/// <summary>
/// A dedicated instance (own container, own DI container, own rate-limiter state) with a tight,
/// deterministic limit — kept entirely separate from <see cref="IdentityApiFixture"/>, whose
/// generous override exists so its unrelated functional tests never trip the limiter.
/// </summary>
public sealed class AuthRateLimitingApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const int PermitLimit = 3;
    public const string SimulatedClientIpHeader = "X-Test-Simulated-Client-Ip";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18").Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:IdentityDb"] = _postgres.GetConnectionString(),
                ["Jwt:Issuer"] = "TutorFlow.Tests",
                ["Jwt:Audience"] = "TutorFlow.Tests",
                ["Jwt:Key"] = "integration-test-signing-key-at-least-32-bytes-long",
                ["Jwt:ExpirationMinutes"] = "60",
                ["RateLimiting:Register:PermitLimit"] = PermitLimit.ToString(),
                ["RateLimiting:Register:WindowSeconds"] = "60",
                ["RateLimiting:Login:PermitLimit"] = PermitLimit.ToString(),
                ["RateLimiting:Login:WindowSeconds"] = "60"
            });
        });

        // TestServer has no real socket, so every request shares one fixed loopback
        // RemoteIpAddress — this test-only filter lets a test simulate distinct source IPs (via
        // an explicit opt-in header, never trusted in production) so the partition-key behavior
        // itself can be exercised, not just the single-partition reject/accept path. Prepended
        // ahead of everything Program.cs registers, including UseRateLimiter(), since it's the
        // only IStartupFilter added here.
        builder.ConfigureTestServices(services =>
            services.AddTransient<IStartupFilter, SimulatedClientIpStartupFilter>());
    }

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await db.Database.MigrateAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }

    private sealed class SimulatedClientIpStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use(async (context, nextMiddleware) =>
            {
                if (context.Request.Headers.TryGetValue(SimulatedClientIpHeader, out var simulatedIp) &&
                    IPAddress.TryParse(simulatedIp.ToString(), out var parsedIp))
                {
                    context.Connection.RemoteIpAddress = parsedIp;
                }

                await nextMiddleware();
            });

            next(app);
        };
    }
}
