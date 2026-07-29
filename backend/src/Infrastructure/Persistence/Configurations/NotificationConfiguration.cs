using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TutorFlow.Domain.Communication;
using TutorFlow.Domain.Communication.ValueObjects;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Infrastructure.Persistence.Configurations;

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id)
            .HasConversion(new ValueConverter<NotificationId, Guid>(id => id.Value, value => NotificationId.From(value)))
            .ValueGeneratedNever();

        // Plain column, no FK — crosses into Identity & Relationship, same
        // pattern as Session.TutorId/StudentId (ADR-002's context-isolation
        // rule).
        builder.Property(n => n.RecipientId)
            .HasConversion(new ValueConverter<AccountId, Guid>(id => id.Value, value => AccountId.From(value)));

        builder.Property(n => n.Type);

        builder.Property(n => n.Summary).HasMaxLength(Notification.MaxSummaryLength);

        // Whatever the notification concerns (a SessionId, a ConversationId)
        // — deliberately untyped/unconverted and un-FK'd: it may point at an
        // aggregate in any context, or none, and exists for deep-linking
        // only, no invariant of its own.
        builder.Property(n => n.RelatedEntityId);

        builder.Property(n => n.CreatedAtUtc);
        builder.Property(n => n.ReadAtUtc);

        // Lookup-query support for GetByRecipientAsync/CountUnreadByRecipientAsync.
        builder.HasIndex(n => new { n.RecipientId, n.ReadAtUtc });

        builder.Ignore(n => n.DomainEvents);
    }
}
