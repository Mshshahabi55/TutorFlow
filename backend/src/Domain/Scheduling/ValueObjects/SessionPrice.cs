using TutorFlow.Domain.Common;

namespace TutorFlow.Domain.Scheduling.ValueObjects;

// What a Session cost, captured from the Tutor's HourlyRate at the moment
// it was booked (Phase 4.6) — fixed once and never re-derived, so a later
// change to the Tutor's own HourlyRate never retroactively changes what an
// already-booked Session cost. Deliberately captures the rate that applied,
// not a Duration-prorated total: proration would require inventing a
// rounding rule for a case Duration/HourlyRate combinations could produce a
// result not evenly divisible by 10 (see ADR-019 Addendum 1's "don't invent
// a rounding rule" precedent) — a new business rule this phase is not
// authorized to decide, not a bug fix. See
// docs/phases/PHASE-046-REPORT.md Section 4 for the full reasoning.
//
// A distinct value object from HourlyRate, not a cross-context reference to
// it: Session belongs to Scheduling & Booking, HourlyRate to Identity &
// Relationship (ADR-002 Context Ownership) — each stays owned by its own
// context. Both validate through the same shared rule (Domain.Common.RialAmount)
// rather than either duplicating the other's copy of it.
public sealed class SessionPrice : ValueObject
{
    public decimal Amount { get; }

    public string Currency => RialAmount.CurrencyCode;

    private SessionPrice(decimal amount) => Amount = amount;

    public static SessionPrice Of(decimal amount) => new(RialAmount.Validate(amount, nameof(amount)));

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
