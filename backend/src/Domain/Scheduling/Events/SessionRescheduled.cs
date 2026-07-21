using TutorFlow.Domain.Common;
using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Domain.Scheduling.Events;

// A Session's time is changed by a permitted actor (PRODUCT_REQUIREMENTS.md
// SCH-7; DOMAIN_MODEL.md: Domain Events).
public sealed record SessionRescheduled(SessionId SessionId, DateTime NewScheduledTimeUtc) : DomainEvent;
