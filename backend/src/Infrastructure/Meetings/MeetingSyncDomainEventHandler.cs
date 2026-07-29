using Microsoft.Extensions.Logging;
using TutorFlow.Application.Common;
using TutorFlow.Application.Meetings.Interfaces;
using TutorFlow.Domain.Common;
using TutorFlow.Domain.Scheduling.Events;

namespace TutorFlow.Infrastructure.Meetings;

// docs/adr/ADR-023-online-lesson-meeting-provider-architecture.md's
// "Reactive sync with Session" — a third IDomainEventHandler alongside
// AuditDomainEventHandler and NotificationDomainEventHandler
// (DomainEventDispatcher already fans out to every registered handler).
//
// Reacts to SessionRescheduled/SessionCancelled. If the Session has no
// Meeting (most Sessions never do — a Meeting only exists once a Tutor has
// clicked "Start Lesson"), this is a no-op. If one exists: the Meeting's
// own domain mutation (Reschedule/Cancel) and DB write always succeed —
// same-transaction durability, the same mechanism ADR-016 established for
// the audit trail. The matching third-party IMeetingProvider call is
// separately attempted, wrapped in its own try/catch that logs and
// swallows any exception — a deliberate, narrow departure from ADR-012
// Decision 3 ("failure propagates"), justified because a synchronous
// external HTTP call has a fundamentally different failure profile than a
// local DB write, and Product Goal 6 ("never lose, double-book, or
// silently drop a session") means a provider outage must never be able to
// block or fail a Session reschedule/cancellation that is otherwise fine.
internal sealed class MeetingSyncDomainEventHandler : IDomainEventHandler
{
    private readonly IMeetingRepository _meetingRepository;
    private readonly IMeetingProviderResolver _providerResolver;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<MeetingSyncDomainEventHandler> _logger;

    public MeetingSyncDomainEventHandler(
        IMeetingRepository meetingRepository,
        IMeetingProviderResolver providerResolver,
        IDateTimeProvider dateTimeProvider,
        ILogger<MeetingSyncDomainEventHandler> logger)
    {
        _meetingRepository = meetingRepository;
        _providerResolver = providerResolver;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        switch (domainEvent)
        {
            case SessionRescheduled e when e.NewEndTimeUtc is not null:
                await HandleRescheduledAsync(e, cancellationToken);
                break;
            case SessionCancelled e:
                await HandleCancelledAsync(e, cancellationToken);
                break;
        }
    }

    private async Task HandleRescheduledAsync(SessionRescheduled e, CancellationToken cancellationToken)
    {
        var meeting = await _meetingRepository.GetBySessionIdAsync(e.SessionId, cancellationToken);
        if (meeting is null)
        {
            return;
        }

        var now = _dateTimeProvider.UtcNow;
        meeting.Reschedule(e.NewScheduledTimeUtc, e.NewEndTimeUtc!.Value, now);

        try
        {
            var provider = _providerResolver.Resolve(meeting.Provider);
            var result = await provider.UpdateMeetingAsync(
                meeting.ProviderMeetingId,
                new UpdateProviderMeetingRequest(e.NewScheduledTimeUtc, e.NewEndTimeUtc.Value),
                cancellationToken);
            meeting.ApplyProviderResult(result.ProviderMeetingId, result.JoinUrl, result.HostUrl, now);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to update Meeting {MeetingId}'s provider-side time for rescheduled Session {SessionId} — " +
                "TutorFlow's own record is updated, but the third-party calendar event may be out of sync.",
                meeting.Id.Value,
                e.SessionId.Value);
        }
    }

    private async Task HandleCancelledAsync(SessionCancelled e, CancellationToken cancellationToken)
    {
        var meeting = await _meetingRepository.GetBySessionIdAsync(e.SessionId, cancellationToken);
        if (meeting is null)
        {
            return;
        }

        meeting.Cancel(_dateTimeProvider.UtcNow);

        try
        {
            await _providerResolver.Resolve(meeting.Provider).CancelMeetingAsync(meeting.ProviderMeetingId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to cancel Meeting {MeetingId}'s provider-side event for cancelled Session {SessionId} — " +
                "TutorFlow's own record is cancelled, but the third-party calendar event may still exist.",
                meeting.Id.Value,
                e.SessionId.Value);
        }
    }
}
