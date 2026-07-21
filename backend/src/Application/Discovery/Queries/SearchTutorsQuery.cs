namespace TutorFlow.Application.Discovery.Queries;

// Discovery composes reads over Identity & Relationship (Tutor) and
// Scheduling & Booking (AvailabilitySlot) data; it owns no aggregate of its
// own (ARCHITECTURE.md Section 4; docs/adr/ADR-002-domain-boundaries.md:
// Context Ownership). All filters are optional (DISC-1) — an all-null
// request behaves like the existing unfiltered discoverable Tutor list.
// Paginated (Backend Gap Closure Plan, Sprint 2).
public sealed record SearchTutorsQuery(
    string? Subject,
    string? Language,
    string? Location,
    DateTime? AvailableFrom,
    int Page,
    int PageSize);
