using TutorFlow.Shared;
using SendAbandonedCheckoutRecoveryEmailFeature = TutorFlow.Modules.Notifications.Features.SendAbandonedCheckoutRecoveryEmail.SendAbandonedCheckoutRecoveryEmail;

namespace TutorFlow.Modules.Notifications.Consumers;

/// <summary>
/// Wolverine discovers this by convention (a public <c>HandleAsync</c> instance method) —
/// ADR-0029, the same shape as <c>Search.TutorPublishedConsumer</c>. Thin delegation to the
/// real feature handler, never business logic of its own.
/// </summary>
public sealed class BookingHoldExpiredConsumer(SendAbandonedCheckoutRecoveryEmailFeature.Handler handler)
{
    public Task HandleAsync(BookingHoldExpired message, CancellationToken ct) => handler.HandleAsync(message, ct);
}
