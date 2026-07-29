namespace TutorFlow.Domain.Meetings.ValueObjects;

// A Meeting has no independent "Completed" state (docs/adr/ADR-023-...) —
// the Session it belongs to already owns lesson completion (SCH-6).
public enum MeetingStatus
{
    Scheduled = 0,
    Cancelled = 1,
}
