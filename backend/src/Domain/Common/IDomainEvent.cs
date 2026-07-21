namespace TutorFlow.Domain.Common;

// Marker for the Domain Event concept fixed in docs/adr/ADR-006-domain-events.md.
// No shape beyond this is established yet; per-event data belongs to each
// owning bounded context, not to this shared abstraction.
public interface IDomainEvent
{
}
