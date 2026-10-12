namespace TutorFlow.Shared;

public interface IActiveBookingIntervalsQuery
{
    Task<IReadOnlyList<BookingInterval>> GetActiveIntervalsAsync(Guid tutorUserId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken ct);
}

public sealed record BookingInterval(DateTimeOffset StartUtc, DateTimeOffset EndUtc);
