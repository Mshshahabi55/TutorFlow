using TutorFlow.Application.Common;
using TutorFlow.Application.Communication.Interfaces;
using TutorFlow.Application.Meetings.DTOs;
using TutorFlow.Application.Meetings.Interfaces;
using TutorFlow.Application.Meetings.Queries;
using TutorFlow.Application.Meetings.Validators;
using TutorFlow.Application.Scheduling.Interfaces;
using TutorFlow.Domain.Communication.ValueObjects;
using TutorFlow.Domain.Identity.ValueObjects;
using TutorFlow.Domain.Scheduling;
using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Application.Meetings.Handlers;

// docs/adr/ADR-023-...'s "Conversation <-> Meeting linkage" — reuses
// Scheduling & Booking's own existing repository methods (GetByTutorIdAsync)
// and Communication's own IConversationRepository directly, the same
// cross-context Application-layer read RC5.1's StartConversationCommandHandler
// already established by injecting Identity's ITutorRepository. No new
// Scheduling-context code, no direct table access across contexts.
public sealed class GetActiveMeetingForConversationQueryHandler
{
    private readonly IConversationRepository _conversationRepository;
    private readonly ISessionRepository _sessionRepository;
    private readonly IMeetingRepository _meetingRepository;
    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetActiveMeetingForConversationQueryHandler(
        IConversationRepository conversationRepository,
        ISessionRepository sessionRepository,
        IMeetingRepository meetingRepository,
        ICurrentUserProvider currentUserProvider,
        IDateTimeProvider dateTimeProvider)
    {
        _conversationRepository = conversationRepository;
        _sessionRepository = sessionRepository;
        _meetingRepository = meetingRepository;
        _currentUserProvider = currentUserProvider;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<MeetingDto?>> Handle(
        GetActiveMeetingForConversationQuery query, CancellationToken cancellationToken = default)
    {
        var validation = GetActiveMeetingForConversationQueryValidator.Validate(query);
        if (validation.IsFailure)
        {
            return Result.Failure<MeetingDto?>(validation.Error);
        }

        var conversation = await _conversationRepository.GetByIdAsync(
            ConversationId.From(query.ConversationId), cancellationToken);
        if (conversation is null)
        {
            return Result.Failure<MeetingDto?>(new Error(
                "GetActiveMeetingForConversationQuery.NotFound", "Conversation was not found.", ErrorType.Domain));
        }

        if (_currentUserProvider.VerifyIsParty(
                "GetActiveMeetingForConversationQuery.Forbidden",
                conversation.ParticipantAId.Value,
                conversation.ParticipantBId.Value)
            is { } forbiddenError)
        {
            return Result.Failure<MeetingDto?>(forbiddenError);
        }

        var session = await FindRelevantOnlineSessionAsync(
            conversation.ParticipantAId.Value, conversation.ParticipantBId.Value, cancellationToken);
        if (session is null)
        {
            return Result.Success<MeetingDto?>(null);
        }

        var meeting = await _meetingRepository.GetBySessionIdAsync(session.Id, cancellationToken);
        return Result.Success(meeting is null ? null : MeetingDto.FromDomain(meeting));
    }

    // Tries each participant in turn as a Tutor — a conversation between two
    // non-Tutor roles (e.g. Admin/Staff support) legitimately has no such
    // Session and correctly resolves to "no active meeting."
    private async Task<Session?> FindRelevantOnlineSessionAsync(
        Guid participantAId, Guid participantBId, CancellationToken cancellationToken)
    {
        return await FindFromTutorPerspectiveAsync(participantAId, participantBId, cancellationToken)
            ?? await FindFromTutorPerspectiveAsync(participantBId, participantAId, cancellationToken);
    }

    private async Task<Session?> FindFromTutorPerspectiveAsync(
        Guid tutorAccountId, Guid otherAccountId, CancellationToken cancellationToken)
    {
        var sessions = await _sessionRepository.GetByTutorIdAsync(TutorId.From(tutorAccountId), cancellationToken);
        var now = _dateTimeProvider.UtcNow;

        var candidates = sessions
            .Where(session =>
                session.Status == SessionStatus.Scheduled
                && session.DeliveryMode == DeliveryMode.Online
                && (session.StudentId.Value == otherAccountId || session.ParentGuardianId?.Value == otherAccountId))
            .ToList();

        if (candidates.Count == 0)
        {
            return null;
        }

        var ongoing = candidates
            .Where(session => session.ScheduledTimeUtc <= now && now <= session.EndTimeUtc)
            .OrderBy(session => session.ScheduledTimeUtc)
            .FirstOrDefault();

        return ongoing ?? candidates
            .Where(session => session.ScheduledTimeUtc > now)
            .OrderBy(session => session.ScheduledTimeUtc)
            .FirstOrDefault();
    }
}
