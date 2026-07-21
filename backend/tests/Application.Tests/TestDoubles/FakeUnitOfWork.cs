using TutorFlow.Application.Common;
using TutorFlow.Domain.Common;

namespace TutorFlow.Application.Tests.TestDoubles;

// Minimal hand-rolled test double — no mocking framework. Tracks how many
// times SaveChangesAsync was called so tests can verify handlers actually
// commit their work. Mirrors EfUnitOfWork's event-clearing behavior so
// tests observe the same lifecycle guarantee as production.
internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveChangesCallCount { get; private set; }

    public Task<int> SaveChangesAsync(IEnumerable<IAggregateRoot> touchedAggregates, CancellationToken cancellationToken = default)
    {
        SaveChangesCallCount++;

        foreach (var aggregate in touchedAggregates)
        {
            aggregate.ClearDomainEvents();
        }

        return Task.FromResult(0);
    }
}
