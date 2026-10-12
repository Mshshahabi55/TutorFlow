namespace TutorFlow.Modules.Tutors.Domain;

public sealed record Money(int AmountMinorUnits, string Currency)
{
    public static Money? TryCreate(int amountMinorUnits, string? currency)
    {
        if (amountMinorUnits < 0 || string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
        {
            return null;
        }

        return new Money(amountMinorUnits, currency.ToUpperInvariant());
    }
}
