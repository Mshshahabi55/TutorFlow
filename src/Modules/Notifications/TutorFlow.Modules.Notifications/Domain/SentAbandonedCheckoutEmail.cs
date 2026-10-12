namespace TutorFlow.Modules.Notifications.Domain;

/// <summary>
/// Append-only idempotency/audit log — one row per <c>BookingId</c> this module ever attempts to
/// notify about an abandoned checkout, ever (ADR-0029). The DB-unique index on
/// <see cref="BookingId"/> (not an in-memory dictionary) is the real idempotency guard: claiming
/// a row for a given booking happens once, in the same unit of work as the send attempt, so a
/// redelivered <c>BookingHoldExpired</c> event, a retried handler, or a process restart can never
/// cause a second email for the same booking. A row existing at all means "sending was already
/// attempted" — <see cref="WasSent"/> distinguishes "attempted and succeeded" from "attempted
/// and skipped/failed", so a permanent failure is recorded honestly rather than retried forever
/// or silently repeated.
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

    public DateTimeOffset ClaimedAtUtc { get; private init; }

    public DateTimeOffset? SentAtUtc { get; private set; }

    public bool WasSent { get; private set; }

    /// <summary>Non-null only when <see cref="WasSent"/> is false — e.g. "No email on file" or a
    /// provider error summary. Never the recipient's email address or the email body itself.</summary>
    public string? SkippedReason { get; private set; }

    public static SentAbandonedCheckoutEmail Claim(Guid bookingId, Guid studentUserId, Guid tutorUserId, TimeProvider clock) => new()
    {
        Id = Guid.NewGuid(),
        BookingId = bookingId,
        StudentUserId = studentUserId,
        TutorUserId = tutorUserId,
        ClaimedAtUtc = clock.GetUtcNow()
    };

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
