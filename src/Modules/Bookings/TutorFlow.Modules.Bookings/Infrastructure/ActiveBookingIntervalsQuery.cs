using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Bookings.Domain;
using TutorFlow.Modules.Bookings.Persistence;
using TutorFlow.Shared;

namespace TutorFlow.Modules.Bookings.Infrastructure;

// ADR-0014 correction: the reciprocal query contract Availability's public browse-slots
// endpoint depends on via DI, with no project reference between the two modules. Returns plain
// (StartUtc, EndUtc) pairs only — never BookingId, StudentUserId, or Status; Availability only
// needs "this window is occupied," never who occupies it.
// Public (not internal, the usual convention) — Availability's AvailabilitySignalPublisher
// (ADR-0024) now reaches this transitively through a Wolverine message handler chain, and
// Wolverine's codegen generates direct construction code for its dependencies; an internal
// concrete type can't be constructed from that generated code, which forces an unwanted
// service-location fallback (disallowed by this app's strict default policy).
public sealed class ActiveBookingIntervalsQuery(BookingsDbContext db) : IActiveBookingIntervalsQuery
{
    public async Task<IReadOnlyList<BookingInterval>> GetActiveIntervalsAsync(Guid tutorUserId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken ct)
    {
        return await db.Bookings
            .AsNoTracking()
            .Where(b => b.TutorUserId == tutorUserId
                && Booking.ActiveStatuses.Contains(b.Status)
                && b.StartUtc < toUtc && b.EndUtc > fromUtc)
            .Select(b => new BookingInterval(b.StartUtc, b.EndUtc))
            .ToListAsync(ct);
    }
}
