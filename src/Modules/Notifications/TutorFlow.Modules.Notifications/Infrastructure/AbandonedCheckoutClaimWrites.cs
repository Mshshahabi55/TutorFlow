using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Notifications.Persistence;

namespace TutorFlow.Modules.Notifications.Infrastructure;

/// <summary>
/// Guarded, set-based writes against <c>SentAbandonedCheckoutEmail</c>'s ownership fencing token
/// (ADR-0029 Addendum A) — mirrors Bookings' <c>BookingWrites.ExpireSingleHoldIfStaleAsync</c>
/// exactly: <c>ExecuteUpdateAsync</c>'s affected-row count, not entity tracking, is the real "did
/// I win this race" signal, so a live handler invocation, a reconciliation takeover, and two
/// concurrent reconciliation ticks can never all believe they own the same row.
/// </summary>
internal static class AbandonedCheckoutClaimWrites
{
    /// <summary>Takes over a claim only if it is still unresolved and its lease has expired.
    /// Returns the affected-row count (0 or 1) — 1 means this caller now owns it.</summary>
    public static Task<int> TryTakeOverStaleAsync(
        NotificationsDbContext db, Guid bookingId, Guid newOwnerToken, DateTimeOffset now, TimeSpan leaseDuration, CancellationToken ct) =>
        db.SentAbandonedCheckoutEmails
            .Where(e => e.BookingId == bookingId && !e.WasSent && e.SkippedReason == null && e.OwnedUntilUtc < now)
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.OwnerToken, newOwnerToken)
                .SetProperty(e => e.OwnedUntilUtc, now + leaseDuration), ct);

    public static Task<bool> StillOwnsAsync(NotificationsDbContext db, Guid bookingId, Guid ownerToken, CancellationToken ct) =>
        db.SentAbandonedCheckoutEmails.AsNoTracking().AnyAsync(e => e.BookingId == bookingId && e.OwnerToken == ownerToken, ct);

    /// <summary>Final resolve write, guarded by the owner token — not a blind tracked-entity
    /// save, so a takeover landing in the narrow window right before this call is detected
    /// (affected == 0) instead of silently overwritten. Returns the affected-row count.</summary>
    public static Task<int> TryResolveAsOwnerAsync(
        NotificationsDbContext db, Guid bookingId, Guid ownerToken, bool wasSent, DateTimeOffset? sentAtUtc, string? skippedReason, CancellationToken ct) =>
        db.SentAbandonedCheckoutEmails
            .Where(e => e.BookingId == bookingId && e.OwnerToken == ownerToken)
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.WasSent, wasSent)
                .SetProperty(e => e.SentAtUtc, sentAtUtc)
                .SetProperty(e => e.SkippedReason, skippedReason), ct);
}
