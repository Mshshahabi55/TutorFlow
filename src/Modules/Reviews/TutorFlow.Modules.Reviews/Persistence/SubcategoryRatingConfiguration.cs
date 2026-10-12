using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TutorFlow.Modules.Reviews.Domain;

namespace TutorFlow.Modules.Reviews.Persistence;

internal sealed class SubcategoryRatingConfiguration : IEntityTypeConfiguration<SubcategoryRating>
{
    public void Configure(EntityTypeBuilder<SubcategoryRating> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.SubmissionMonthKey).HasMaxLength(7).IsRequired();

        // The real structural invariant for the documented "once per month" cap — DB-enforced,
        // same discipline as every other uniqueness invariant in this codebase.
        builder.HasIndex(r => new { r.StudentUserId, r.TutorUserId, r.SubmissionMonthKey })
            .IsUnique()
            .HasDatabaseName("IX_SubcategoryRatings_StudentUserId_TutorUserId_SubmissionMonthKey");

        // The public aggregate-average query (GetTutorReviewSummary) filters by TutorUserId alone.
        builder.HasIndex(r => r.TutorUserId).HasDatabaseName("IX_SubcategoryRatings_TutorUserId");
    }
}
