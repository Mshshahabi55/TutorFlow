namespace TutorFlow.Domain.Common;

// A Domain Event is raised only once the transaction that made the underlying
// fact true has completed (docs/adr/ADR-006-domain-events.md: Transaction
// Relationship), so its timestamp defaults to the moment it is constructed.
public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();

    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
