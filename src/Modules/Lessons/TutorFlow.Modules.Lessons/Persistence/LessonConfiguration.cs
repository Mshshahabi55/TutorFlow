using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TutorFlow.Modules.Lessons.Domain;

namespace TutorFlow.Modules.Lessons.Persistence;

internal sealed class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        // One Lesson ever per Booking (ADR-0016 §3) — the DB enforces the dedup guarantee that
        // makes at-least-once BookingConfirmed delivery safe, not just application logic.
        builder.HasIndex(l => l.BookingId).IsUnique().HasDatabaseName("IX_Lessons_BookingId");
        builder.HasIndex(l => l.StudentUserId).HasDatabaseName("IX_Lessons_StudentUserId");
        builder.HasIndex(l => l.TutorUserId).HasDatabaseName("IX_Lessons_TutorUserId");
    }
}
