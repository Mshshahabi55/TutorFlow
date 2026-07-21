using TutorFlow.Application.Common;
using TutorFlow.Application.Oversight.Queries;
using TutorFlow.Application.Oversight.Validators;
using TutorFlow.Application.Scheduling.DTOs;
using TutorFlow.Application.Scheduling.Interfaces;

namespace TutorFlow.Application.Oversight.Handlers;

public sealed class GetAllSessionsQueryHandler
{
    private readonly ISessionRepository _sessionRepository;

    public GetAllSessionsQueryHandler(ISessionRepository sessionRepository)
    {
        _sessionRepository = sessionRepository;
    }

    public async Task<Result<PagedResult<SessionDto>>> Handle(
        GetAllSessionsQuery query,
        CancellationToken cancellationToken = default)
    {
        var validation = GetAllSessionsQueryValidator.Validate(query);
        if (validation.IsFailure)
        {
            return Result.Failure<PagedResult<SessionDto>>(validation.Error);
        }

        var pageRequest = new PageRequest(query.Page, query.PageSize);
        var (sessions, totalCount) = await _sessionRepository.GetAllAsync(pageRequest, cancellationToken);

        var dtos = sessions.Select(SessionDto.FromDomain).ToList();

        return Result.Success(new PagedResult<SessionDto>(dtos, totalCount, query.Page, query.PageSize));
    }
}
