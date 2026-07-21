using TutorFlow.Domain.Scheduling;
using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Application.Scheduling.Interfaces;

// Repository abstraction only (docs/adr/ADR-005-application-boundary.md).
// Infrastructure implements this in a later phase.
public interface IAvailabilitySlotRepository
{
    Task<AvailabilitySlot?> GetByIdAsync(AvailabilitySlotId id, CancellationToken cancellationToken = default);

    // Distinct TutorIds with at least one unconsumed slot starting at or
    // after fromUtc — serves Discovery's "Availability" search criterion
    // (DISC-1) without exposing AvailabilitySlot itself across the context
    // boundary (ADR-002: Integration Rules — a fact, read, not a copy).
    Task<IReadOnlyCollection<TutorId>> GetTutorIdsWithOpenSlotFromAsync(DateTime fromUtc, CancellationToken cancellationToken = default);

    // Every Availability Slot for a Tutor, regardless of consumption state
    // (Backend Completion Phase, Track A, Phase A2: List/Calendar). Mirrors
    // ISessionRepository.GetByTutorIdAsync's unpaginated shape exactly —
    // the closest existing precedent for "everything belonging to one Tutor."
    Task<IReadOnlyCollection<AvailabilitySlot>> GetByTutorIdAsync(TutorId tutorId, CancellationToken cancellationToken = default);

    Task AddAsync(AvailabilitySlot availabilitySlot, CancellationToken cancellationToken = default);
}
