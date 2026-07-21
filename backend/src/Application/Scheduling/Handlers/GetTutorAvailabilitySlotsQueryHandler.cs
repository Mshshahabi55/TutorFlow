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

// Verifies the Tutor exists before returning its availability, so an invalid
// id is distinguishable from a valid Tutor with no declared slots yet —
// mirrors GetTutorScheduleQueryHandler exactly (Backend Completion Phase,
// Track A, Phase A2).
public sealed class GetTutorAvailabilitySlotsQueryHandler
{
    private readonly ITutorRepository _tutorRepository;
    private readonly IAvailabilitySlotRepository _availabilitySlotRepository;
    private readonly ICurrentUserProvider _currentUserProvider;

    public GetTutorAvailabilitySlotsQueryHandler(
        ITutorRepository tutorRepository,
        IAvailabilitySlotRepository availabilitySlotRepository,
        ICurrentUserProvider currentUserProvider)
    {
        _tutorRepository = tutorRepository;
        _availabilitySlotRepository = availabilitySlotRepository;
        _currentUserProvider = currentUserProvider;
    }

    public async Task<Result<IReadOnlyCollection<AvailabilitySlotDto>>> Handle(
        GetTutorAvailabilitySlotsQuery query,
        CancellationToken cancellationToken = default)
    {
        var validation = GetTutorAvailabilitySlotsQueryValidator.Validate(query);
        if (validation.IsFailure)
        {
            return Result.Failure<IReadOnlyCollection<AvailabilitySlotDto>>(validation.Error);
        }

        var tutor = await _tutorRepository.GetByIdAsync(AccountId.From(query.TutorId), cancellationToken);
        if (tutor is null)
        {
            return Result.Failure<IReadOnlyCollection<AvailabilitySlotDto>>(new Error(
                "GetTutorAvailabilitySlotsQuery.NotFound",
                "Tutor was not found.",
                ErrorType.Domain));
        }

        // ADR-003 Third Addendum, Decision 13: any authenticated caller may
        // view a discoverable Tutor's slots; a non-discoverable Tutor's
        // slots are visible only to that Tutor or Admin/Staff. Never
        // visible to an unauthenticated caller either way.
        if (tutor.IsDiscoverable)
        {
            if (_currentUserProvider.VerifyAuthenticated("GetTutorAvailabilitySlotsQuery.Unauthenticated") is { } authError)
            {
                return Result.Failure<IReadOnlyCollection<AvailabilitySlotDto>>(authError);
            }
        }
        else if (_currentUserProvider.Role.ToRole() != Role.AdminStaff)
        {
            if (_currentUserProvider.VerifyAuthenticated("GetTutorAvailabilitySlotsQuery.Unauthenticated") is { } authError)
            {
                return Result.Failure<IReadOnlyCollection<AvailabilitySlotDto>>(authError);
            }

            if (_currentUserProvider.VerifyOwnTutorId(query.TutorId, "GetTutorAvailabilitySlotsQuery.Forbidden") is { } ownershipError)
            {
                return Result.Failure<IReadOnlyCollection<AvailabilitySlotDto>>(ownershipError);
            }
        }

        var slots = await _availabilitySlotRepository.GetByTutorIdAsync(TutorId.From(query.TutorId), cancellationToken);

        IReadOnlyCollection<AvailabilitySlotDto> dtos = slots.Select(AvailabilitySlotDto.FromDomain).ToList();

        return Result.Success(dtos);
    }
}
