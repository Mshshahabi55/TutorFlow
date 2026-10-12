namespace TutorFlow.Shared;

public interface ITutorAvailabilityQuery
{
    Task<SlotValidationResult> ValidateSlotAsync(Guid tutorUserId, DateTimeOffset startUtc, DateTimeOffset endUtc, CancellationToken ct);
}

public sealed record SlotValidationResult(bool IsValid, string? Reason);
