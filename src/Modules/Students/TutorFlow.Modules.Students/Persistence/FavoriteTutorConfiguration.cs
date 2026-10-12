using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TutorFlow.Modules.Students.Domain;

namespace TutorFlow.Modules.Students.Persistence;

internal sealed class FavoriteTutorConfiguration : IEntityTypeConfiguration<FavoriteTutor>
{
    public void Configure(EntityTypeBuilder<FavoriteTutor> builder)
    {
        builder.HasKey(f => f.Id);

        builder.HasIndex(f => new { f.StudentUserId, f.TutorUserId }).IsUnique();
        builder.HasIndex(f => f.StudentUserId).HasDatabaseName("IX_FavoriteTutors_StudentUserId");
    }
}
