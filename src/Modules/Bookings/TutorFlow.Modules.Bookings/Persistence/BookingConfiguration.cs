using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TutorFlow.Modules.Bookings.Domain;

namespace TutorFlow.Modules.Bookings.Persistence;

internal sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(b => b.PriceCurrency).HasMaxLength(3).IsRequired();
        builder.Property(b => b.PriceAmountMinorUnits).IsRequired();
        builder.Property(b => b.CancellationReason).HasMaxLength(500);
        builder.Property(b => b.PaymentReferenceId).HasMaxLength(200);
        builder.Property(b => b.IsTrial).HasDefaultValue(false);

        // The GiST exclusion constraint (tutor_user_id, during) — including its generated
        // `During` tstzrange column and the btree_gist extension it requires — is added via raw
        // SQL in the initial migration (ADR-0013 §1). EF Core has no first-class mapping for
        // either PostgreSQL feature; this configuration intentionally does not reference them.
        builder.HasIndex(b => b.StudentUserId).HasDatabaseName("IX_Bookings_StudentUserId");
        builder.HasIndex(b => b.TutorUserId).HasDatabaseName("IX_Bookings_TutorUserId");
    }
}
