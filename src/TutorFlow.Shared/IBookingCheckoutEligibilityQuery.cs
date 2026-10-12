namespace TutorFlow.Shared;

/// <summary>
/// Consumed via DI by <c>Payments</c>, implemented by <c>Bookings</c> (ADR-0019, ADR-0027) — the
/// fourth instance of ADR-0014's read-only cross-module query pattern, now in a new direction
/// (<c>Payments</c> consuming from <c>Bookings</c>). <c>Payments</c> uses this, and only this, to
/// obtain the authoritative price/currency/tutor for a checkout call — never a caller-supplied
/// value. <c>IsEligible</c> requires the booking to exist, be owned by <paramref
/// name="callerStudentUserId"/>-equivalent, and be <c>PendingPayment</c> specifically (not
/// <c>Held</c>) — that ordering, not a new auth scheme, is what prevents a bypass of <c>Bookings</c>'
/// own checkout handler.
/// </summary>
public interface IBookingCheckoutEligibilityQuery
{
    Task<BookingCheckoutEligibility> ValidateAsync(Guid bookingId, Guid callerStudentUserId, CancellationToken ct);
}

public sealed record BookingCheckoutEligibility(
    bool IsEligible,
    string? Reason,
    int? AmountMinorUnits,
    string? Currency,
    Guid? TutorUserId);
