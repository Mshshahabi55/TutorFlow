using TutorFlow.Domain.Common;
using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Domain.Scheduling.Events;

// A Session is cancelled by a permitted actor (PRODUCT_REQUIREMENTS.md SCH-7;
// DOMAIN_MODEL.md: Domain Events).
public sealed record SessionCancelled(SessionId SessionId) : DomainEvent;
