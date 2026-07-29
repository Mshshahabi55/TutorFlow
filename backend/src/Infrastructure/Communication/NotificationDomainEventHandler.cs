using TutorFlow.Application.Authorization;
using TutorFlow.Application.Common;
using TutorFlow.Application.Scheduling.Interfaces;
using TutorFlow.Domain.Common;
using TutorFlow.Domain.Communication;
using TutorFlow.Domain.Communication.Events;
using TutorFlow.Domain.Communication.ValueObjects;
using TutorFlow.Domain.Identity.Events;
using TutorFlow.Domain.Identity.ValueObjects;
using TutorFlow.Domain.Meetings.Events;
using TutorFlow.Domain.Scheduling.Events;
using TutorFlow.Infrastructure.Persistence;

namespace TutorFlow.Infrastructure.Communication;

// docs/adr/ADR-022-communication-and-notifications-architecture.md's
// reactive Notification creation — the same same-transaction mechanism
// ADR-016 already established for the audit trail (AuditDomainEventHandler
// is the precedent this mirrors): staging new Notification rows into the
// same Scoped TutorFlowDbContext the triggering mutation is about to
// commit, via a second IDomainEventHandler registered alongside the audit
// one (DomainEventDispatcher already fans out to every registered handler).
//
// Reads only fields already present on the event, except for
// MeetingCreated/MeetingUpdated (docs/adr/ADR-023-...): a Meeting only
// knows its own SessionId, never the Session's Student/Parent-Guardian, so
// this handler resolves them via Scheduling & Booking's own
// ISessionRepository — the same cross-context Application-layer read
// RC5.1's StartConversationCommandHandler already established, never a
// direct query against Scheduling's own tables (ADR-002's Integration
// Rules). Where an event doesn't carry enough information to name a
// recipient at all (AvailabilityDeclared has no natural single recipient),
// no Notification is created — see the ADR's Non-Goals, not a bug.
internal sealed class NotificationDomainEventHandler : IDomainEventHandler
{
    private readonly TutorFlowDbContext _dbContext;
    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly ISessionRepository _sessionRepository;

    public NotificationDomainEventHandler(
        TutorFlowDbContext dbContext, ICurrentUserProvider currentUserProvider, ISessionRepository sessionRepository)
    {
        _dbContext = dbContext;
        _currentUserProvider = currentUserProvider;
        _sessionRepository = sessionRepository;
    }

    public async Task HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        foreach (var notification in await BuildNotificationsAsync(domainEvent, cancellationToken))
        {
            _dbContext.Add(notification);
        }
    }

    private async Task<List<Notification>> BuildNotificationsAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var notifications = new List<Notification>();

        if (domainEvent is not DomainEvent baseEvent)
        {
            return notifications;
        }

        var now = baseEvent.OccurredOnUtc;

        switch (domainEvent)
        {
            case SessionBooked e:
                notifications.Add(Notification.Create(
                    AccountId.From(e.StudentId.Value),
                    NotificationType.BookingConfirmed,
                    "Your lesson has been booked.",
                    e.SessionId.Value,
                    now));

                if (e.ParentGuardianId is not null)
                {
                    notifications.Add(Notification.Create(
                        AccountId.From(e.ParentGuardianId.Value),
                        NotificationType.BookingConfirmed,
                        "A lesson has been booked for your child.",
                        e.SessionId.Value,
                        now));
                }

                break;

            case SessionCancelled e:
                // StudentId/ParentGuardianId are only present when the
                // enriched shape was used (Session.Cancel() always supplies
                // them) — the defaulted-null case only applies to a caller
                // that never went through the aggregate (not a real path).
                if (e.StudentId is not null)
                {
                    notifications.Add(Notification.Create(
                        AccountId.From(e.StudentId.Value),
                        NotificationType.LessonCancelled,
                        "Your lesson has been cancelled.",
                        e.SessionId.Value,
                        now));
                }

                if (e.ParentGuardianId is not null)
                {
                    notifications.Add(Notification.Create(
                        AccountId.From(e.ParentGuardianId.Value),
                        NotificationType.LessonCancelled,
                        "Your child's lesson has been cancelled.",
                        e.SessionId.Value,
                        now));
                }

                break;

            case RelationshipConfirmed e:
                if (e.InvitedByAccountId is not null)
                {
                    notifications.Add(Notification.Create(
                        e.InvitedByAccountId,
                        NotificationType.ParentConfirmed,
                        "A Relationship you invited has been confirmed.",
                        e.RelationshipId.Value,
                        now));
                }

                break;

            case MessageSent e:
                // The sender is the current request's own caller — the same
                // request scope SendMessageCommandHandler ran in — so
                // ICurrentUserProvider.Role reliably reflects who just sent
                // this Message, not an unrelated caller.
                var senderIsTutor = _currentUserProvider.Role.ToRole() == Role.Tutor;
                notifications.Add(Notification.Create(
                    e.RecipientId,
                    senderIsTutor ? NotificationType.TutorReplied : NotificationType.NewMessage,
                    senderIsTutor ? "Your tutor replied to your message." : "You have a new message.",
                    e.ConversationId.Value,
                    now));

                break;

            case MeetingCreated e:
                await AddMeetingRecipientsAsync(
                    e.SessionId.Value, NotificationType.MeetingCreated,
                    "Your online lesson meeting is ready to join.",
                    "Your child's online lesson meeting is ready to join.",
                    e.SessionId.Value, now, notifications, cancellationToken);
                break;

            case MeetingUpdated e:
                await AddMeetingRecipientsAsync(
                    e.SessionId.Value, NotificationType.MeetingUpdated,
                    "Your online lesson meeting time has changed.",
                    "Your child's online lesson meeting time has changed.",
                    e.SessionId.Value, now, notifications, cancellationToken);
                break;
        }

        return notifications;
    }

    private async Task AddMeetingRecipientsAsync(
        Guid sessionId,
        NotificationType type,
        string studentSummary,
        string parentSummary,
        Guid relatedEntityId,
        DateTime now,
        List<Notification> notifications,
        CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdAsync(
            TutorFlow.Domain.Scheduling.ValueObjects.SessionId.From(sessionId), cancellationToken);
        if (session is null)
        {
            return;
        }

        notifications.Add(Notification.Create(AccountId.From(session.StudentId.Value), type, studentSummary, relatedEntityId, now));

        if (session.ParentGuardianId is not null)
        {
            notifications.Add(Notification.Create(
                AccountId.From(session.ParentGuardianId.Value), type, parentSummary, relatedEntityId, now));
        }
    }
}
