using TutorFlow.Application.Common;
using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Identity.Interfaces;

// Repository abstraction only (docs/adr/ADR-005-application-boundary.md:
// Application Layer Responsibilities). Infrastructure implements this in a
// later phase; nothing here assumes a persistence technology.
public interface ITutorRepository
{
    Task<Tutor?> GetByIdAsync(AccountId id, CancellationToken cancellationToken = default);

    // Login lookup (docs/adr/ADR-017-authentication-mechanism-decision.md) —
    // email is unique per role, not globally, so this returns at most one Tutor.
    Task<Tutor?> GetByEmailAsync(EmailAddress email, CancellationToken cancellationToken = default);

    // Discoverable-only per Tutor.IsDiscoverable (IsApproved && !IsSuspended),
    // matching IDR-2/ADM-2's invariant.
    Task<IReadOnlyCollection<Tutor>> GetDiscoverableAsync(CancellationToken cancellationToken = default);

    // Serves ADM-1's "review pending Tutor registrations" need (PRODUCT_REQUIREMENTS.md
    // User Journey 5.4 step 1) — every Tutor not yet approved, regardless of any
    // later suspension state. Paginated (Sprint 2: Pagination Infrastructure) —
    // the underlying filter is a plain bool column, safely translatable to SQL
    // Skip/Take.
    Task<(IReadOnlyCollection<Tutor> Items, int TotalCount)> GetPendingAsync(PageRequest pageRequest, CancellationToken cancellationToken = default);

    // Discoverable-only (same gate as GetDiscoverableAsync), additionally
    // filtered by Subject/Language/Location when supplied (DISC-1). Matching
    // is case-insensitive, evaluated in memory rather than translated to SQL —
    // deliberately, since Subject/Language/Location are EF value-converted
    // Domain Value Objects, and comparing their .Value in a LINQ-to-Entities
    // query is not reliably translatable, and default SQL collation
    // case-sensitivity differs by provider (SQLite vs PostgreSQL). In-memory
    // comparison guarantees identical behavior across both. Unpaginated: the
    // caller (Application/Discovery) composes this with an Availability
    // filter from a different bounded context before paginating the final,
    // intersected result.
    Task<IReadOnlyCollection<Tutor>> SearchDiscoverableAsync(
        string? subject, string? language, string? location, CancellationToken cancellationToken = default);

    Task AddAsync(Tutor tutor, CancellationToken cancellationToken = default);
}
