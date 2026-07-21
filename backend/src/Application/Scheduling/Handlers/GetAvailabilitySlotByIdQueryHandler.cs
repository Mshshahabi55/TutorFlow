using TutorFlow.Application.Authorization;
using TutorFlow.Application.Common;
using TutorFlow.Application.Scheduling.DTOs;
using TutorFlow.Application.Scheduling.Interfaces;
using TutorFlow.Application.Scheduling.Queries;
using TutorFlow.Application.Scheduling.Validators;
using TutorFlow.Domain.Identity.ValueObjects;
using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Application.Scheduling.Handlers;

public sealed class GetAvailabilitySlotByIdQueryHandler
{
    private readonly IAvailabilitySlotRepository _availabilitySlotRepository;
    private readonly ISessionRepository _sessionRepository;
    private readonly ICurrentUserProvider _currentUserProvider;

    public GetAvailabilitySlotByIdQueryHandler(
        IAvailabilitySlotRepository availabilitySlotRepository,
        ISessionRepository sessionRepository,
        ICurrentUserProvider currentUserProvider)
    {
        _availabilitySlotRepository = availabilitySlotRepository;
        _sessionRepository = sessionRepository;
        _currentUserProvider = currentUserProvider;
    }

    public async Task<Result<AvailabilitySlotDto>> Handle(
        GetAvailabilitySlotByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var validation = GetAvailabilitySlotByIdQueryValidator.Validate(query);
        if (validation.IsFailure)
        {
            return Result.Failure<AvailabilitySlotDto>(validation.Error);
        }

        var slot = await _availabilitySlotRepository.GetByIdAsync(
            AvailabilitySlotId.From(query.AvailabilitySlotId),
            cancellationToken);

        if (slot is null)
        {
            return Result.Failure<AvailabilitySlotDto>(new Error(
                "GetAvailabilitySlotByIdQuery.NotFound",
                "Availability slot was not found.",
                ErrorType.Domain));
        }

        // ADR-003 Addendum, Decision 4: visible to the declaring Tutor
        // (owner), the Student with a booking against it, the Parent/Guardian
        // managing that booking, and Admin — no other authenticated user
        // (AUTHORIZATION_MATRIX.md §4.3). The booking Student/Parent-Guardian
        // only exist once the slot is consumed; an unconsumed slot has no
        // Session to check against.
        if (_currentUserProvider.Role.ToRole() != Role.AdminStaff)
        {
            if (_currentUserProvider.VerifyAuthenticated("GetAvailabilitySlotByIdQuery.Unauthenticated") is { } authError)
            {
                return Result.Failure<AvailabilitySlotDto>(authError);
            }

            var partyIds = new List<Guid?> { slot.TutorId.Value };
            if (slot.IsConsumed)
            {
                var session = await _sessionRepository.GetByAvailabilitySlotIdAsync(slot.Id, cancellationToken);
                if (session is not null)
                {
                    partyIds.Add(session.StudentId.Value);
                    partyIds.Add(session.ParentGuardianId?.Value);
                }
            }

            if (_currentUserProvider.VerifyIsParty("GetAvailabilitySlotByIdQuery.Forbidden", partyIds.ToArray())
                is { } ownershipError)
            {
                return Result.Failure<AvailabilitySlotDto>(ownershipError);
            }
        }

        return Result.Success(AvailabilitySlotDto.FromDomain(slot));
    }
}
