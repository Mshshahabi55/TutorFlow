using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TutorFlow.Application.Common;
using TutorFlow.Domain.Common;

namespace TutorFlow.Infrastructure.Persistence;

// Real transaction boundary (docs/adr/ADR-013-persistence-technology.md;
// docs/adr/ADR-015-transaction-boundary-confirmation.md). Hands touched
// aggregates' Domain Events to the injected IDomainEventDispatcher before
// clearing them and committing — the same lifecycle InMemoryUnitOfWork
// established, now backed by a real, atomic SaveChangesAsync call, so a
// future Dispatch consumer added to the same DbContext would be saved
// together with the aggregate mutation. Translates a unique-constraint
// violation (docs/adr/ADR-014-concurrency-control-strategy.md) into
// ConcurrencyConflictException so Application can react without depending on
// any database-provider type.
internal sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly TutorFlowDbContext _dbContext;
    private readonly IDomainEventDispatcher _domainEventDispatcher;

    public EfUnitOfWork(TutorFlowDbContext dbContext, IDomainEventDispatcher domainEventDispatcher)
    {
        _dbContext = dbContext;
        _domainEventDispatcher = domainEventDispatcher;
    }

    public async Task<int> SaveChangesAsync(IEnumerable<IAggregateRoot> touchedAggregates, CancellationToken cancellationToken = default)
    {
        var aggregates = touchedAggregates as IReadOnlyCollection<IAggregateRoot> ?? touchedAggregates.ToList();

        await _domainEventDispatcher.DispatchAsync(aggregates, cancellationToken);

        foreach (var aggregate in aggregates)
        {
            aggregate.ClearDomainEvents();
        }

        try
        {
            return await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new ConcurrencyConflictException(
                "A concurrent write conflicted with an existing uniqueness constraint.", ex);
        }
    }

    // Narrow, provider-aware detection so only a genuine uniqueness
    // violation is reclassified as a concurrency conflict — any other
    // DbUpdateException (a foreign-key violation, a connection fault, etc.)
    // remains an unclassified failure, per ADR-008's Domain-Error-vs-
    // Infrastructure-Failure distinction.
    private static bool IsUniqueConstraintViolation(DbUpdateException ex) => ex.InnerException switch
    {
        PostgresException postgres => postgres.SqlState == PostgresErrorCodes.UniqueViolation,
        SqliteException sqlite => sqlite.SqliteErrorCode == 19 && sqlite.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase),
        _ => false
    };
}
