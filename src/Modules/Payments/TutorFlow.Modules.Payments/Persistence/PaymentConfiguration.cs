using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TutorFlow.Modules.Payments.Domain;

namespace TutorFlow.Modules.Payments.Persistence;

internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Currency).HasMaxLength(3).IsRequired();
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(p => p.IdempotencyKey).HasMaxLength(200).IsRequired();
        builder.Property(p => p.StripePaymentIntentId).HasMaxLength(200);
        builder.Property(p => p.FailureReason).HasMaxLength(500);

        // One Payment ever per Booking — never reused, never retried in place
        // (PAYMENTS_SPEC.md §3).
        builder.HasIndex(p => p.BookingId).IsUnique().HasDatabaseName("IX_Payments_BookingId");
        builder.HasIndex(p => p.StripePaymentIntentId).IsUnique().HasDatabaseName("IX_Payments_StripePaymentIntentId");
        builder.HasIndex(p => p.StudentUserId).HasDatabaseName("IX_Payments_StudentUserId");
        builder.HasIndex(p => p.TutorUserId).HasDatabaseName("IX_Payments_TutorUserId");
    }
}
