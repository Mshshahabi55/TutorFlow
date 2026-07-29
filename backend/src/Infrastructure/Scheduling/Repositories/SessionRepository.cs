using Microsoft.EntityFrameworkCore;
using TutorFlow.Application.Common;
using TutorFlow.Application.Scheduling.Interfaces;
using TutorFlow.Domain.Scheduling;
using TutorFlow.Domain.Scheduling.ValueObjects;
using TutorFlow.Infrastructure.Persistence;

namespace TutorFlow.Infrastructure.Scheduling.Repositories;

internal sealed class SessionRepository : ISessionRepository
{
    private readonly TutorFlowDbContext _dbContext;

    public SessionRepository(TutorFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Session?> GetByIdAsync(SessionId id, CancellationToken cancellationToken = default) =>
        _dbContext.Sessions.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<(IReadOnlyCollection<Session> Items, int TotalCount)> GetAllAsync(
        PageRequest pageRequest, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Sessions.AsNoTracking();

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageRequest.Page - 1) * pageRequest.PageSize)
            .Take(pageRequest.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyCollection<Session>> GetByTutorIdAsync(TutorId tutorId, CancellationToken cancellationToken = default) =>
        await _dbContext.Sessions.AsNoTracking().Where(s => s.TutorId == tutorId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<Session>> GetByStudentIdAsync(StudentId studentId, CancellationToken cancellationToken = default) =>
        await _dbContext.Sessions.AsNoTracking().Where(s => s.StudentId == studentId).ToListAsync(cancellationToken);

    public Task<Session?> GetByAvailabilitySlotIdAsync(
        AvailabilitySlotId availabilitySlotId, CancellationToken cancellationToken = default) =>
        _dbContext.Sessions.FirstOrDefaultAsync(s => s.AvailabilitySlotId == availabilitySlotId, cancellationToken);

    public async Task<IReadOnlyDictionary<SessionStatus, int>> GetStatusCountsAsync(CancellationToken cancellationToken = default)
    {
        var counts = await _dbContext.Sessions
            .AsNoTracking()
            .GroupBy(s => s.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return counts.ToDictionary(c => c.Status, c => c.Count);
    }

    public Task AddAsync(Session session, CancellationToken cancellationToken = default)
    {
        _dbContext.Sessions.Add(session);
        return Task.CompletedTask;
    }
}
