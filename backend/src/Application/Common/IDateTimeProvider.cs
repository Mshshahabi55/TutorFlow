namespace TutorFlow.Application.Common;

// Every mutating action must carry a timestamp (docs/adr/ADR-003-authentication-
// and-authorization.md: CONST-2). Time is obtained through this abstraction so
// it is never read from the system clock directly inside Application or Domain.
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
