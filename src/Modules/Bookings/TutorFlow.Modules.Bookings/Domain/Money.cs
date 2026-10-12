namespace TutorFlow.Modules.Bookings.Domain;

// Module-local value object mirroring Tutors.Domain.Money exactly — not promoted to
// TutorFlow.Shared; A13 remains explicitly deferred (docs/domain/DOMAIN_MODEL.md).
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
