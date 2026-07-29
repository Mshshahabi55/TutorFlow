using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Application.Scheduling.DTOs;

// Explicit named fields, not a Dictionary<SessionStatus, int> — every
// enum in this API already serializes as its own underlying int
// (Program.cs's own convention), so a dictionary keyed by SessionStatus
// would serialize as an object with numeric-string keys ("0", "1", ...),
// which no other DTO in this API does and a frontend caller would have to
// know to parse. A status absent from the source data (zero Sessions in
// that status) still gets an explicit 0 here, never an omitted key.
public sealed record SessionStatusCountsDto(int Scheduled, int Completed, int Cancelled, int NoShow)
{
    public static SessionStatusCountsDto FromCounts(IReadOnlyDictionary<SessionStatus, int> counts) => new(
        counts.GetValueOrDefault(SessionStatus.Scheduled),
        counts.GetValueOrDefault(SessionStatus.Completed),
        counts.GetValueOrDefault(SessionStatus.Cancelled),
        counts.GetValueOrDefault(SessionStatus.NoShow));
}
