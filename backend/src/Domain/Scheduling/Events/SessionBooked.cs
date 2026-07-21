using TutorFlow.Domain.Common;
using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Domain.Scheduling.Events;

// A Student/Parent books an Availability Slot, producing a Session in
// Scheduled status (PRODUCT_REQUIREMENTS.md SCH-5, SCH-6; DOMAIN_MODEL.md:
// Domain Events).
public sealed record SessionBooked(
    SessionId SessionId,
    TutorId TutorId,
    StudentId StudentId,
    AvailabilitySlotId AvailabilitySlotId) : DomainEvent;
