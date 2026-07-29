using TutorFlow.Application.Authorization;
using TutorFlow.Application.Common;
using TutorFlow.Application.Meetings.DTOs;
using TutorFlow.Application.Meetings.Interfaces;
using TutorFlow.Application.Meetings.Queries;
using TutorFlow.Application.Meetings.Validators;
using TutorFlow.Application.Scheduling.Interfaces;
using TutorFlow.Domain.Identity.ValueObjects;
using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Application.Meetings.Handlers;

public sealed class GetMeetingBySessionQueryHandler
{
    private readonly ISessionRepository _sessionRepository;
    private readonly IMeetingRepository _meetingRepository;
    private readonly ICurrentUserProvider _currentUserProvider;

    public GetMeetingBySessionQueryHandler(
        ISessionRepository sessionRepository,
        IMeetingRepository meetingRepository,
        ICurrentUserProvider currentUserProvider)
    {
        _sessionRepository = sessionRepository;
        _meetingRepository = meetingRepository;
        _currentUserProvider = currentUserProvider;
    }

    public async Task<Result<MeetingDto>> Handle(GetMeetingBySessionQuery query, CancellationToken cancellationToken = default)
    {
        var validation = GetMeetingBySessionQueryValidator.Validate(query);
        if (validation.IsFailure)
        {
            return Result.Failure<MeetingDto>(validation.Error);
        }

        var sessionId = SessionId.From(query.SessionId);
        var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return Result.Failure<MeetingDto>(new Error(
                "GetMeetingBySessionQuery.SessionNotFound", "Session was not found.", ErrorType.Domain));
        }

        // Same parties as GetSessionByIdQuery itself (AUTHORIZATION_MATRIX.md
        // §4.3) — Admin's own visibility here is read-only by construction,
        // since no Admin-facing mutation exists in this context at all.
        if (_currentUserProvider.Role.ToRole() != Role.AdminStaff)
        {
            if (_currentUserProvider.VerifyAuthenticated("GetMeetingBySessionQuery.Unauthenticated") is { } authError)
            {
                return Result.Failure<MeetingDto>(authError);
            }

            if (_currentUserProvider.VerifyIsParty(
                    "GetMeetingBySessionQuery.Forbidden", session.TutorId.Value, session.StudentId.Value, session.ParentGuardianId?.Value)
                is { } ownershipError)
            {
                return Result.Failure<MeetingDto>(ownershipError);
            }
        }

        var meeting = await _meetingRepository.GetBySessionIdAsync(sessionId, cancellationToken);
        if (meeting is null)
        {
            return Result.Failure<MeetingDto>(new Error(
                "GetMeetingBySessionQuery.NotFound", "No meeting has been started for this session yet.", ErrorType.Domain));
        }

        return Result.Success(MeetingDto.FromDomain(meeting));
    }
}
