namespace TutorFlow.Domain.Common;

// Only an aggregate root raises Domain Events (docs/adr/ADR-006-domain-events.md:
// "an event is raised by Domain only once ... the transaction that made the
// underlying fact true"). ClearDomainEvents is called by the Application layer
// once it has recognized and processed the raised events.
public abstract class AggregateRoot<TId> : Entity<TId>, IAggregateRoot where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = new();

    protected AggregateRoot() { }

    protected AggregateRoot(TId id) : base(id) { }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
