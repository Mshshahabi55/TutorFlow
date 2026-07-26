namespace TutorFlow.Application.Scheduling.Commands;

// Phase 4.7: rescheduling targets an existing, open Availability Slot —
// exactly like BookSessionCommand — not an arbitrary timestamp
// (PRODUCT_REQUIREMENTS.md SCH-7; corresponds to Session.Reschedule).
public sealed record RescheduleSessionCommand(Guid SessionId, Guid NewAvailabilitySlotId);
