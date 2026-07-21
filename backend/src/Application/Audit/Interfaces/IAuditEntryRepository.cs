using TutorFlow.Application.Audit.DTOs;
using TutorFlow.Application.Common;

namespace TutorFlow.Application.Audit.Interfaces;

// Repository abstraction only (docs/adr/ADR-005-application-boundary.md).
// Infrastructure implements this against the already-persisted AuditEntry
// table (docs/adr/ADR-016-audit-durability-strategy.md) — this interface
// introduces no new schema, no new event, and no new write path; it is a
// read-only addition around already-existing, already-durable data.
//
// Returns AuditEntryDto directly rather than a Domain/Infrastructure entity,
// unlike every other repository interface in this codebase: AuditEntry has
// no Domain representation for Application to depend on, so Infrastructure
// must perform the DTO translation itself, inside the repository
// implementation (see AuditEntryDto's own remarks).
public interface IAuditEntryRepository
{
    Task<(IReadOnlyCollection<AuditEntryDto> Items, int TotalCount)> GetAllAsync(
        PageRequest pageRequest, Guid? subjectId, CancellationToken cancellationToken = default);
}
