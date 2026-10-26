using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.PostgreSql;
using TutorFlow.Host;
using TutorFlow.Modules.Availability.Persistence;
using TutorFlow.Modules.Messaging.Persistence;
using TutorFlow.Modules.Tutors.Persistence;

namespace TutorFlow.Modules.Messaging.Tests;

/// <summary>
/// Three Postgres containers — Messaging's own, plus Availability (whose
/// <c>ITutorAvailabilityQuery.IsTutorBookableAsync</c>, ADR-0030, the conversation-creation
/// handler resolves via real DI) and Tutors (whose <c>ITutorProfileSummaryQuery</c>, ADR-0030
/// Batch 69, the list/messages handlers now resolve via the same real DI) — through the real Host
/// composition root, proving real cross-module wiring, not a mocked substitute, same precedent as
/// <c>NotificationsApiFixture</c>/<c>BookingsApiFixture</c>. Generous rate limits (far above
/// anything a single functional test sends) so this fixture is never accidentally tripped by
/// tests that aren't about rate limiting — see <see cref="MessagingRateLimitingApiFixture"/> for
/// that.
/// </summary>
public sealed class MessagingApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string JwtIssuer = "TutorFlow.Tests";
    public const string JwtAudience = "TutorFlow.Tests";
    public const string JwtKey = "messaging-integration-test-signing-key-at-least-32-bytes-long";

    private readonly PostgreSqlContainer _messagingPostgres = new PostgreSqlBuilder("postgres:18").Build();
    private readonly PostgreSqlContainer _availabilityPostgres = new PostgreSqlBuilder("postgres:18").Build();
    private readonly PostgreSqlContainer _tutorsPostgres = new PostgreSqlBuilder("postgres:18").Build();

    private IDisposable? _wolverineConnectionStrings;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:MessagingDb"] = _messagingPostgres.GetConnectionString(),
                ["ConnectionStrings:AvailabilityDb"] = _availabilityPostgres.GetConnectionString(),
                ["ConnectionStrings:TutorsDb"] = _tutorsPostgres.GetConnectionString(),
                ["Jwt:Issuer"] = JwtIssuer,
                ["Jwt:Audience"] = JwtAudience,
                ["Jwt:Key"] = JwtKey,
                ["Jwt:ExpirationMinutes"] = "60",
                ["RateLimiting:MessagingCreateConversation:PermitLimit"] = "1000",
                ["RateLimiting:MessagingCreateConversation:WindowSeconds"] = "3600",
                ["RateLimiting:MessagingSendMessage:PermitLimit"] = "1000",
                ["RateLimiting:MessagingSendMessage:WindowSeconds"] = "60"
            });
        });
    }

    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(_messagingPostgres.StartAsync(), _availabilityPostgres.StartAsync(), _tutorsPostgres.StartAsync());

        _wolverineConnectionStrings = WolverineTestConnectionStrings.Configure(
            _messagingPostgres.GetConnectionString(),
            availabilityDb: _availabilityPostgres.GetConnectionString(),
            tutorsDb: _tutorsPostgres.GetConnectionString());

        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<MessagingDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<AvailabilityDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<TutorsDbContext>().Database.MigrateAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        await Task.WhenAll(
            _messagingPostgres.DisposeAsync().AsTask(),
            _availabilityPostgres.DisposeAsync().AsTask(),
            _tutorsPostgres.DisposeAsync().AsTask());
        await base.DisposeAsync();
        _wolverineConnectionStrings?.Dispose();
    }

    public static string CreateAccessToken(Guid userId, params string[] roles)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = JwtIssuer,
            Audience = JwtAudience,
            Expires = DateTime.UtcNow.AddHours(1),
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = userId.ToString(),
                [ClaimTypes.NameIdentifier] = userId.ToString(),
                [ClaimTypes.Role] = roles
            },
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey)),
                SecurityAlgorithms.HmacSha256)
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
