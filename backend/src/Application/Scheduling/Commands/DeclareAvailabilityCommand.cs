using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Application.Scheduling.Commands;

// Corresponds to AvailabilitySlot.Declare(...) (PRODUCT_REQUIREMENTS.md SCH-3, SCH-5).
public sealed record DeclareAvailabilityCommand(
    Guid TutorId,
    DateTime StartTimeUtc,
    TimeSpan Duration,
    DeliveryMode DeliveryMode);
