using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TutorFlow.Modules.Subjects.Domain;

namespace TutorFlow.Modules.Subjects.Persistence;

internal sealed class SubjectConfiguration : IEntityTypeConfiguration<Subject>
{
    public void Configure(EntityTypeBuilder<Subject> builder)
    {
        builder.HasKey(s => s.Id);
        builder.HasIndex(s => s.Code).IsUnique();
        builder.Property(s => s.Code).HasMaxLength(100);
        builder.Property(s => s.DisplayName).HasMaxLength(200);

        builder.HasData(
            Subject.Seed(SeedIds.English, "english", "English", 1),
            Subject.Seed(SeedIds.Spanish, "spanish", "Spanish", 2),
            Subject.Seed(SeedIds.French, "french", "French", 3),
            Subject.Seed(SeedIds.German, "german", "German", 4),
            Subject.Seed(SeedIds.Mathematics, "mathematics", "Mathematics", 5));
    }
}
