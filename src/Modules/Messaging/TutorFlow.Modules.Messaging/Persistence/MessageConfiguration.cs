using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TutorFlow.Modules.Messaging.Domain;

namespace TutorFlow.Modules.Messaging.Persistence;

internal sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Body).HasMaxLength(5000).IsRequired();

        // PostgreSQL-generated IDENTITY column (ADR-0030) — Postgres' own internal sequence
        // assigns this value atomically on INSERT, immune to the classic concurrent-MAX()+1 race
        // an application-computed counter would suffer. Global across all conversations (gaps
        // between a single conversation's own values are expected and harmless); the ordering
        // guarantee this module actually needs — "WHERE ConversationId = x ORDER BY
        // SequenceNumber" is deterministic — holds regardless of global gaps.
        builder.Property(m => m.SequenceNumber).UseIdentityColumn();

        // GetMessages' cursor pagination: "all messages in this conversation before cursor X,
        // newest-first" — this composite index serves exactly that predicate + order directly.
        builder.HasIndex(m => new { m.ConversationId, m.SequenceNumber })
            .HasDatabaseName("IX_Messages_ConversationId_SequenceNumber");
    }
}
