using TutorFlow.Application.Common;
using TutorFlow.Application.Oversight.Queries;
using TutorFlow.Application.Scheduling.DTOs;
using TutorFlow.Application.Scheduling.Interfaces;

namespace TutorFlow.Application.Oversight.Handlers;

// No validator: GetSessionStatusCountsQuery carries no parameters (same
// shape as GetMyConversationsQuery/GetMyNotificationsQuery) — nothing to
// validate. Coarse-grained authorization only (Permission.ViewAllSchedules,
// same as GetAllSessionsQueryHandler — this is the same ADM-3 audience,
// aggregated instead of listed) — platform-wide counts have no
// resource-instance owner to check against, so no fine-grained check applies.
public sealed class GetSessionStatusCountsQueryHandler
{
    private readonly ISessionRepository _sessionRepository;

    public GetSessionStatusCountsQueryHandler(ISessionRepository sessionRepository)
    {
        _sessionRepository = sessionRepository;
    }

    public async Task<Result<SessionStatusCountsDto>> Handle(
        GetSessionStatusCountsQuery query, CancellationToken cancellationToken = default)
    {
        var counts = await _sessionRepository.GetStatusCountsAsync(cancellationToken);
        return Result.Success(SessionStatusCountsDto.FromCounts(counts));
    }
}
