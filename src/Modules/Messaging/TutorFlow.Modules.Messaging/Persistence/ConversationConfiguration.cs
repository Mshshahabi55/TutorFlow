using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TutorFlow.Modules.Messaging.Domain;

namespace TutorFlow.Modules.Messaging.Persistence;

internal sealed class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.HasKey(c => c.Id);

        // The real structural invariant (ADR-0030): one conversation per Student<->Tutor
        // relationship — DB-enforced, not just application logic, the same discipline as
        // Reviews' identical (StudentUserId, TutorUserId) uniqueness.
        builder.HasIndex(c => new { c.StudentUserId, c.TutorUserId })
            .IsUnique()
            .HasDatabaseName("IX_Conversations_StudentUserId_TutorUserId");

        // ListConversations filters by "am I the tutor" as a separate query from "am I the
        // student" — the composite unique index above already serves student-side lookups via
        // its leftmost column, so only the tutor side needs its own index.
        builder.HasIndex(c => c.TutorUserId).HasDatabaseName("IX_Conversations_TutorUserId");
    }
}
