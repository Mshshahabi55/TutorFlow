using TutorFlow.Domain.Common;
using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Domain.Scheduling.Events;

// A Session's status is set to Completed (PRODUCT_REQUIREMENTS.md SCH-6;
// DOMAIN_MODEL.md: Domain Events). Which role may trigger this transition is
// not established (DOMAIN_MODEL.md Open Question 6) — this event records
// only that the transition occurred.
public sealed record SessionCompleted(SessionId SessionId) : DomainEvent;
