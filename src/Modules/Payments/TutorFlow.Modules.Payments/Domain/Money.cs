namespace TutorFlow.Modules.Payments.Domain;

// Module-local value object mirroring Bookings.Domain.Money/Tutors.Domain.Money exactly — not
// promoted to TutorFlow.Shared; A13 remains explicitly deferred (docs/domain/DOMAIN_MODEL.md).
public sealed record Money(int AmountMinorUnits, string Currency);
