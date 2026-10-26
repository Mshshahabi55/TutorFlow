using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TutorFlow.Modules.Notifications.Domain;

namespace TutorFlow.Modules.Notifications.Persistence;

internal sealed class SentAbandonedCheckoutEmailConfiguration : IEntityTypeConfiguration<SentAbandonedCheckoutEmail>
{
    public void Configure(EntityTypeBuilder<SentAbandonedCheckoutEmail> builder)
    {
        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.BookingId).IsUnique();

        // Supports AbandonedCheckoutClaimReconciliationService's bounded scan for stale,
        // unresolved claims — a partial index so resolved rows (the overwhelming majority over
        // time) never need to be scanned.
        builder.HasIndex(e => e.OwnedUntilUtc)
            .HasFilter("\"WasSent\" = false AND \"SkippedReason\" IS NULL");

        builder.Property(e => e.SkippedReason).HasMaxLength(500);
    }
}
