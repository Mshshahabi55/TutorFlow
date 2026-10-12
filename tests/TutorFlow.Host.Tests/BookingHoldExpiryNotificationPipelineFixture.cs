using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using TutorFlow.Modules.Bookings.Persistence;
using TutorFlow.Modules.Identity.Persistence;
using TutorFlow.Modules.Notifications.Infrastructure;
using TutorFlow.Modules.Notifications.Persistence;
using TutorFlow.Modules.Tutors.Persistence;

namespace TutorFlow.Host.Tests;

/// <summary>
/// ADR-0029: proves the real, end-to-end wiring — Bookings' <c>BookingHoldExpirySweepService</c>
/// publishes <c>BookingHoldExpired</c> via Wolverine, and <c>Notifications</c>' real consumer
/// receives it and attempts a real send — with no direct DbContext/project reference between any
/// of the three modules involved. Same shape as <c>TutorPublishedPipelineFixture</c>.
/// </summary>
public sealed class BookingHoldExpiryNotificationPipelineFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _bookingsPostgres = new PostgreSqlBuilder("postgres:18").Build();
    private readonly PostgreSqlContainer _notificationsPostgres = new PostgreSqlBuilder("postgres:18").Build();
    private readonly PostgreSqlContainer _identityPostgres = new PostgreSqlBuilder("postgres:18").Build();
    private readonly PostgreSqlContainer _tutorsPostgres = new PostgreSqlBuilder("postgres:18").Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:BookingsDb"] = _bookingsPostgres.GetConnectionString(),
                ["ConnectionStrings:NotificationsDb"] = _notificationsPostgres.GetConnectionString(),
                ["ConnectionStrings:IdentityDb"] = _identityPostgres.GetConnectionString(),
                ["ConnectionStrings:TutorsDb"] = _tutorsPostgres.GetConnectionString(),
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
            _bookingsPostgres.StartAsync(),
            _notificationsPostgres.StartAsync(),
            _identityPostgres.StartAsync(),
            _tutorsPostgres.StartAsync());

        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<BookingsDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<NotificationsDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<TutorsDbContext>().Database.MigrateAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        await Task.WhenAll(
            _bookingsPostgres.DisposeAsync().AsTask(),
            _notificationsPostgres.DisposeAsync().AsTask(),
            _identityPostgres.DisposeAsync().AsTask(),
            _tutorsPostgres.DisposeAsync().AsTask());
        await base.DisposeAsync();
    }
}
