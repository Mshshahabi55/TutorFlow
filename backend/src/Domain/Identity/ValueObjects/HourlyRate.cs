using TutorFlow.Domain.Common;

namespace TutorFlow.Domain.Identity.ValueObjects;

// A Tutor's rate, visible to Students/Parents (PRODUCT_REQUIREMENTS.md DISC-2).
// ADR-019 (+ Addendum 1, Phase 4.5): v1 prices exclusively in Iranian Rial
// (IRR), stored as whole numbers evenly divisible by 10 — the product is
// priced in whole Toman (1 Toman = 10 Rial), and Rial has no practical
// minor unit of its own, so an Amount that isn't a multiple of 10 has no
// price a user could have actually entered. Toman is a presentation-only
// concern, converted at exactly one seam (frontend/src/shared/money/),
// never assumed here — but the divisibility invariant itself belongs in
// Domain, not in that presentation seam's exception path (Addendum 1).
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

    // The largest multiple of 10 within numeric(12,0)'s ceiling (twelve
    // nines) — 999,999,999,999 itself is not a legal Amount under the
    // whole-Toman invariant below, so the true maximum is one Rial short of
    // it. Still roughly four orders of magnitude above any
    // currently-plausible hourly rate even accounting for Rial's history of
    // rapid devaluation, chosen deliberately generous so this column does
    // not need a second migration for a long time.
    public const decimal MaxAmount = 999_999_999_990m;

    private const decimal RialPerToman = 10m;

    private HourlyRate(decimal amount) => Amount = amount;

    public decimal Amount { get; }

    public string Currency => CurrencyCode;

    public static HourlyRate Of(decimal amount)
    {
        Guard.Against.NegativeOrZero(amount, nameof(amount));

        // Subsumes the old "must be a whole number" check: anything that
        // divides 10 exactly is necessarily an integer itself.
        if (amount % RialPerToman != 0)
        {
            throw new ArgumentException(
                "TutorFlow prices in whole Toman (1 Toman = 10 Rial); amount must be a whole " +
                "number evenly divisible by 10.", nameof(amount));
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
