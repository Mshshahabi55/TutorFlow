using TutorFlow.Domain.Meetings;
using TutorFlow.Domain.Meetings.ValueObjects;
using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Application.Meetings.Interfaces;

public interface IMeetingRepository
{
    Task<Meeting?> GetByIdAsync(MeetingId id, CancellationToken cancellationToken = default);

    // Tracked, not AsNoTracking — every caller either mutates the result
    // (CreateMeetingCommandHandler's idempotent-return path,
    // MeetingSyncDomainEventHandler) or needs at most one row, so a
    // no-tracking projection would save nothing here.
    Task<Meeting?> GetBySessionIdAsync(SessionId sessionId, CancellationToken cancellationToken = default);

    Task AddAsync(Meeting meeting, CancellationToken cancellationToken = default);
}
