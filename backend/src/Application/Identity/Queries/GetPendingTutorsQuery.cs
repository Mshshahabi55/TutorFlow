namespace TutorFlow.Application.Identity.Queries;

// Serves ADM-1's "review pending Tutor registrations" need
// (PRODUCT_REQUIREMENTS.md User Journey 5.4 step 1) — deliberately a separate
// query from GetTutorListQuery, which returns only discoverable Tutors.
// Paginated (Backend Gap Closure Plan, Sprint 2: Pagination Infrastructure).
public sealed record GetPendingTutorsQuery(int Page, int PageSize);
