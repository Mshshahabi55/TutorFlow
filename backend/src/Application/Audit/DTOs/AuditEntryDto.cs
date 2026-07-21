namespace TutorFlow.Application.Audit.DTOs;

// Application's own translated representation of an audit record. Note this
// is not a "FromDomain" projection like every other DTO in this codebase:
// the underlying AuditEntry (Infrastructure/Audit/AuditEntry.cs) is
// Infrastructure-internal derived data with no Domain representation
// (docs/adr/ADR-016-audit-durability-strategy.md: Impact on DDD), so
// Application cannot reference it at all (Application depends only on
// Domain). The mapping from AuditEntry to this DTO therefore happens inside
// Infrastructure's AuditEntryRepository, not here — the only repository in
// this codebase where that direction of mapping is necessary.
public sealed record AuditEntryDto(
    Guid Id,
    DateTime OccurredOnUtc,
    string Action,
    Guid? SubjectId,
    string? ActorId,
    string? ActorRole);
