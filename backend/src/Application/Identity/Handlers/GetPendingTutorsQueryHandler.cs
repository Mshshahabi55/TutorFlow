using TutorFlow.Application.Common;
using TutorFlow.Application.Identity.DTOs;
using TutorFlow.Application.Identity.Interfaces;
using TutorFlow.Application.Identity.Queries;
using TutorFlow.Application.Identity.Validators;

namespace TutorFlow.Application.Identity.Handlers;

// Admin-only capability (ADM-1) — see GetPendingTutorsQuery. Least-privilege
// enforcement at the boundary is not yet possible since ADR-011
// (authentication) remains frozen; this is the same, already-disclosed
// limitation shared by every endpoint under that freeze, not a new risk.
public sealed class GetPendingTutorsQueryHandler
{
    private readonly ITutorRepository _tutorRepository;

    public GetPendingTutorsQueryHandler(ITutorRepository tutorRepository)
    {
        _tutorRepository = tutorRepository;
    }

    public async Task<Result<PagedResult<TutorDto>>> Handle(
        GetPendingTutorsQuery query,
        CancellationToken cancellationToken = default)
    {
        var validation = GetPendingTutorsQueryValidator.Validate(query);
        if (validation.IsFailure)
        {
            return Result.Failure<PagedResult<TutorDto>>(validation.Error);
        }

        var pageRequest = new PageRequest(query.Page, query.PageSize);
        var (tutors, totalCount) = await _tutorRepository.GetPendingAsync(pageRequest, cancellationToken);

        var dtos = tutors.Select(TutorDto.FromDomain).ToList();

        return Result.Success(new PagedResult<TutorDto>(dtos, totalCount, query.Page, query.PageSize));
    }
}
