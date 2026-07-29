using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TutorFlow.Domain.Meetings;
using TutorFlow.Domain.Meetings.ValueObjects;
using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Infrastructure.Persistence.Configurations;

internal sealed class MeetingConfiguration : IEntityTypeConfiguration<Meeting>
{
    public void Configure(EntityTypeBuilder<Meeting> builder)
    {
        builder.ToTable("Meetings");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id)
            .HasConversion(new ValueConverter<MeetingId, Guid>(id => id.Value, value => MeetingId.From(value)))
            .ValueGeneratedNever();

        // Plain column, no FK — SessionId crosses into Scheduling & Booking,
        // the same cross-context reference pattern Session.TutorId/StudentId
        // and Conversation.ParticipantAId/BId already establish (ADR-002).
        builder.Property(m => m.SessionId)
            .HasConversion(new ValueConverter<SessionId, Guid>(id => id.Value, value => SessionId.From(value)));

        // One Meeting per Session (docs/adr/ADR-023-...) — enforced at the
        // storage layer, the same unique-constraint-based concurrency
        // pattern ADR-014 already established, rather than trusting the
        // Application layer's own idempotent-return check alone under a
        // race between two simultaneous "Start Lesson" requests.
        builder.HasIndex(m => m.SessionId).IsUnique();

        builder.Property(m => m.Provider).HasConversion<int>();

        builder.Property(m => m.ProviderMeetingId).IsRequired().HasMaxLength(200);
        builder.Property(m => m.JoinUrl).IsRequired().HasMaxLength(2000);
        builder.Property(m => m.HostUrl).HasMaxLength(2000);

        builder.Property(m => m.StartsAtUtc);
        builder.Property(m => m.EndsAtUtc);

        builder.Property(m => m.Status).HasConversion<int>();

        builder.Property(m => m.CreatedAtUtc);
        builder.Property(m => m.UpdatedAtUtc);

        builder.Ignore(m => m.DomainEvents);
    }
}
