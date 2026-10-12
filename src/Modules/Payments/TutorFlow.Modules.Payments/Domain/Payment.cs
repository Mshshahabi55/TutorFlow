namespace TutorFlow.Modules.Payments.Domain;

// Production state transitions (requires-action, processing, succeeded, failed) are expressed as
// conditional UPDATE ... WHERE Status = @expected statements in the webhook feature handler, not
// load-then-mutate instance methods — mirrors Bookings' own ADR-0013 §3 convention exactly, for
// the same reason (a single atomic statement, safe against out-of-order/duplicate webhook
// delivery).
public sealed class Payment
{
    private Payment()
    {
    }

    public Guid Id { get; private init; }

    public Guid BookingId { get; private init; }

    public string? StripePaymentIntentId { get; private init; }

    public Guid StudentUserId { get; private init; }

    public Guid TutorUserId { get; private init; }

    public int AmountMinorUnits { get; private init; }

    public string Currency { get; private init; } = string.Empty;

    public PaymentStatus Status { get; private init; }

    public string IdempotencyKey { get; private init; } = string.Empty;

    public DateTimeOffset? ConfirmedAtUtc { get; private init; }

    public DateTimeOffset? FailedAtUtc { get; private init; }

    public string? FailureReason { get; private init; }

    public DateTimeOffset CreatedAtUtc { get; private init; }

    public DateTimeOffset UpdatedAtUtc { get; private init; }

    public Money Price => new(AmountMinorUnits, Currency);

    public static Payment Create(
        Guid bookingId,
        Guid studentUserId,
        Guid tutorUserId,
        Money price,
        string idempotencyKey,
        string? stripePaymentIntentId,
        TimeProvider clock)
    {
        var now = clock.GetUtcNow();
        return new Payment
        {
            Id = Guid.NewGuid(),
            BookingId = bookingId,
            StudentUserId = studentUserId,
            TutorUserId = tutorUserId,
            AmountMinorUnits = price.AmountMinorUnits,
            Currency = price.Currency,
            Status = PaymentStatus.Created,
            IdempotencyKey = idempotencyKey,
            StripePaymentIntentId = stripePaymentIntentId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }
}
