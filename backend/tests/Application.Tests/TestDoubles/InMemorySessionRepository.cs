using TutorFlow.Application.Common;
using TutorFlow.Application.Scheduling.Interfaces;
using TutorFlow.Domain.Scheduling;
using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Application.Tests.TestDoubles;

internal sealed class InMemorySessionRepository : ISessionRepository
{
    private readonly Dictionary<Guid, Session> _sessions = new();

    public Task<Session?> GetByIdAsync(SessionId id, CancellationToken cancellationToken = default)
    {
        _sessions.TryGetValue(id.Value, out var session);
        return Task.FromResult(session);
    }

    public Task<(IReadOnlyCollection<Session> Items, int TotalCount)> GetAllAsync(
        PageRequest pageRequest, CancellationToken cancellationToken = default)
    {
        var all = _sessions.Values.ToList();
        var page = all
            .Skip((pageRequest.Page - 1) * pageRequest.PageSize)
            .Take(pageRequest.PageSize)
            .ToList();
        return Task.FromResult<(IReadOnlyCollection<Session>, int)>((page, all.Count));
    }

    public Task<IReadOnlyCollection<Session>> GetByTutorIdAsync(TutorId tutorId, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<Session> sessions = _sessions.Values.Where(s => s.TutorId == tutorId).ToList();
        return Task.FromResult(sessions);
    }

    public Task<IReadOnlyCollection<Session>> GetByStudentIdAsync(StudentId studentId, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<Session> sessions = _sessions.Values.Where(s => s.StudentId == studentId).ToList();
        return Task.FromResult(sessions);
    }

    public Task<Session?> GetByAvailabilitySlotIdAsync(
        AvailabilitySlotId availabilitySlotId, CancellationToken cancellationToken = default)
    {
        var session = _sessions.Values.FirstOrDefault(s => s.AvailabilitySlotId == availabilitySlotId);
        return Task.FromResult(session);
    }

    public Task AddAsync(Session session, CancellationToken cancellationToken = default)
    {
        _sessions[session.Id.Value] = session;
        return Task.CompletedTask;
    }
}
