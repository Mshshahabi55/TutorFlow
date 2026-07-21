namespace TutorFlow.Application.Audit.Queries;

// Read access to the audit trail ADR-016 already writes (Backend Completion
// Phase, Track A, Phase A1). SubjectId is optional — an all-null request
// returns every audited action across the platform; a provided SubjectId
// scopes to one aggregate's history (ADR-009: Traceability Principles —
// "an Admin/Staff member can reconstruct a Session's entire history").
// Paginated, mirroring GetAllSessionsQuery's existing shape.
public sealed record GetAuditEntriesQuery(Guid? SubjectId, int Page, int PageSize);
