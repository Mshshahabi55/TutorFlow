using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TutorFlow.Modules.Payments.Domain;

namespace TutorFlow.Modules.Payments.Persistence;

internal sealed class PaymentWebhookEventConfiguration : IEntityTypeConfiguration<PaymentWebhookEvent>
{
    public void Configure(EntityTypeBuilder<PaymentWebhookEvent> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.StripeEventId).HasMaxLength(200).IsRequired();
        builder.Property(e => e.EventType).HasMaxLength(200).IsRequired();
        builder.Property(e => e.RawPayloadJson).IsRequired();

        builder.HasIndex(e => e.StripeEventId).IsUnique().HasDatabaseName("IX_PaymentWebhookEvents_StripeEventId");
        builder.HasIndex(e => e.PaymentId).HasDatabaseName("IX_PaymentWebhookEvents_PaymentId");
    }
}
