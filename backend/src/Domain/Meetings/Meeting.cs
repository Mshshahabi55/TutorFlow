using TutorFlow.Domain.Common;
using TutorFlow.Domain.Meetings.Events;
using TutorFlow.Domain.Meetings.ValueObjects;
using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Domain.Meetings;

// docs/adr/ADR-023-online-lesson-meeting-provider-architecture.md: one
// Meeting per Session, created on demand (a Tutor's own "Start Lesson"
// action, never automatically at booking time). SessionId is referenced by
// identity only, no navigation property, no FK (docs/adr/ADR-002's
// Integration Rules) — Meetings is its own bounded context, downstream of
// Scheduling & Booking.
public sealed class Meeting : AggregateRoot<MeetingId>
{
    // createdAtUtc/updatedAtUtc are two separate parameters (both equal to
    // "now" at construction time), not one nowUtc — EF Core's constructor-
    // binding convention matches each parameter to a mapped property by
    // name, and CreatedAtUtc/UpdatedAtUtc are two distinct properties; a
    // single nowUtc parameter cannot bind to both (mirrors Notification's
    // own constructor, whose createdAtUtc parameter name matches its own
    // one CreatedAtUtc property for the same reason).
    private Meeting(
        MeetingId id,
        SessionId sessionId,
        MeetingProviderOption provider,
        string providerMeetingId,
        string joinUrl,
        string? hostUrl,
        DateTime startsAtUtc,
        DateTime endsAtUtc,
        MeetingStatus status,
        DateTime createdAtUtc,
        DateTime updatedAtUtc) : base(id)
    {
        SessionId = sessionId;
        Provider = provider;
        ProviderMeetingId = providerMeetingId;
        JoinUrl = joinUrl;
        HostUrl = hostUrl;
        StartsAtUtc = startsAtUtc;
        EndsAtUtc = endsAtUtc;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public SessionId SessionId { get; }

    public MeetingProviderOption Provider { get; }

    // The id the provider itself assigned — never TutorFlow's own MeetingId,
    // used only when calling back into that provider's own API (Update/Cancel/Get).
    public string ProviderMeetingId { get; private set; }

    public string JoinUrl { get; private set; }

    // Nullable — not every provider distinguishes a separate host link.
    public string? HostUrl { get; private set; }

    public DateTime StartsAtUtc { get; private set; }

    public DateTime EndsAtUtc { get; private set; }

    public MeetingStatus Status { get; private set; }

    public DateTime CreatedAtUtc { get; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static Meeting Create(
        SessionId sessionId,
        MeetingProviderOption provider,
        string providerMeetingId,
        string joinUrl,
        string? hostUrl,
        DateTime startsAtUtc,
        DateTime endsAtUtc,
        DateTime nowUtc)
    {
        Guard.Against.Null(sessionId, nameof(sessionId));
        var trimmedProviderMeetingId = Guard.Against.NullOrWhiteSpace(providerMeetingId, nameof(providerMeetingId)).Trim();
        var trimmedJoinUrl = Guard.Against.NullOrWhiteSpace(joinUrl, nameof(joinUrl)).Trim();

        if (endsAtUtc <= startsAtUtc)
        {
            throw new ArgumentException("A Meeting's end time must be after its start time.", nameof(endsAtUtc));
        }

        var meeting = new Meeting(
            MeetingId.New(),
            sessionId,
            provider,
            trimmedProviderMeetingId,
            trimmedJoinUrl,
            string.IsNullOrWhiteSpace(hostUrl) ? null : hostUrl.Trim(),
            startsAtUtc,
            endsAtUtc,
            MeetingStatus.Scheduled,
            nowUtc,
            nowUtc);

        meeting.RaiseDomainEvent(new MeetingCreated(meeting.Id, sessionId, provider));

        return meeting;
    }

    // Only ever invoked reactively, when the Session it belongs to is
    // rescheduled (docs/adr/ADR-023-...'s "Reactive sync with Session") —
    // never a direct, standalone user action.
    public void Reschedule(DateTime newStartsAtUtc, DateTime newEndsAtUtc, DateTime nowUtc)
    {
        if (Status == MeetingStatus.Cancelled)
        {
            throw new InvalidOperationException("A Cancelled Meeting cannot be rescheduled.");
        }

        if (newEndsAtUtc <= newStartsAtUtc)
        {
            throw new ArgumentException("A Meeting's end time must be after its start time.", nameof(newEndsAtUtc));
        }

        StartsAtUtc = newStartsAtUtc;
        EndsAtUtc = newEndsAtUtc;
        UpdatedAtUtc = nowUtc;

        RaiseDomainEvent(new MeetingUpdated(Id, SessionId));
    }

    // Idempotent — only ever invoked reactively, when the Session it
    // belongs to is cancelled. A no-op (not an exception) if already
    // Cancelled, since MeetingSyncDomainEventHandler must never let this
    // call fail the Session cancellation it is reacting to.
    public void Cancel(DateTime nowUtc)
    {
        if (Status == MeetingStatus.Cancelled)
        {
            return;
        }

        Status = MeetingStatus.Cancelled;
        UpdatedAtUtc = nowUtc;

        RaiseDomainEvent(new MeetingCancelled(Id, SessionId));
    }

    // Refreshes provider-reported fields after a real IMeetingProvider
    // Update/Get call — Application-layer orchestration only, never
    // Domain-initiated; raises no event of its own (MeetingUpdated is
    // raised by Reschedule(), which already carries the same information
    // this is used alongside).
    public void ApplyProviderResult(string providerMeetingId, string joinUrl, string? hostUrl, DateTime nowUtc)
    {
        ProviderMeetingId = Guard.Against.NullOrWhiteSpace(providerMeetingId, nameof(providerMeetingId)).Trim();
        JoinUrl = Guard.Against.NullOrWhiteSpace(joinUrl, nameof(joinUrl)).Trim();
        HostUrl = string.IsNullOrWhiteSpace(hostUrl) ? null : hostUrl.Trim();
        UpdatedAtUtc = nowUtc;
    }
}
