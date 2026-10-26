using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using TutorFlow.Host;
using TutorFlow.Modules.Search.Persistence;

namespace TutorFlow.Modules.Search.Tests;

public sealed class SearchApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18").Build();

    private IDisposable? _wolverineConnectionStrings;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SearchDb"] = _postgres.GetConnectionString(),
            });
        });
    }

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();

        // A44: see WolverineTestConnectionStrings' own doc comment for why this must be set via
        // environment variables, before Services is first touched below.
        _wolverineConnectionStrings = WolverineTestConnectionStrings.Configure(_postgres.GetConnectionString());

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SearchDbContext>();
        await db.Database.MigrateAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
        _wolverineConnectionStrings?.Dispose();
    }
}
