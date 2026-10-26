namespace TutorFlow.Shared;

public interface ITutorAvailabilityQuery
{
    Task<SlotValidationResult> ValidateSlotAsync(Guid tutorUserId, DateTimeOffset startUtc, DateTimeOffset endUtc, CancellationToken ct);

    /// <summary>Whether this tutor is currently bookable at all, independent of any specific
    /// slot — the same <c>TutorBookabilityRecords</c> signal <see cref="ValidateSlotAsync"/>
    /// already checks first (ADR-0014), reused by <c>Messaging</c> (ADR-0030) as its conversation-
    /// eligibility gate so a second, independent "is this tutor published" source of truth is
    /// never created. Fail-closed on a missing record, exactly like <see cref="ValidateSlotAsync"/>
    /// — never defaults a tutor Availability hasn't heard about yet to eligible.</summary>
    Task<bool> IsTutorBookableAsync(Guid tutorUserId, CancellationToken ct);
}

public sealed record SlotValidationResult(bool IsValid, string? Reason);
