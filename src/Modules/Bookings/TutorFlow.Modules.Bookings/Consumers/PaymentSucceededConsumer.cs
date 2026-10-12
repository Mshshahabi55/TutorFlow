using TutorFlow.Modules.Bookings.Features.ConfirmBooking;
using TutorFlow.Shared;

namespace TutorFlow.Modules.Bookings.Consumers;

/// <summary>
/// Wolverine discovers this by convention (a public <c>HandleAsync</c> instance method) —
/// <c>Payments</c>' first real publisher, consumed here for the first time (ADR-0027 §5).
/// Drives the already-built, already-idempotent <see cref="ConfirmBooking.Handler"/> — no
/// change to its existing shape, just a new caller.
/// </summary>
public sealed class PaymentSucceededConsumer(ConfirmBooking.Handler confirmBooking)
{
    public async Task HandleAsync(PaymentSucceeded message, CancellationToken ct)
    {
        await confirmBooking.HandleAsync(message.BookingId, message.PaymentReferenceId.ToString(), ct);
    }
}
