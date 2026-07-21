using TutorFlow.Domain.Common;
using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Domain.Scheduling.Events;

// A Tutor declares an Availability Slot (PRODUCT_REQUIREMENTS.md SCH-3, SCH-5;
// DOMAIN_MODEL.md: Domain Events).
public sealed record AvailabilityDeclared(
    AvailabilitySlotId AvailabilitySlotId,
    TutorId TutorId) : DomainEvent;
