namespace TutorFlow.Modules.Bookings.Domain;

// docs/domain/BOOKINGS_SPEC.md §2 is the single authoritative source for which transitions
// between these are actually valid — this enum only lists the schema-supported values.
public enum BookingStatus
{
    Held,
    PendingPayment,
    Confirmed,
    Rescheduled,
    InProgress,
    Completed,
    Cancelled,
    NoShow,
    Expired,
    Refunded
}
