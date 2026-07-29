using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TutorFlow.Domain.Communication;
using TutorFlow.Domain.Communication.ValueObjects;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Infrastructure.Persistence.Configurations;

internal sealed class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("Conversations");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasConversion(new ValueConverter<ConversationId, Guid>(id => id.Value, value => ConversationId.From(value)))
            .ValueGeneratedNever();

        // Plain columns, no FK — ParticipantAId/ParticipantBId cross into
        // Identity & Relationship, the same pattern Session.TutorId/StudentId
        // already establish for a cross-context Account reference
        // (ADR-002's context-isolation rule).
        builder.Property(c => c.ParticipantAId)
            .HasConversion(new ValueConverter<AccountId, Guid>(id => id.Value, value => AccountId.From(value)));

        builder.Property(c => c.ParticipantBId)
            .HasConversion(new ValueConverter<AccountId, Guid>(id => id.Value, value => AccountId.From(value)));

        // Lookup-query support for IConversationRepository.GetByParticipantAsync
        // (a Conversation can be found via either side). No unique constraint
        // on the pair: participants have no canonical ordering, so a race
        // between two simultaneous "start conversation" requests between the
        // same two accounts could in principle create two rows — an accepted,
        // low-consequence risk (recoverable data-quality issue, not a
        // correctness/security concern), not backed by a storage-layer
        // constraint the way CONST-1 booking is.
        builder.HasIndex(c => c.ParticipantAId);
        builder.HasIndex(c => c.ParticipantBId);

        builder.Property(c => c.CreatedAtUtc);
        builder.Property(c => c.LastMessageAtUtc);

        builder.Ignore(c => c.DomainEvents);
    }
}
