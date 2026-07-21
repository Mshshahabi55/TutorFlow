using TutorFlow.Application.Scheduling.Interfaces;
using TutorFlow.Domain.Scheduling;
using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Application.Tests.TestDoubles;

internal sealed class InMemoryAvailabilitySlotRepository : IAvailabilitySlotRepository
{
    private readonly Dictionary<Guid, AvailabilitySlot> _availabilitySlots = new();

    public Task<AvailabilitySlot?> GetByIdAsync(AvailabilitySlotId id, CancellationToken cancellationToken = default)
    {
        _availabilitySlots.TryGetValue(id.Value, out var availabilitySlot);
        return Task.FromResult(availabilitySlot);
    }

    public Task<IReadOnlyCollection<TutorId>> GetTutorIdsWithOpenSlotFromAsync(DateTime fromUtc, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<TutorId> tutorIds = _availabilitySlots.Values
            .Where(s => !s.IsConsumed && s.StartTimeUtc >= fromUtc)
            .Select(s => s.TutorId)
            .Distinct()
            .ToList();
        return Task.FromResult(tutorIds);
    }

    public Task<IReadOnlyCollection<AvailabilitySlot>> GetByTutorIdAsync(TutorId tutorId, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<AvailabilitySlot> slots = _availabilitySlots.Values
            .Where(s => s.TutorId == tutorId)
            .OrderBy(s => s.StartTimeUtc)
            .ToList();
        return Task.FromResult(slots);
    }

    public Task AddAsync(AvailabilitySlot availabilitySlot, CancellationToken cancellationToken = default)
    {
        _availabilitySlots[availabilitySlot.Id.Value] = availabilitySlot;
        return Task.CompletedTask;
    }
}
