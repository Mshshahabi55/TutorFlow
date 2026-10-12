namespace TutorFlow.Modules.Bookings.Infrastructure;

// Thrown when the PostgreSQL exclusion constraint (ADR-0013) rejects a hold-creation or
// reschedule INSERT with a 23P01 exclusion_violation — mapped to 409 Conflict at the endpoint,
// mirroring Availability's WeeklyRulesConflictException convention exactly.
public sealed class SlotNotAvailableException(string message) : Exception(message);
