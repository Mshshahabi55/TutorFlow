using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using TutorFlow.Modules.Availability.Persistence;
using TutorFlow.Modules.Bookings.Persistence;
using TutorFlow.Modules.Search.Persistence;
using TutorFlow.Modules.Tutors.Persistence;

namespace TutorFlow.Host.Tests;

/// <summary>
/// The only test fixture in this repo that boots four modules' real schemas against one
/// Testcontainers Postgres database (different schema per DbContext, same database) — needed
/// because the Tutors -&gt; Search/Availability Wolverine pipeline (ADR-0021, extended by
/// ADR-0024) is the first cross-module integration this codebase has, and a single-module
/// fixture can't prove it end-to-end: the Host-wide Wolverine discovery tries to invoke every
/// subscriber's consumer even from a Tutors-only test run, which fails without each one's real
/// connection string configured. BookingsDb is included because Availability's availability-
/// signal computation transitively calls Bookings' IActiveBookingIntervalsQuery (ADR-0014).
/// </summary>
public sealed class TutorPublishedPipelineFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18").Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:TutorsDb"] = _postgres.GetConnectionString(),
                ["ConnectionStrings:SearchDb"] = _postgres.GetConnectionString(),
                ["ConnectionStrings:AvailabilityDb"] = _postgres.GetConnectionString(),
                ["ConnectionStrings:BookingsDb"] = _postgres.GetConnectionString(),
            });
        });
    }

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();

        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<TutorsDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<SearchDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<AvailabilityDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<BookingsDbContext>().Database.MigrateAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }
}
