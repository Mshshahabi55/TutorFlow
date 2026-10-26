using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using TutorFlow.Host;
using TutorFlow.Modules.Subjects.Persistence;

namespace TutorFlow.Modules.Subjects.Tests;

public sealed class SubjectsApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18").Build();

    private IDisposable? _wolverineConnectionStrings;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SubjectsDb"] = _postgres.GetConnectionString(),
            });
        });
    }

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();

        // A44: Program.cs's UseWolverine block reads WolverineDb/BookingsDb/AvailabilityDb/
        // TutorsDb/PaymentsDb eagerly, before WebApplicationFactory's ConfigureAppConfiguration
        // override is ever applied (see the A44 blocker investigation) — environment variables
        // are the one source visible at that point. Must be set before Services is first
        // touched below.
        _wolverineConnectionStrings = WolverineTestConnectionStrings.Configure(_postgres.GetConnectionString());

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SubjectsDbContext>();
        await db.Database.MigrateAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
        _wolverineConnectionStrings?.Dispose();
    }
}
