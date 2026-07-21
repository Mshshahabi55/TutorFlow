using Microsoft.EntityFrameworkCore;
using TutorFlow.Application.Scheduling.Interfaces;
using TutorFlow.Domain.Scheduling;
using TutorFlow.Domain.Scheduling.ValueObjects;
using TutorFlow.Infrastructure.Persistence;

namespace TutorFlow.Infrastructure.Scheduling.Repositories;

// Real persistence (docs/adr/ADR-013-persistence-technology.md). Never
// validates invariants or business rules — those belong to the Domain
// aggregate alone. CONST-1's physical enforcement lives in
// SessionConfiguration's unique index, not here.
internal sealed class AvailabilitySlotRepository : IAvailabilitySlotRepository
{
    private readonly TutorFlowDbContext _dbContext;

    public AvailabilitySlotRepository(TutorFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<AvailabilitySlot?> GetByIdAsync(AvailabilitySlotId id, CancellationToken cancellationToken = default) =>
        _dbContext.AvailabilitySlots.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<TutorId>> GetTutorIdsWithOpenSlotFromAsync(DateTime fromUtc, CancellationToken cancellationToken = default) =>
        await _dbContext.AvailabilitySlots
            .AsNoTracking()
            .Where(s => !s.IsConsumed && s.StartTimeUtc >= fromUtc)
            .Select(s => s.TutorId)
            .Distinct()
            .ToListAsync(cancellationToken);

    // Ordered by StartTimeUtc: the natural chronological order for a
    // List/Calendar view, and required for deterministic pagination in any
    // future paginated caller (Backend Completion Phase, Track A, Phase A2).
    public async Task<IReadOnlyCollection<AvailabilitySlot>> GetByTutorIdAsync(TutorId tutorId, CancellationToken cancellationToken = default) =>
        await _dbContext.AvailabilitySlots
            .AsNoTracking()
            .Where(s => s.TutorId == tutorId)
            .OrderBy(s => s.StartTimeUtc)
            .ToListAsync(cancellationToken);

    public Task AddAsync(AvailabilitySlot availabilitySlot, CancellationToken cancellationToken = default)
    {
        _dbContext.AvailabilitySlots.Add(availabilitySlot);
        return Task.CompletedTask;
    }
}
