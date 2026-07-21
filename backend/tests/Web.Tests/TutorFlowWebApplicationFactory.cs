using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
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
}
