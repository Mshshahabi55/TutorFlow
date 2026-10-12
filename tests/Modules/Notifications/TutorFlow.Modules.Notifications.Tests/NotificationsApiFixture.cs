using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using TutorFlow.Modules.Identity.Persistence;
using TutorFlow.Modules.Notifications.Infrastructure;
using TutorFlow.Modules.Notifications.Persistence;
using TutorFlow.Modules.Tutors.Persistence;

namespace TutorFlow.Modules.Notifications.Tests;

/// <summary>
/// Three Postgres containers — Notifications' own, plus Identity and Tutors, the two modules
/// whose Shared query contracts (<c>IUserEmailQuery</c>, <c>ITutorProfileSummaryQuery</c>) the
/// recovery-email handler actually resolves via real DI (ADR-0029) — same precedent as
/// <c>BookingsApiFixture</c>'s multi-container shape: proves real cross-module wiring, not a
/// mocked substitute. <see cref="TestEmailSender"/> is re-registered as a singleton (the module
/// itself registers IEmailSender scoped) so tests can retrieve the exact instance a handler call
/// sent through and inspect <see cref="TestEmailSender.SentMessages"/>.
/// </summary>
public sealed class NotificationsApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string JwtIssuer = "TutorFlow.Tests";
    public const string JwtAudience = "TutorFlow.Tests";
    public const string JwtKey = "notifications-integration-test-signing-key-at-least-32-bytes";

    private readonly PostgreSqlContainer _notificationsPostgres = new PostgreSqlBuilder("postgres:18").Build();
    private readonly PostgreSqlContainer _identityPostgres = new PostgreSqlBuilder("postgres:18").Build();
    private readonly PostgreSqlContainer _tutorsPostgres = new PostgreSqlBuilder("postgres:18").Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:NotificationsDb"] = _notificationsPostgres.GetConnectionString(),
                ["ConnectionStrings:IdentityDb"] = _identityPostgres.GetConnectionString(),
                ["ConnectionStrings:TutorsDb"] = _tutorsPostgres.GetConnectionString(),
                ["Jwt:Issuer"] = JwtIssuer,
                ["Jwt:Audience"] = JwtAudience,
                ["Jwt:Key"] = JwtKey,
                ["Jwt:ExpirationMinutes"] = "60"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender, TestEmailSender>();
        });
    }

    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(
            _notificationsPostgres.StartAsync(),
            _identityPostgres.StartAsync(),
            _tutorsPostgres.StartAsync());

        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<NotificationsDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<TutorsDbContext>().Database.MigrateAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        await Task.WhenAll(
            _notificationsPostgres.DisposeAsync().AsTask(),
            _identityPostgres.DisposeAsync().AsTask(),
            _tutorsPostgres.DisposeAsync().AsTask());
        await base.DisposeAsync();
    }
}
