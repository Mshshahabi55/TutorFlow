using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TutorFlow.Modules.Bookings.Domain;

namespace TutorFlow.Modules.Bookings.Persistence;

internal sealed class BookingHoldExpiryIntentConfiguration : IEntityTypeConfiguration<BookingHoldExpiryIntent>
{
    public void Configure(EntityTypeBuilder<BookingHoldExpiryIntent> builder)
    {
        builder.HasKey(i => i.Id);

        // Defense-in-depth, not load-bearing: BookingWrites.ExpireSingleHoldIfStaleAsync's own
        // conditional UPDATE already guarantees a booking can only be claimed once (its WHERE
        // clause excludes anything no longer Held/PendingPayment), so a second intent for the
        // same booking should never be attempted at all.
        builder.HasIndex(i => i.BookingId).IsUnique().HasDatabaseName("IX_BookingHoldExpiryIntents_BookingId");

        // The dispatch phase's own query target — a partial index keeps it cheap regardless of
        // how large this table grows, since dispatched rows never need to be found again.
        builder.HasIndex(i => i.DispatchedAtUtc)
            .HasDatabaseName("IX_BookingHoldExpiryIntents_Pending")
            .HasFilter("\"DispatchedAtUtc\" IS NULL");
    }
}
