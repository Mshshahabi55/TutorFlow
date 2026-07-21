using TutorFlow.Application.Authorization;
using TutorFlow.Application.Common;
using TutorFlow.Application.Identity.DTOs;
using TutorFlow.Application.Identity.Interfaces;
using TutorFlow.Application.Identity.Queries;
using TutorFlow.Application.Identity.Validators;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Identity.Handlers;

// Not filtered by discoverability, unlike GetTutorListQueryHandler — see
// ITutorRepository.GetDiscoverableAsync's own comment and the Read Side
// design specification: this is the only existing query capable of serving
// ADM-1's "review a pending Tutor registration" need.
public sealed class GetTutorByIdQueryHandler
{
    private readonly ITutorRepository _tutorRepository;
    private readonly ICurrentUserProvider _currentUserProvider;

    public GetTutorByIdQueryHandler(ITutorRepository tutorRepository, ICurrentUserProvider currentUserProvider)
    {
        _tutorRepository = tutorRepository;
        _currentUserProvider = currentUserProvider;
    }

    public async Task<Result<TutorDto>> Handle(GetTutorByIdQuery query, CancellationToken cancellationToken = default)
    {
        var validation = GetTutorByIdQueryValidator.Validate(query);
        if (validation.IsFailure)
        {
            return Result.Failure<TutorDto>(validation.Error);
        }

        var tutor = await _tutorRepository.GetByIdAsync(AccountId.From(query.TutorId), cancellationToken);
        if (tutor is null)
        {
            return Result.Failure<TutorDto>(new Error(
                "GetTutorByIdQuery.NotFound",
                "Tutor was not found.",
                ErrorType.Domain));
        }

        // ADR-003 Second Addendum, Decision 2: an approved (discoverable)
        // Tutor's detail is Public — no authentication or ownership check
        // applies at all. A pending or suspended Tutor's detail is Owner
        // (that Tutor) + Admin/Staff only (AUTHORIZATION_MATRIX.md §4.1).
        if (!tutor.IsDiscoverable && _currentUserProvider.Role.ToRole() != Role.AdminStaff)
        {
            if (_currentUserProvider.VerifyAuthenticated("GetTutorByIdQuery.Unauthenticated") is { } authError)
            {
                return Result.Failure<TutorDto>(authError);
            }

            if (_currentUserProvider.VerifyOwnTutorId(query.TutorId, "GetTutorByIdQuery.Forbidden") is { } ownershipError)
            {
                return Result.Failure<TutorDto>(ownershipError);
            }
        }

        return Result.Success(TutorDto.FromDomain(tutor));
    }
}
