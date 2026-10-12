namespace TutorFlow.Shared;

public interface ITutorPricingQuery
{
    Task<TutorPrice?> GetCurrentPriceAsync(Guid tutorUserId, int durationMinutes, CancellationToken ct);
}

public sealed record TutorPrice(int AmountMinorUnits, string Currency);
