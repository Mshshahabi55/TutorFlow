using TutorFlow.Domain.Meetings;
using TutorFlow.Domain.Meetings.ValueObjects;

namespace TutorFlow.Application.Meetings.DTOs;

public sealed record MeetingDto(
    Guid MeetingId,
    Guid SessionId,
    MeetingProviderOption Provider,
    string JoinUrl,
    string? HostUrl,
    DateTime StartsAtUtc,
    DateTime EndsAtUtc,
    MeetingStatus Status,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc)
{
    // ProviderMeetingId is deliberately never included — it is an internal
    // reference used only for calling back into the provider's own API, not
    // information a Tutor/Student/Parent/Admin viewer needs.
    public static MeetingDto FromDomain(Meeting meeting) => new(
        meeting.Id.Value,
        meeting.SessionId.Value,
        meeting.Provider,
        meeting.JoinUrl,
        meeting.HostUrl,
        meeting.StartsAtUtc,
        meeting.EndsAtUtc,
        meeting.Status,
        meeting.CreatedAtUtc,
        meeting.UpdatedAtUtc);
}
