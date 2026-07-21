using TutorFlow.Application.Audit.DTOs;
using TutorFlow.Application.Audit.Interfaces;
using TutorFlow.Application.Audit.Queries;
using TutorFlow.Application.Audit.Validators;
using TutorFlow.Application.Common;

namespace TutorFlow.Application.Audit.Handlers;

public sealed class GetAuditEntriesQueryHandler
{
    private readonly IAuditEntryRepository _auditEntryRepository;

    public GetAuditEntriesQueryHandler(IAuditEntryRepository auditEntryRepository)
    {
        _auditEntryRepository = auditEntryRepository;
    }

    public async Task<Result<PagedResult<AuditEntryDto>>> Handle(
        GetAuditEntriesQuery query,
        CancellationToken cancellationToken = default)
    {
        var validation = GetAuditEntriesQueryValidator.Validate(query);
        if (validation.IsFailure)
        {
            return Result.Failure<PagedResult<AuditEntryDto>>(validation.Error);
        }

        var pageRequest = new PageRequest(query.Page, query.PageSize);
        var (items, totalCount) = await _auditEntryRepository.GetAllAsync(
            pageRequest, query.SubjectId, cancellationToken);

        return Result.Success(new PagedResult<AuditEntryDto>(items, totalCount, query.Page, query.PageSize));
    }
}
