using TutorFlow.Domain.Common;

namespace TutorFlow.Application.Common;

// A listener registered with IDomainEventDispatcher
// (docs/adr/ADR-012-domain-event-dispatch-semantics.md). Zero implementations
// exist yet — the first will be Audit's, once
// docs/adr/ADR-016-audit-durability-strategy.md is accepted.
public interface IDomainEventHandler
{
    Task HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
}
