using Microsoft.EntityFrameworkCore;
using TutorFlow.Application.Meetings.Interfaces;
using TutorFlow.Domain.Meetings;
using TutorFlow.Domain.Meetings.ValueObjects;
using TutorFlow.Domain.Scheduling.ValueObjects;
using TutorFlow.Infrastructure.Persistence;

namespace TutorFlow.Infrastructure.Meetings.Repositories;

internal sealed class MeetingRepository : IMeetingRepository
{
    private readonly TutorFlowDbContext _dbContext;

    public MeetingRepository(TutorFlowDbContext dbContext) => _dbContext = dbContext;

    public Task<Meeting?> GetByIdAsync(MeetingId id, CancellationToken cancellationToken = default) =>
        _dbContext.Meetings.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    // Tracked, not AsNoTracking — CreateMeetingCommandHandler's idempotent
    // path and MeetingSyncDomainEventHandler both mutate the result they
    // read here (CLAUDE.md: ".AsNoTracking() is permitted only on read-only
    // query paths").
    public Task<Meeting?> GetBySessionIdAsync(SessionId sessionId, CancellationToken cancellationToken = default) =>
        _dbContext.Meetings.FirstOrDefaultAsync(m => m.SessionId == sessionId, cancellationToken);

    public Task AddAsync(Meeting meeting, CancellationToken cancellationToken = default)
    {
        _dbContext.Meetings.Add(meeting);
        return Task.CompletedTask;
    }
}
