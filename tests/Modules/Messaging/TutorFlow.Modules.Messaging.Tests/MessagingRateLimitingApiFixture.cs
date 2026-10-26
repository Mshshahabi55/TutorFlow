using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using TutorFlow.Host;
using TutorFlow.Modules.Availability.Persistence;
using TutorFlow.Modules.Messaging.Persistence;

namespace TutorFlow.Modules.Messaging.Tests;

/// <summary>
/// A dedicated instance (own containers, own rate-limiter state) with a tight, deterministic
/// limit — kept entirely separate from <see cref="MessagingApiFixture"/>, whose generous override
/// exists so its unrelated functional tests never trip the limiter. Mirrors
/// <c>AuthRateLimitingApiFixture</c>'s identical rationale. Unlike auth's IP-partitioned policies,
/// Messaging's policies partition by the authenticated caller's UserId claim, so no simulated-IP
/// header is needed here — each distinct test user already gets its own partition for free.
/// </summary>
public sealed class MessagingRateLimitingApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const int CreateConversationPermitLimit = 3;
    public const int SendMessagePermitLimit = 3;

    private readonly PostgreSqlContainer _messagingPostgres = new PostgreSqlBuilder("postgres:18").Build();
    private readonly PostgreSqlContainer _availabilityPostgres = new PostgreSqlBuilder("postgres:18").Build();

    private IDisposable? _wolverineConnectionStrings;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:MessagingDb"] = _messagingPostgres.GetConnectionString(),
                ["ConnectionStrings:AvailabilityDb"] = _availabilityPostgres.GetConnectionString(),
                ["Jwt:Issuer"] = MessagingApiFixture.JwtIssuer,
                ["Jwt:Audience"] = MessagingApiFixture.JwtAudience,
                ["Jwt:Key"] = MessagingApiFixture.JwtKey,
                ["Jwt:ExpirationMinutes"] = "60",
                ["RateLimiting:MessagingCreateConversation:PermitLimit"] = CreateConversationPermitLimit.ToString(),
                ["RateLimiting:MessagingCreateConversation:WindowSeconds"] = "3600",
                ["RateLimiting:MessagingSendMessage:PermitLimit"] = SendMessagePermitLimit.ToString(),
                ["RateLimiting:MessagingSendMessage:WindowSeconds"] = "60"
            });
        });
    }

    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(_messagingPostgres.StartAsync(), _availabilityPostgres.StartAsync());

        _wolverineConnectionStrings = WolverineTestConnectionStrings.Configure(
            _messagingPostgres.GetConnectionString(),
            availabilityDb: _availabilityPostgres.GetConnectionString());

        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<MessagingDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<AvailabilityDbContext>().Database.MigrateAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        await Task.WhenAll(
            _messagingPostgres.DisposeAsync().AsTask(),
            _availabilityPostgres.DisposeAsync().AsTask());
        await base.DisposeAsync();
        _wolverineConnectionStrings?.Dispose();
    }
}
