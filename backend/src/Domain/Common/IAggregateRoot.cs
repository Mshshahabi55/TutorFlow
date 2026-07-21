namespace TutorFlow.Domain.Common;

// Marker for a consistency-boundary root, per DOMAIN_MODEL.md: Aggregates
// and docs/adr/ADR-002-domain-boundaries.md (each aggregate has exactly one
// owning bounded context). Exposes DomainEvents/ClearDomainEvents so the
// Application layer can complete the event lifecycle (docs/adr/ADR-006-domain-events.md)
// without depending on any concrete aggregate type. Every AggregateRoot<TId>
// already implements both members; this only lets Application observe them
// through the interface.
public interface IAggregateRoot
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    void ClearDomainEvents();
}
