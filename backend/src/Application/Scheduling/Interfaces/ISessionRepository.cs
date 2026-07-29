using TutorFlow.Application.Common;
using TutorFlow.Domain.Scheduling;
using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Application.Scheduling.Interfaces;

public interface ISessionRepository
{
    Task<Session?> GetByIdAsync(SessionId id, CancellationToken cancellationToken = default);

    // Unscoped by actor — serves ADM-3's "view all schedules across the
    // platform" need. Paginated (Backend Gap Closure Plan, Sprint 2/3:
    // Pagination Infrastructure) since Sessions is the table most likely to
    // grow large fastest.
    Task<(IReadOnlyCollection<Session> Items, int TotalCount)> GetAllAsync(PageRequest pageRequest, CancellationToken cancellationToken = default);

    // Every status included — the schedule view shows a Session's status,
    // it does not filter by it (PRODUCT_REQUIREMENTS.md 5.1 step 5, 5.3 step 5).
    Task<IReadOnlyCollection<Session>> GetByTutorIdAsync(TutorId tutorId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Session>> GetByStudentIdAsync(StudentId studentId, CancellationToken cancellationToken = default);

    // An AvailabilitySlot produces at most one Session (CONST-1) — used to
    // resolve ADR-003's Addendum Decision 4 (Availability Slot visibility),
    // which needs to know the booked Session's Student/Parent-Guardian to
    // decide read access. Session.AvailabilitySlotId already exists on the
    // aggregate; this exposes an existing fact, it does not add one.
    Task<Session?> GetByAvailabilitySlotIdAsync(AvailabilitySlotId availabilitySlotId, CancellationToken cancellationToken = default);

    // Platform-wide, unscoped by actor (same audience as GetAllAsync above,
    // ADM-3) — a DB-side GROUP BY, never every Session row pulled into
    // memory just to count them, since Sessions is the table most likely
    // to grow large fastest (GetAllAsync's own comment).
    Task<IReadOnlyDictionary<SessionStatus, int>> GetStatusCountsAsync(CancellationToken cancellationToken = default);

    Task AddAsync(Session session, CancellationToken cancellationToken = default);
}
