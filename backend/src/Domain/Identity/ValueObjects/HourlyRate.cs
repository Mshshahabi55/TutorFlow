using TutorFlow.Domain.Common;

namespace TutorFlow.Domain.Identity.ValueObjects;

// A Tutor's rate, visible to Students/Parents (PRODUCT_REQUIREMENTS.md DISC-2).
// ADR-019: v1 prices exclusively in Iranian Rial (IRR), stored as whole
// numbers — Rial has no practical minor unit, so there is no fractional
// Rial anywhere in this model. Toman (1 Toman = 10 Rial) is a presentation-
// only concern, converted at exactly one seam (frontend/src/shared/money/),
// never assumed here.
//
// CurrencyCode is a constant, not a per-instance field/value object,
// deliberately: v1 has exactly one currency, so per-instance state would be
// pure duplication — and this phase's own rule forbids building a multi-
// currency abstraction now. A constant is still a real, typed, grep-able
// fact in the model (not a comment), which is what ADR-018's Forward
// Compatibility clause requires ("which currency" must remain a nameable
// fact even with only one value). Widening to multi-currency in v2 means
// changing this one type's shape (e.g. adding a Currency parameter to Of)
// — every caller already goes through it — not hunting down scattered
// assumptions.
public sealed class HourlyRate : ValueObject
{
    /// <summary>ISO 4217 code for the only currency this model represents (ADR-019).</summary>
    public const string CurrencyCode = "IRR";

    // numeric(12,0)'s ceiling (see TutorConfiguration/the Phase 4 migration)
    // — twelve nines. Roughly four orders of magnitude above any
    // currently-plausible hourly rate even accounting for Rial's history of
    // rapid devaluation, chosen deliberately generous so this column does
    // not need a second migration for a long time.
    public const decimal MaxAmount = 999_999_999_999m;

    private HourlyRate(decimal amount) => Amount = amount;

    public decimal Amount { get; }

    public string Currency => CurrencyCode;

    public static HourlyRate Of(decimal amount)
    {
        Guard.Against.NegativeOrZero(amount, nameof(amount));

        if (amount != decimal.Truncate(amount))
        {
            throw new ArgumentException(
                "Iranian Rial has no minor unit; amount must be a whole number.", nameof(amount));
        }

        if (amount > MaxAmount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(amount), amount, $"Amount must not exceed {MaxAmount:N0} Rial.");
        }

        return new HourlyRate(amount);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
