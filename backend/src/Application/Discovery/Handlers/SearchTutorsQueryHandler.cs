using TutorFlow.Application.Common;
using TutorFlow.Application.Discovery.Queries;
using TutorFlow.Application.Discovery.Validators;
using TutorFlow.Application.Identity.DTOs;
using TutorFlow.Application.Identity.Interfaces;
using TutorFlow.Application.Scheduling.Interfaces;

namespace TutorFlow.Application.Discovery.Handlers;

// Discovery's first real capability (DISC-1) — composes Identity &
// Relationship's Tutor data with Scheduling & Booking's AvailabilitySlot
// data entirely at this Application layer, per ARCHITECTURE.md Section 4/9;
// neither owning context's own aggregate or repository is bypassed
// (docs/adr/ADR-002-domain-boundaries.md: Integration Rules). Pagination is
// applied last, over the final, intersected result — Discovery has no single
// queryable source to page against, since it composes two.
public sealed class SearchTutorsQueryHandler
{
    private readonly ITutorRepository _tutorRepository;
    private readonly IAvailabilitySlotRepository _availabilitySlotRepository;

    public SearchTutorsQueryHandler(
        ITutorRepository tutorRepository,
        IAvailabilitySlotRepository availabilitySlotRepository)
    {
        _tutorRepository = tutorRepository;
        _availabilitySlotRepository = availabilitySlotRepository;
    }

    public async Task<Result<PagedResult<TutorDto>>> Handle(
        SearchTutorsQuery query,
        CancellationToken cancellationToken = default)
    {
        var validation = SearchTutorsQueryValidator.Validate(query);
        if (validation.IsFailure)
        {
            return Result.Failure<PagedResult<TutorDto>>(validation.Error);
        }

        var tutors = await _tutorRepository.SearchDiscoverableAsync(
            query.Subject, query.Language, query.Location, cancellationToken);

        if (query.AvailableFrom.HasValue)
        {
            var availableTutorIds = await _availabilitySlotRepository.GetTutorIdsWithOpenSlotFromAsync(
                query.AvailableFrom.Value, cancellationToken);

            // Bridged by the underlying Guid only: Tutor.Id is an AccountId
            // (Identity & Relationship), while AvailabilitySlot.TutorId is a
            // distinct TutorId type (Scheduling & Booking) — the two contexts
            // reference the same real-world Tutor by identity only, never by
            // sharing a Value Object type across the boundary (ADR-002).
            var availableGuids = availableTutorIds.Select(id => id.Value).ToHashSet();
            tutors = tutors.Where(t => availableGuids.Contains(t.Id.Value)).ToList();
        }

        var totalCount = tutors.Count;
        var page = tutors
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(TutorDto.FromDomain)
            .ToList();

        return Result.Success(new PagedResult<TutorDto>(page, totalCount, query.Page, query.PageSize));
    }
}
