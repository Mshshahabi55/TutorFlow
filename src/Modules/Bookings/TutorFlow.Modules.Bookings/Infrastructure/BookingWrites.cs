using Microsoft.EntityFrameworkCore;
using Npgsql;
using TutorFlow.Modules.Bookings.Domain;
using TutorFlow.Modules.Bookings.Persistence;

namespace TutorFlow.Modules.Bookings.Infrastructure;

// Shared write-path mechanics used by both CreateTrialHold and RescheduleBooking — both create
// a new Held row through the identical sweep-then-insert path (ADR-0013 §2-3, ADR-0017).
// Kept here once rather than duplicated, per "do not duplicate domain logic."
internal static class BookingWrites
{
    // Correctness-critical, not a hygiene task (ADR-0013 §2): a Held/PendingPayment row whose
    // TTL has passed must have its Status actually flipped before the exclusion constraint will
    // stop treating it as active. Must run inside the same transaction as the subsequent
    // INSERT, scoped to the specific tutor being requested.
    public static Task ExpireStaleHoldsAsync(BookingsDbContext db, Guid tutorUserId, DateTimeOffset now, CancellationToken ct)
    {
        return db.Bookings
            .Where(b => b.TutorUserId == tutorUserId
                && (b.Status == BookingStatus.Held || b.Status == BookingStatus.PendingPayment)
                && b.HoldExpiresAtUtc < now)
            .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.Status, BookingStatus.Expired)
                .SetProperty(b => b.HoldExpiresAtUtc, (DateTimeOffset?)null)
                .SetProperty(b => b.UpdatedAtUtc, now), ct);
    }

    // exclusion_violation (23P01) is not classified as a "transient" error by EF Core's
    // execution strategy, so — unlike serialization_failure/deadlock_detected (see
    // Availability's ReplaceWeeklyRules precedent) — it is never wrapped in an extra
    // InvalidOperationException even inside a manually-managed explicit transaction.
    // Single-row counterpart to ExpireStaleHoldsAsync above, used by BookingHoldExpirySweepService
    // (a separate, time-driven trigger — see that type's own doc comment for why the per-tutor
    // lazy sweep above cannot serve this purpose). Same exact guard (Status + HoldExpiresAtUtc),
    // just scoped to one booking instead of one tutor's whole set, and clearing HoldExpiresAtUtc
    // afterward to match. Returns the affected-row count (0 or 1) as the real source of truth for
    // whether *this* caller is the one that performed the transition — the same "only publish if
    // affected == 1" convention ConfirmBooking/CheckoutBooking already use, so a redelivered or
    // concurrently-racing sweep never double-fires BookingHoldExpired for the same row.
    public static Task<int> ExpireSingleHoldIfStaleAsync(BookingsDbContext db, Guid bookingId, DateTimeOffset now, CancellationToken ct)
    {
        return db.Bookings
            .Where(b => b.Id == bookingId
                && (b.Status == BookingStatus.Held || b.Status == BookingStatus.PendingPayment)
                && b.HoldExpiresAtUtc < now)
            .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.Status, BookingStatus.Expired)
                .SetProperty(b => b.HoldExpiresAtUtc, (DateTimeOffset?)null)
                .SetProperty(b => b.UpdatedAtUtc, now), ct);
    }

    public static bool IsExclusionViolation(Exception ex) =>
        ex is DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.ExclusionViolation } };
}
