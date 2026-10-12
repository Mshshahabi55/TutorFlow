namespace TutorFlow.Modules.Lessons.Domain;

// Accepted skeleton (docs/domain/STATE_MACHINES.md "Lesson state machine"): Scheduled ->
// InProgress -> Completed, with Scheduled/InProgress -> Cancelled | NoShow. The full enum is
// defined now (schema-supported) so a future migration never needs to add a value — but, same
// as Booking's own InProgress/Completed/NoShow (BOOKINGS_SPEC.md §2 "Clarification on scope"),
// only Scheduled is ever written by this implementation slice; who/what drives every other
// transition is still [OPEN — Product Owner] and is not implemented here.
public enum LessonStatus
{
    Scheduled,
    InProgress,
    Completed,
    Cancelled,
    NoShow
}
