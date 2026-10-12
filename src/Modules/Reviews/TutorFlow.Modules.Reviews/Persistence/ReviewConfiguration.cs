using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TutorFlow.Modules.Reviews.Domain;

namespace TutorFlow.Modules.Reviews.Persistence;

internal sealed class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Comment).HasMaxLength(2000);

        // The real structural invariant (Product Owner decision): one review per Student<->Tutor
        // relationship. DB-enforced, not just application logic — the same discipline as every
        // other uniqueness invariant in this codebase (e.g. Lessons' one-row-per-BookingId).
        builder.HasIndex(r => new { r.StudentUserId, r.TutorUserId }).IsUnique().HasDatabaseName("IX_Reviews_StudentUserId_TutorUserId");

        // The public summary query (GetTutorReviewSummary) filters by TutorUserId alone.
        builder.HasIndex(r => r.TutorUserId).HasDatabaseName("IX_Reviews_TutorUserId");
    }
}
