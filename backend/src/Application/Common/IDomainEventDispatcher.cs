using TutorFlow.Domain.Common;

namespace TutorFlow.Application.Common;

// The integration point through which the Application layer's transaction
// boundary (IUnitOfWork) hands off touched aggregates' raised Domain Events
// (docs/adr/ADR-006-domain-events.md) to whatever is listening.
//
// This is a contract only — it carries no delivery policy. It does not
// establish synchronous or asynchronous delivery, a retry/redelivery
// guarantee, or any ordering beyond what each aggregate's own DomainEvents
// collection already provides. Those questions remain open
// (docs/adr/ADR-012-domain-event-dispatch-semantics.md) and are decided by
// a future implementation of this interface, not by the interface itself.
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IReadOnlyCollection<IAggregateRoot> touchedAggregates, CancellationToken cancellationToken = default);
}
