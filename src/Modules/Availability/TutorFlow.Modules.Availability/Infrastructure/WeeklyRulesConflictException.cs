namespace TutorFlow.Modules.Availability.Infrastructure;

// Thrown when a concurrent PUT /me/weekly-rules replace operation is detected by PostgreSQL's
// serializable isolation (write-skew/phantom-insert conflict). Mapped to 409 Conflict at the
// endpoint so the caller gets a clear signal to retry, instead of one request's rules silently
// disappearing (last-write-wins).
public sealed class WeeklyRulesConflictException(string message) : Exception(message);
