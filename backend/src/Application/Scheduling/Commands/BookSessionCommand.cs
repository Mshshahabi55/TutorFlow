namespace TutorFlow.Application.Scheduling.Commands;

// Corresponds to Session.Book(...) (PRODUCT_REQUIREMENTS.md SCH-5). Tutor,
// time, duration, and delivery mode are not passed here — the handler derives
// them from the referenced Availability Slot, since a booking is always
// against an already-declared slot's own data (DOMAIN_MODEL.md: Relationships,
// "Availability Slot → Session").
public sealed record BookSessionCommand(
    Guid AvailabilitySlotId,
    Guid StudentId,
    Guid? ParentGuardianId);
