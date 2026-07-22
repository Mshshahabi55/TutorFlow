namespace TutorFlow.Domain.Common;

// ADR-019 (+ Addendum 1): Iranian Rial has no minor unit, and this codebase
// prices exclusively in whole Toman (1 Toman = 10 Rial) — a fact shared by
// every context that models a Rial amount, not owned by any one of them.
// Currently used by HourlyRate (Identity & Relationship) and SessionPrice
// (Scheduling & Booking, Phase 4.6) — each stays its own distinct value
// object per ADR-002's Context Ownership, but both validate through this
// one shared rule rather than each keeping an independent copy of it.
public static class RialAmount
{
    /// <summary>ISO 4217 code for the only currency this codebase represents (ADR-019).</summary>
    public const string CurrencyCode = "IRR";

    // The largest multiple of 10 within numeric(12,0)'s ceiling (twelve
    // nines) — 999,999,999,999 itself is not a legal amount under the
    // whole-Toman invariant below, so the true maximum is one Rial short
    // of it (ADR-019 Addendum 1).
    public const decimal MaxAmount = 999_999_999_990m;

    private const decimal RialPerToman = 10m;

    /// <summary>Validates a Rial amount is positive, evenly divisible by 10, and within MaxAmount. Returns it unchanged.</summary>
    public static decimal Validate(decimal amount, string parameterName)
    {
        Guard.Against.NegativeOrZero(amount, parameterName);

        // Subsumes "must be a whole number": anything that divides 10
        // exactly is necessarily an integer itself.
        if (amount % RialPerToman != 0)
        {
            throw new ArgumentException(
                "TutorFlow prices in whole Toman (1 Toman = 10 Rial); amount must be a whole " +
                "number evenly divisible by 10.", parameterName);
        }

        if (amount > MaxAmount)
        {
            throw new ArgumentOutOfRangeException(
                parameterName, amount, $"Amount must not exceed {MaxAmount:N0} Rial.");
        }

        return amount;
    }
}
