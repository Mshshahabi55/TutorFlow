using TutorFlow.Domain.Common;
using TutorFlow.Domain.Meetings.ValueObjects;
using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Domain.Meetings.Events;

public sealed record MeetingUpdated(MeetingId MeetingId, SessionId SessionId) : DomainEvent;
