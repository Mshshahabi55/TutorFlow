using TutorFlow.Application.Meetings.Interfaces;
using TutorFlow.Domain.Meetings;
using TutorFlow.Domain.Meetings.ValueObjects;
using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Application.Tests.TestDoubles;

internal sealed class InMemoryMeetingRepository : IMeetingRepository
{
    private readonly Dictionary<Guid, Meeting> _meetings = new();

    public Task<Meeting?> GetByIdAsync(MeetingId id, CancellationToken cancellationToken = default)
    {
        _meetings.TryGetValue(id.Value, out var meeting);
        return Task.FromResult(meeting);
    }

    public Task<Meeting?> GetBySessionIdAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        var meeting = _meetings.Values.FirstOrDefault(m => m.SessionId == sessionId);
        return Task.FromResult(meeting);
    }

    public Task AddAsync(Meeting meeting, CancellationToken cancellationToken = default)
    {
        _meetings[meeting.Id.Value] = meeting;
        return Task.CompletedTask;
    }
}
