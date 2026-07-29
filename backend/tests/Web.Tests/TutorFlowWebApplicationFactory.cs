using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TutorFlow.Infrastructure.Persistence;

namespace TutorFlow.Web.Tests;

// Overrides the production PostgreSQL DbContext registration
// (docs/adr/ADR-013-persistence-technology.md) with an in-memory SQLite
// database for these end-to-end tests only, since no PostgreSQL server is
// available in this environment. SQLite is also relational/ACID and
// exercises the same EF Core mapping and constraint mechanism; production
// configuration and behavior are otherwise untouched.
public sealed class TutorFlowWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        // Ensures Program.cs skips its Npgsql UseInfrastructure(...) branch
        // entirely (see Program.cs) — Npgsql's services are then never
        // added, so there is nothing to remove before this factory's own
        // SQLite registration below.
        builder.UseEnvironment("Testing");

        // docs/adr/ADR-023-online-lesson-meeting-provider-architecture.md:
        // MockMeetingProvider is always registered for the "Testing"
        // environment (see Program.cs) — pointing the default provider at
        // it here lets Web.Tests exercise the real "Start Lesson" success
        // path end-to-end without real vendor credentials.
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Meeting:DefaultProvider"] = "Mock",
                // Deliberately far above appsettings.json's production
                // defaults — every Web.Tests class shares one client IP
                // (the in-memory TestServer's), and many legitimately fire
                // dozens of requests (including repeated /auth/login calls
                // via RegisterAndLoginTutorAsync-style helpers) within a
                // single test class's lifetime. RateLimitingTests.cs is the
                // one place that needs the real, tight limits — it opts
                // back into them itself via WithWebHostBuilder rather than
                // this shared factory ever throttling ordinary test traffic.
                ["RateLimiting:General:PermitLimit"] = "100000",
                ["RateLimiting:General:WindowSeconds"] = "10",
                ["RateLimiting:Auth:PermitLimit"] = "100000",
                ["RateLimiting:Auth:WindowSeconds"] = "60",
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<TutorFlowDbContext>>();
            services.RemoveAll<TutorFlowDbContext>();

            services.AddDbContext<TutorFlowDbContext>(options => options.UseSqlite(_connection));

            using var scope = services.BuildServiceProvider().CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TutorFlowDbContext>();
            dbContext.Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }

    // Called from each test class's IAsyncLifetime.InitializeAsync() so every
    // [Fact] runs against a genuinely empty database, rather than one
    // accumulating rows from every prior test method in the class (see
    // docs/phases/PHASE-01-REPORT.md Section 3 for why the previous
    // one-database-per-class-lifetime setup produced order-dependent
    // failures). Cheaper than rebuilding the whole WebApplicationFactory/host
    // per test: reuses the already-open connection and just drops/recreates
    // schema on it.
    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TutorFlowDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();
    }
}
