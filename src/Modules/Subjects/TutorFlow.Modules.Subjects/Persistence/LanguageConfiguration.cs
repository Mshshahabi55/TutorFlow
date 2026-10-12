using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TutorFlow.Modules.Subjects.Domain;

namespace TutorFlow.Modules.Subjects.Persistence;

internal sealed class LanguageConfiguration : IEntityTypeConfiguration<Language>
{
    public void Configure(EntityTypeBuilder<Language> builder)
    {
        builder.HasKey(l => l.Code);
        builder.Property(l => l.Code).HasMaxLength(2);
        builder.Property(l => l.DisplayName).HasMaxLength(200);

        builder.HasData(
            Language.Create("en", "English", 1),
            Language.Create("es", "Spanish", 2),
            Language.Create("fr", "French", 3),
            Language.Create("de", "German", 4),
            Language.Create("pt", "Portuguese", 5),
            Language.Create("it", "Italian", 6));
    }
}
