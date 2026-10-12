using TutorFlow.Modules.Bookings.Features.CancelBookingForPaymentFailure;
using TutorFlow.Shared;

namespace TutorFlow.Modules.Bookings.Consumers;

/// <summary>
/// Wolverine discovers this by convention — drives the new, webhook-triggered cancellation
/// path (ADR-0027 §5, OPEN_QUESTIONS.md A33). Treated as terminal: V1 does not support in-place
/// retry with a different payment method on the same booking (ADR-0015 §2).
/// </summary>
public sealed class PaymentFailedConsumer(CancelBookingForPaymentFailure.Handler cancelForFailure)
{
    public async Task HandleAsync(PaymentFailed message, CancellationToken ct)
    {
        await cancelForFailure.HandleAsync(message.BookingId, message.Reason, ct);
    }
}
