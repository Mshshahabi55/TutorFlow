using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Bookings.Domain;
using Wolverine.EntityFrameworkCore;

namespace TutorFlow.Modules.Bookings.Persistence;

public sealed class BookingsDbContext(DbContextOptions<BookingsDbContext> options) : DbContext(options)
{
    public DbSet<Booking> Bookings => Set<Booking>();

    public DbSet<BookingHoldExpiryIntent> BookingHoldExpiryIntents => Set<BookingHoldExpiryIntent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("bookings");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookingsDbContext).Assembly);

        // A44: Bookings' own durable outbox/inbox tables, in its own schema — registered as an
        // Ancillary Wolverine message store (Program.cs) so ConfirmBooking/
        // BookingHoldExpirySweepService's business writes and outgoing envelopes commit
        // atomically.
        modelBuilder.MapWolverineEnvelopeStorage();
    }
}
