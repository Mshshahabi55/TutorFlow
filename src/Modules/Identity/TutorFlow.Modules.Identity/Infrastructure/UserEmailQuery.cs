using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Identity.Persistence;
using TutorFlow.Shared;

namespace TutorFlow.Modules.Identity.Infrastructure;

// ADR-0014/ADR-0029: Identity implements the Shared query contract Notifications depends on via
// DI. No project reference to TutorFlow.Modules.Notifications is added by this registration.
// Public (not the usual internal convention) — Notifications' BookingHoldExpiredConsumer
// reaches this transitively through a Wolverine message handler chain, and Wolverine's codegen
// generates direct construction code for a consumer's dependencies; an internal concrete type
// can't be constructed from that generated code (same reason ActiveBookingIntervalsQuery was
// made public in Bookings).
public sealed class UserEmailQuery(IdentityDbContext db) : IUserEmailQuery
{
    public async Task<string?> GetEmailAsync(Guid userId, CancellationToken ct)
    {
        var email = await db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.Email).FirstOrDefaultAsync(ct);
        return string.IsNullOrWhiteSpace(email) ? null : email;
    }
}
