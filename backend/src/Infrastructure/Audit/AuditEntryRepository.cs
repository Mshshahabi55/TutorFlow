using Microsoft.EntityFrameworkCore;
using TutorFlow.Application.Audit.DTOs;
using TutorFlow.Application.Audit.Interfaces;
using TutorFlow.Application.Common;
using TutorFlow.Infrastructure.Persistence;

namespace TutorFlow.Infrastructure.Audit;

// Reads back the same AuditEntry rows AuditDomainEventHandler writes
// (docs/adr/ADR-016-audit-durability-strategy.md) — no schema change, no
// AuditEntry change, no event change: this is a read-only addition around
// already-persisted data (Backend Completion Phase, Track A, Phase A1).
// Maps AuditEntry (Infrastructure-internal) to AuditEntryDto (Application)
// here, in the repository, since AuditEntry has no Domain representation
// for a query handler to project itself — see AuditEntryDto's own remarks.
// Ordered by OccurredOnUtc descending: most-recent-first is both the
// natural reading order for an audit trail and, independent of that,
// required for stable, deterministic pagination (SQL guarantees ordering
// only when an ORDER BY is present) — not a business rule.
internal sealed class AuditEntryRepository : IAuditEntryRepository
{
    private readonly TutorFlowDbContext _dbContext;

    public AuditEntryRepository(TutorFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<(IReadOnlyCollection<AuditEntryDto> Items, int TotalCount)> GetAllAsync(
        PageRequest pageRequest, Guid? subjectId, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.AuditEntries.AsNoTracking().AsQueryable();

        if (subjectId.HasValue)
        {
            query = query.Where(e => e.SubjectId == subjectId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(e => e.OccurredOnUtc)
            .Skip((pageRequest.Page - 1) * pageRequest.PageSize)
            .Take(pageRequest.PageSize)
            .Select(e => new AuditEntryDto(e.Id, e.OccurredOnUtc, e.Action, e.SubjectId, e.ActorId, e.ActorRole))
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
