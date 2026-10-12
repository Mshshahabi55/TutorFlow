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

        builder.Property(e => e.SkippedReason).HasMaxLength(500);
    }
}
