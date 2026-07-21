namespace TutorFlow.Application.Common;

// Applied only where a result set genuinely warrants it (Backend Gap Closure
// Plan, Sprint 2: Pagination Infrastructure) — not retrofitted onto every
// list query. MinPageSize/MaxPageSize/DefaultPageSize are technical safety
// limits, not business rules (no approved document establishes a scale
// target — ARCHITECTURE.md Section 14).
public sealed record PageRequest(int Page, int PageSize)
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 20;
    public const int MinPageSize = 1;
    public const int MaxPageSize = 100;
}
