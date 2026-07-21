using TutorFlow.Application.Audit.DTOs;
using TutorFlow.Application.Audit.Interfaces;
using TutorFlow.Application.Common;

namespace TutorFlow.Application.Tests.TestDoubles;

internal sealed class InMemoryAuditEntryRepository : IAuditEntryRepository
{
    private readonly List<AuditEntryDto> _entries = new();

    public void Add(AuditEntryDto entry) => _entries.Add(entry);

    public Task<(IReadOnlyCollection<AuditEntryDto> Items, int TotalCount)> GetAllAsync(
        PageRequest pageRequest, Guid? subjectId, CancellationToken cancellationToken = default)
    {
        var filtered = subjectId is null
            ? _entries.AsEnumerable()
            : _entries.Where(e => e.SubjectId == subjectId.Value);

        var ordered = filtered.OrderByDescending(e => e.OccurredOnUtc).ToList();

        IReadOnlyCollection<AuditEntryDto> page = ordered
            .Skip((pageRequest.Page - 1) * pageRequest.PageSize)
            .Take(pageRequest.PageSize)
            .ToList();

        return Task.FromResult((page, ordered.Count));
    }
}
