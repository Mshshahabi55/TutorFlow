using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TutorFlow.Modules.Subjects.Domain;

namespace TutorFlow.Modules.Subjects.Persistence;

internal sealed class SpecialtyConfiguration : IEntityTypeConfiguration<Specialty>
{
    public void Configure(EntityTypeBuilder<Specialty> builder)
    {
        builder.HasKey(s => s.Id);
        builder.HasIndex(s => s.SubjectId);
        builder.HasIndex(s => new { s.SubjectId, s.Code }).IsUnique();
        builder.Property(s => s.Code).HasMaxLength(100);
        builder.Property(s => s.DisplayName).HasMaxLength(200);

        // Real FK — both Subject and Specialty live in Subjects' own schema, so this is an
        // intra-module relationship, not the cross-module FK CLAUDE.md forbids.
        builder.HasOne<Subject>().WithMany().HasForeignKey(s => s.SubjectId).OnDelete(DeleteBehavior.Restrict);

        builder.HasData(
            Specialty.Seed(SeedIds.EnglishConversation, SeedIds.English, "conversation", "Conversation practice", 1),
            Specialty.Seed(SeedIds.EnglishExamPrep, SeedIds.English, "exam-prep", "Exam preparation", 2),
            Specialty.Seed(SeedIds.EnglishBusiness, SeedIds.English, "business", "Business English", 3),
            Specialty.Seed(SeedIds.EnglishKidsAndTeens, SeedIds.English, "kids-teens", "Kids & teens", 4),
            Specialty.Seed(SeedIds.SpanishConversation, SeedIds.Spanish, "conversation", "Conversation practice", 1),
            Specialty.Seed(SeedIds.MathExamPrep, SeedIds.Mathematics, "exam-prep", "Exam preparation", 1));
    }
}
