using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Infrastructure.Persistence;

namespace TutorFlow.Infrastructure.Tests.Postgres;

// PHASE-02: these tests connect to a REAL PostgreSQL instance (tutorflow_test),
// never SQLite — Npgsql is stricter than SQLite in ways that matter (see
// docs/phases/PHASE-02-REPORT.md Section 4), and that divergence is invisible
// to every other test in this solution.
//
// If TUTORFLOW_TEST_CONNECTION is not set, every test using this fixture
// FAILS LOUDLY with a clear message in its constructor — it must never
// silently skip. A skipped test that looks green in a CI summary is exactly
// the failure mode this project has been fighting since Phase 1
// (docs/phases/PHASE-01-REPORT.md).
public sealed class PostgresTestFixture
{
    public const string ConnectionStringEnvVar = "TUTORFLOW_TEST_CONNECTION";

    public string ConnectionString { get; }

    public PostgresTestFixture()
    {
        ConnectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvVar)
            ?? throw new InvalidOperationException(
                $"Environment variable '{ConnectionStringEnvVar}' is not set. The Postgres-backed " +
                "integration tests in this directory require a real PostgreSQL database (see " +
                "README.md's \"Database setup\" section) and must fail, not skip, when it isn't " +
                "configured.");
    }

    // Builds the exact same DI graph Program.cs composes in production
    // (DependencyInjection.AddInfrastructure), pointed at Npgsql instead of
    // a mock or SQLite — real EfUnitOfWork, real AuditDomainEventHandler,
    // real repositories, real DomainEventDispatcher.
    public ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(options => options.UseNpgsql(ConnectionString), isDevelopment: false);
        return services.BuildServiceProvider();
    }

    public async Task<TutorFlowDbContext> CreateFreshDbContextAsync()
    {
        var options = new DbContextOptionsBuilder<TutorFlowDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        var dbContext = new TutorFlowDbContext(options);

        // Applies the real migrations (not EnsureCreated, which bypasses
        // __EFMigrationsHistory) so this test database's schema is exactly
        // what dotnet ef database update produces in Task 3 — idempotent,
        // safe to call before every test.
        await dbContext.Database.MigrateAsync();
        return dbContext;
    }
}
