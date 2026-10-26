namespace TutorFlow.Modules.Notifications.Domain;

/// <summary>
/// Append-only idempotency/audit log — one row per <c>BookingId</c> this module ever attempts to
/// notify about an abandoned checkout, ever (ADR-0029, Addendum A). The DB-unique index on
/// <see cref="BookingId"/> (not an in-memory dictionary) prevents a second row for the same
/// booking; it does NOT by itself prove a row is finished being processed — <see cref="WasSent"/>
/// / <see cref="SkippedReason"/> together are the only resolved/terminal signal
/// (<c>WasSent || SkippedReason != null</c>). A row that is neither is "unresolved": either
/// genuinely in-flight right now, or abandoned by a crash/ambiguous commit/cancellation.
/// <see cref="OwnerToken"/>/<see cref="OwnedUntilUtc"/> are the fencing-token lease that lets
/// <c>AbandonedCheckoutClaimReconciliationService</c> safely distinguish and take over the latter
/// without ever reopening a resolved row or racing a worker that is still genuinely active.
/// </summary>
public sealed class SentAbandonedCheckoutEmail
{
    private SentAbandonedCheckoutEmail()
    {
    }

    public Guid Id { get; private init; }

    public Guid BookingId { get; private init; }

    public Guid StudentUserId { get; private init; }

    public Guid TutorUserId { get; private init; }

    /// <summary>Captured from <c>BookingHoldExpired</c> at claim time so a reconciliation
    /// takeover can rebuild the recovery email without Notifications ever reaching into
    /// Bookings' own schema (ADR-0014's established own-copy-of-cross-module-data pattern).</summary>
    public DateTimeOffset StartUtc { get; private init; }

    public DateTimeOffset ClaimedAtUtc { get; private init; }

    public DateTimeOffset? SentAtUtc { get; private set; }

    public bool WasSent { get; private set; }

    /// <summary>Non-null only when <see cref="WasSent"/> is false — e.g. "No email on file" or a
    /// provider error summary. Never the recipient's email address or the email body itself.</summary>
    public string? SkippedReason { get; private set; }

    /// <summary>Fencing token identifying whichever worker currently owns the right to process
    /// this row. Reassigned on initial claim and on every reconciliation takeover. A worker must
    /// re-verify this token still matches its own copy immediately before the email-send side
    /// effect, and the final resolve write is itself guarded by it — see
    /// <c>SendAbandonedCheckoutRecoveryEmail.Handler</c>.</summary>
    public Guid OwnerToken { get; private set; }

    /// <summary>Lease expiry for the current owner. Only consulted for still-unresolved rows;
    /// reconciliation only takes over when this is in the past. Not a renewed heartbeat — a
    /// single window set generously longer than realistic end-to-end processing time (including
    /// the first-dispatch latency documented in this project's own reliability investigation).</summary>
    public DateTimeOffset OwnedUntilUtc { get; private set; }

    public static SentAbandonedCheckoutEmail Claim(
        Guid bookingId, Guid studentUserId, Guid tutorUserId, DateTimeOffset startUtc,
        Guid ownerToken, TimeSpan leaseDuration, TimeProvider clock)
    {
        var now = clock.GetUtcNow();
        return new SentAbandonedCheckoutEmail
        {
            Id = Guid.NewGuid(),
            BookingId = bookingId,
            StudentUserId = studentUserId,
            TutorUserId = tutorUserId,
            StartUtc = startUtc,
            ClaimedAtUtc = now,
            OwnerToken = ownerToken,
            OwnedUntilUtc = now + leaseDuration
        };
    }

    public void MarkSent(TimeProvider clock)
    {
        WasSent = true;
        SentAtUtc = clock.GetUtcNow();
    }

    public void MarkSkipped(string reason)
    {
        WasSent = false;
        SkippedReason = reason;
    }
}
