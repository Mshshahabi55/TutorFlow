using TutorFlow.Application.Common;
using TutorFlow.Domain.Common;

namespace TutorFlow.Infrastructure.Common;

// Implements the four decisions of docs/adr/ADR-012-domain-event-dispatch-semantics.md:
// 1. Synchronous, in-process — every handler is awaited inline, no queue, no broker.
// 2. Ordering — each aggregate's own DomainEvents are dispatched in raised
//    order; no ordering is imposed across different aggregates.
// 3. Failure propagates — a handler exception is not caught here; it
//    propagates to EfUnitOfWork.SaveChangesAsync, failing the transaction.
// 4. Exactly one attempt — no retry, no redelivery.
// Zero IDomainEventHandler implementations are registered yet, so this is
// currently a no-op in practice — the mechanism is real, the consumers are
// not (docs/adr/ADR-016-audit-durability-strategy.md remains Proposed).
internal sealed class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IReadOnlyCollection<IDomainEventHandler> _handlers;

    public DomainEventDispatcher(IEnumerable<IDomainEventHandler> handlers)
    {
        _handlers = handlers.ToList();
    }

    public async Task DispatchAsync(IReadOnlyCollection<IAggregateRoot> touchedAggregates, CancellationToken cancellationToken = default)
    {
        foreach (var aggregate in touchedAggregates)
        {
            foreach (var domainEvent in aggregate.DomainEvents)
            {
                foreach (var handler in _handlers)
                {
                    await handler.HandleAsync(domainEvent, cancellationToken);
                }
            }
        }
    }
}
