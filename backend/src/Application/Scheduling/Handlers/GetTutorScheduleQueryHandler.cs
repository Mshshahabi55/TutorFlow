using TutorFlow.Application.Authorization;
using TutorFlow.Application.Common;
using TutorFlow.Application.Identity.Interfaces;
using TutorFlow.Application.Scheduling.DTOs;
using TutorFlow.Application.Scheduling.Interfaces;
using TutorFlow.Application.Scheduling.Queries;
using TutorFlow.Application.Scheduling.Validators;
using TutorFlow.Domain.Identity.ValueObjects;
using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Application.Scheduling.Handlers;

// Verifies the Tutor exists before returning its schedule, so an invalid id
// is distinguishable from a valid Tutor with no Sessions yet. Every Session
// status is included (PRODUCT_REQUIREMENTS.md 5.3 step 5).
public sealed class GetTutorScheduleQueryHandler
{
    private readonly ITutorRepository _tutorRepository;
    private readonly ISessionRepository _sessionRepository;
    private readonly ICurrentUserProvider _currentUserProvider;

    public GetTutorScheduleQueryHandler(
        ITutorRepository tutorRepository,
        ISessionRepository sessionRepository,
        ICurrentUserProvider currentUserProvider)
    {
        _tutorRepository = tutorRepository;
        _sessionRepository = sessionRepository;
        _currentUserProvider = currentUserProvider;
    }

    public async Task<Result<IReadOnlyCollection<SessionDto>>> Handle(
        GetTutorScheduleQuery query,
        CancellationToken cancellationToken = default)
    {
        var validation = GetTutorScheduleQueryValidator.Validate(query);
        if (validation.IsFailure)
        {
            return Result.Failure<IReadOnlyCollection<SessionDto>>(validation.Error);
        }

        var tutor = await _tutorRepository.GetByIdAsync(AccountId.From(query.TutorId), cancellationToken);
        if (tutor is null)
        {
            return Result.Failure<IReadOnlyCollection<SessionDto>>(new Error(
                "GetTutorScheduleQuery.NotFound",
                "Tutor was not found.",
                ErrorType.Domain));
        }

        // ADR-003 Third Addendum, Decision 12: visible to the named Tutor
        // only, or Admin/Staff — no other authenticated user (CONST-5).
        if (_currentUserProvider.Role.ToRole() != Role.AdminStaff)
        {
            if (_currentUserProvider.VerifyAuthenticated("GetTutorScheduleQuery.Unauthenticated") is { } authError)
            {
                return Result.Failure<IReadOnlyCollection<SessionDto>>(authError);
            }

            if (_currentUserProvider.VerifyOwnTutorId(query.TutorId, "GetTutorScheduleQuery.Forbidden") is { } ownershipError)
            {
                return Result.Failure<IReadOnlyCollection<SessionDto>>(ownershipError);
            }
        }

        var sessions = await _sessionRepository.GetByTutorIdAsync(TutorId.From(query.TutorId), cancellationToken);

        IReadOnlyCollection<SessionDto> dtos = sessions.Select(SessionDto.FromDomain).ToList();

        return Result.Success(dtos);
    }
}
