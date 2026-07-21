using TutorFlow.Domain.Scheduling;
using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Application.Scheduling.DTOs;

// Application's own translated representation of an Availability Slot —
// never the raw Domain object itself (docs/adr/ADR-010-api-boundary.md).
public sealed record AvailabilitySlotDto(
    Guid AvailabilitySlotId,
    Guid TutorId,
    DateTime StartTimeUtc,
    DateTime EndTimeUtc,
    TimeSpan Duration,
    DeliveryMode DeliveryMode,
    // Exposes the Domain aggregate's own already-existing IsConsumed fact
    // (Backend Completion Phase, Track A, Phase A2) — necessary for a List/
    // Calendar view to distinguish open from booked slots. Not a new
    // business rule: the value was already computed and enforced by
    // AvailabilitySlot.Book(); this only makes it visible for the first time.
    bool IsConsumed)
{
    public static AvailabilitySlotDto FromDomain(AvailabilitySlot slot) => new(
        slot.Id.Value,
        slot.TutorId.Value,
        slot.StartTimeUtc,
        slot.EndTimeUtc,
        slot.Duration.Value,
        slot.DeliveryMode,
        slot.IsConsumed);
}
