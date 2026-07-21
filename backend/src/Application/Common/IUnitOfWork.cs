using TutorFlow.Domain.Common;

namespace TutorFlow.Application.Common;

// The Application layer demarcates its own bounded context's transaction
// boundary (docs/adr/ADR-004-persistence-strategy.md: Transaction Boundaries);
// this is the interface it invokes to do so. Infrastructure implements it —
// no persistence mechanism is chosen here.
//
// touchedAggregates: every aggregate the calling use case created or
// mutated, so the implementation can complete the Domain Event lifecycle
// (harvest/clear) at the same point it commits the unit of work
// (docs/adr/ADR-006-domain-events.md). This phase only guarantees events are
// not left uncleared — it does not dispatch them to any consumer; that
// remains a later, separate phase.
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(IEnumerable<IAggregateRoot> touchedAggregates, CancellationToken cancellationToken = default);
}
