using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TutorFlow.Domain.Communication;
using TutorFlow.Domain.Communication.ValueObjects;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Infrastructure.Persistence.Configurations;

internal sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("Messages");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id)
            .HasConversion(new ValueConverter<MessageId, Guid>(id => id.Value, value => MessageId.From(value)))
            .ValueGeneratedNever();

        builder.Property(m => m.ConversationId)
            .HasConversion(new ValueConverter<ConversationId, Guid>(id => id.Value, value => ConversationId.From(value)));

        // Same-context real FK — Message and Conversation are both owned by
        // Communication (mirrors Session -> AvailabilitySlot's real-FK
        // treatment for the same reason: both aggregates share one bounded
        // context). Restrict, not Cascade — deleting a Conversation is not a
        // capability this ADR builds.
        builder.HasOne<Conversation>()
            .WithMany()
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Plain columns, no FK — cross into Identity & Relationship, same
        // pattern as Session.TutorId/StudentId (ADR-002's context-isolation
        // rule).
        builder.Property(m => m.SenderId)
            .HasConversion(new ValueConverter<AccountId, Guid>(id => id.Value, value => AccountId.From(value)));

        builder.Property(m => m.RecipientId)
            .HasConversion(new ValueConverter<AccountId, Guid>(id => id.Value, value => AccountId.From(value)));

        builder.Property(m => m.Body).HasMaxLength(Message.MaxBodyLength);

        builder.Property(m => m.SentAtUtc);
        builder.Property(m => m.ReadAtUtc);

        // Lookup-query support for GetByConversationIdAsync/GetLastByConversationIdAsync
        // and CountUnreadAsync respectively.
        builder.HasIndex(m => m.ConversationId);
        builder.HasIndex(m => new { m.RecipientId, m.ReadAtUtc });

        builder.Ignore(m => m.DomainEvents);
    }
}
