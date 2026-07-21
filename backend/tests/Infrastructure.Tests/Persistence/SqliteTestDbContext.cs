using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TutorFlow.Infrastructure.Persistence;

namespace TutorFlow.Infrastructure.Tests.Persistence;

// Test-only helper: an open, in-memory SQLite connection backing a real
// TutorFlowDbContext. SQLite is also relational/ACID and enforces the same
// unique-constraint mechanism ADR-014 selected, so this exercises the real
// EF Core mapping and constraint behavior without requiring a PostgreSQL
// server (docs/adr/ADR-013-persistence-technology.md remains the production
// technology; this is a test-tooling substitution only).
internal sealed class SqliteTestDbContext : IDisposable
{
    private readonly SqliteConnection _connection;

    public TutorFlowDbContext DbContext { get; }

    public SqliteTestDbContext()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TutorFlowDbContext>()
            .UseSqlite(_connection)
            .Options;

        DbContext = new TutorFlowDbContext(options);
        DbContext.Database.EnsureCreated();
    }

    public void Dispose()
    {
        DbContext.Dispose();
        _connection.Dispose();
    }
}
