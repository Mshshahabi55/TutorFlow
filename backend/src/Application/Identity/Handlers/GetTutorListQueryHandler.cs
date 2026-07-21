using TutorFlow.Application.Common;
using TutorFlow.Application.Identity.DTOs;
using TutorFlow.Application.Identity.Interfaces;
using TutorFlow.Application.Identity.Queries;

namespace TutorFlow.Application.Identity.Handlers;

// Discoverable Tutors only (IsApproved && !IsSuspended) — see
// ITutorRepository.GetDiscoverableAsync. This is narrower than DISC-1's
// filtered "Search Tutors" capability: no Subject/Availability/Language/
// Location filter is applied, since none of that criteria's semantics is
// established by any approved document (PRODUCT_REQUIREMENTS.md Section
// 10.3). No pagination, matching GetTutorListQuery's own comment.
public sealed class GetTutorListQueryHandler
{
    private readonly ITutorRepository _tutorRepository;

    public GetTutorListQueryHandler(ITutorRepository tutorRepository)
    {
        _tutorRepository = tutorRepository;
    }

    public async Task<Result<IReadOnlyCollection<TutorDto>>> Handle(
        GetTutorListQuery query,
        CancellationToken cancellationToken = default)
    {
        var tutors = await _tutorRepository.GetDiscoverableAsync(cancellationToken);

        IReadOnlyCollection<TutorDto> dtos = tutors.Select(TutorDto.FromDomain).ToList();

        return Result.Success(dtos);
    }
}
