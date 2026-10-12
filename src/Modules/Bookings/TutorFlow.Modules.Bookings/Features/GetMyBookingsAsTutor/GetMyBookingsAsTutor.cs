using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Bookings.Domain;
using TutorFlow.Modules.Bookings.Persistence;

namespace TutorFlow.Modules.Bookings.Features.GetMyBookingsAsTutor;

public static class GetMyBookingsAsTutor
{
    public sealed record Summary(Guid Id, Guid StudentUserId, BookingStatus Status, DateTimeOffset StartUtc, DateTimeOffset EndUtc, int DurationMinutes);

    public sealed record Response(IReadOnlyList<Summary> Bookings);

    internal sealed class Handler(BookingsDbContext db)
    {
        public async Task<Response> HandleAsync(Guid tutorUserId, CancellationToken ct)
        {
            var bookings = await db.Bookings
                .AsNoTracking()
                .Where(b => b.TutorUserId == tutorUserId)
                .OrderByDescending(b => b.StartUtc)
                .Select(b => new Summary(b.Id, b.StudentUserId, b.Status, b.StartUtc, b.EndUtc, b.DurationMinutes))
                .ToListAsync(ct);

            return new Response(bookings);
        }
    }
}
