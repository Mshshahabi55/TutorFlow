namespace TutorFlow.Modules.Bookings.Domain;

// Production state transitions (checkout, cancel, confirm, reschedule) are expressed as
// conditional UPDATE ... WHERE Status = @expected statements in the feature handlers, not as
// load-then-mutate instance methods — ADR-0013 §3 requires this specifically for Bookings'
// concurrency-critical transitions (a single atomic statement, no read-then-write gap). The
// guard sets below (CancellableStatuses, TerminalStatuses) are the single source of truth for
// those WHERE clauses AND are reusable, DB-free, for domain unit tests.
public sealed class Booking
{
    // The set of statuses during which a booking occupies the tutor's calendar — exactly the
    // exclusion constraint's protected WHERE-clause set (ADR-0013 §1). Also, coincidentally but
    // not accidentally, exactly the set a booking may be cancelled from (ADR-0013's protected
    // set and "cancellable" happen to be the same three statuses — Rescheduled/Expired and the
    // rest are terminal either way).
    public static readonly BookingStatus[] ActiveStatuses = [BookingStatus.Held, BookingStatus.PendingPayment, BookingStatus.Confirmed];

    public static readonly BookingStatus[] TerminalStatuses =
        [BookingStatus.Rescheduled, BookingStatus.Expired, BookingStatus.Completed, BookingStatus.NoShow, BookingStatus.Refunded, BookingStatus.Cancelled];

    // A trial "uses up" the one-trial-per-tutor allowance once it gets past a mere momentary
    // Held/Expired/Cancelled/Refunded state (BOOKINGS_SPEC.md, GetTrialEligibility) — mirrors
    // Preply's own model (a missed or cancelled trial is explicitly eligible for a replacement
    // per their help content), not an invented rule. Single source of truth shared by
    // GetTrialEligibility (advisory, for the frontend CTA) and CreateTrialHold (the actual
    // server-side enforcement gate).
    public static readonly BookingStatus[] UsedTrialStatuses =
        [BookingStatus.PendingPayment, BookingStatus.Confirmed, BookingStatus.InProgress, BookingStatus.Completed, BookingStatus.NoShow];

    private Booking()
    {
    }

    public Guid Id { get; private init; }

    public Guid StudentUserId { get; private init; }

    public Guid TutorUserId { get; private init; }

    public BookingStatus Status { get; private init; }

    public int DurationMinutes { get; private init; }

    public DateTimeOffset StartUtc { get; private init; }

    public DateTimeOffset EndUtc { get; private init; }

    public DateTimeOffset HeldAtUtc { get; private init; }

    public DateTimeOffset? HoldExpiresAtUtc { get; private init; }

    public DateTimeOffset? ConfirmedAtUtc { get; private init; }

    public DateTimeOffset? CancelledAtUtc { get; private init; }

    public Guid? CancelledByUserId { get; private init; }

    public string? CancellationReason { get; private init; }

    public int PriceAmountMinorUnits { get; private init; }

    public string PriceCurrency { get; private init; } = string.Empty;

    public string? PaymentReferenceId { get; private init; }

    public Guid? SubscriptionReferenceId { get; private init; }

    public Guid? RescheduledFromBookingId { get; private init; }

    public Guid? RescheduledToBookingId { get; private init; }

    /// <summary>True for every booking created today — <c>CreateTrialHold</c> is currently the
    /// only creation path in this module (no regular/subscription-lesson booking exists yet).
    /// Explicit rather than inferred so the Tutor Profile's trial-eligibility read
    /// (GetTrialEligibility, ADR-0022) has a real field to query once a second creation path
    /// (regular lessons) exists, instead of silently becoming wrong at that point.</summary>
    public bool IsTrial { get; private init; }

    public DateTimeOffset CreatedAtUtc { get; private init; }

    public DateTimeOffset UpdatedAtUtc { get; private init; }

    public Money Price => new(PriceAmountMinorUnits, PriceCurrency);

    public bool IsTerminal => TerminalStatuses.Contains(Status);

    public static Result<Booking> CreateHeld(
        Guid studentUserId,
        Guid tutorUserId,
        int durationMinutes,
        DateTimeOffset startUtc,
        DateTimeOffset holdExpiresAtUtc,
        Money price,
        TimeProvider clock,
        bool isTrial,
        Guid? rescheduledFromBookingId = null)
    {
        if (durationMinutes <= 0)
        {
            return Result.Failure<Booking>("Duration must be positive.");
        }

        var endUtc = startUtc.AddMinutes(durationMinutes);
        if (startUtc >= endUtc)
        {
            return Result.Failure<Booking>("A booking's start must be before its end.");
        }

        var now = clock.GetUtcNow();
        return Result.Success(new Booking
        {
            Id = Guid.NewGuid(),
            StudentUserId = studentUserId,
            TutorUserId = tutorUserId,
            Status = BookingStatus.Held,
            DurationMinutes = durationMinutes,
            StartUtc = startUtc,
            EndUtc = endUtc,
            HeldAtUtc = now,
            HoldExpiresAtUtc = holdExpiresAtUtc,
            PriceAmountMinorUnits = price.AmountMinorUnits,
            PriceCurrency = price.Currency,
            IsTrial = isTrial,
            RescheduledFromBookingId = rescheduledFromBookingId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
    }
}
