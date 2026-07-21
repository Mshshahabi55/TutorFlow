using TutorFlow.Application.Authorization;
using TutorFlow.Application.Common;
using TutorFlow.Application.Scheduling.DTOs;
using TutorFlow.Application.Scheduling.Interfaces;
using TutorFlow.Application.Scheduling.Queries;
using TutorFlow.Application.Scheduling.Validators;
using TutorFlow.Domain.Identity.ValueObjects;
using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Application.Scheduling.Handlers;

public sealed class GetSessionByIdQueryHandler
{
    private readonly ISessionRepository _sessionRepository;
    private readonly ICurrentUserProvider _currentUserProvider;

    public GetSessionByIdQueryHandler(ISessionRepository sessionRepository, ICurrentUserProvider currentUserProvider)
    {
        _sessionRepository = sessionRepository;
        _currentUserProvider = currentUserProvider;
    }

    public async Task<Result<SessionDto>> Handle(GetSessionByIdQuery query, CancellationToken cancellationToken = default)
    {
        var validation = GetSessionByIdQueryValidator.Validate(query);
        if (validation.IsFailure)
        {
            return Result.Failure<SessionDto>(validation.Error);
        }

        var session = await _sessionRepository.GetByIdAsync(SessionId.From(query.SessionId), cancellationToken);
        if (session is null)
        {
            return Result.Failure<SessionDto>(new Error(
                "GetSessionByIdQuery.NotFound",
                "Session was not found.",
                ErrorType.Domain));
        }

        // ADR-003 Second Addendum, Decision 7: same parties as Decision 4 —
        // the Tutor on this Session, the Student on it, the Parent/Guardian
        // who booked it, and Admin (AUTHORIZATION_MATRIX.md §4.3).
        if (_currentUserProvider.Role.ToRole() != Role.AdminStaff)
        {
            if (_currentUserProvider.VerifyAuthenticated("GetSessionByIdQuery.Unauthenticated") is { } authError)
            {
                return Result.Failure<SessionDto>(authError);
            }

            if (_currentUserProvider.VerifyIsParty(
                "GetSessionByIdQuery.Forbidden", session.TutorId.Value, session.StudentId.Value, session.ParentGuardianId?.Value)
                is { } ownershipError)
            {
                return Result.Failure<SessionDto>(ownershipError);
            }
        }

        return Result.Success(SessionDto.FromDomain(session));
    }
}
