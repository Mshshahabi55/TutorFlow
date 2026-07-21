using TutorFlow.Domain.Common;

namespace TutorFlow.Domain.Identity.ValueObjects;

// A Tutor's rate, visible to Students/Parents (PRODUCT_REQUIREMENTS.md DISC-2).
// Currency/format is not established by any approved document
// (DOMAIN_MODEL.md Open Question 18) and is deliberately not modeled here.
public sealed class HourlyRate : ValueObject
{
    private HourlyRate(decimal amount) => Amount = amount;

    public decimal Amount { get; }

    public static HourlyRate Of(decimal amount) =>
        new(Guard.Against.NegativeOrZero(amount, nameof(amount)));

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
    }
}
