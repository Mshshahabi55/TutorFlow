namespace TutorFlow.Application.Oversight.Queries;

// Marketplace Oversight's first real capability (ADM-3: "view all schedules
// across the platform"). Owns no aggregate of its own — this reads
// Scheduling & Booking's own Session data through that context's own
// repository interface (ARCHITECTURE.md Section 4; docs/adr/ADR-002-domain-
// boundaries.md: Context Ownership). Paginated (Backend Gap Closure Plan,
// Sprint 2/3).
public sealed record GetAllSessionsQuery(int Page, int PageSize);
