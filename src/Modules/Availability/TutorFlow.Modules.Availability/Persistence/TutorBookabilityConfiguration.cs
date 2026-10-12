using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TutorFlow.Modules.Availability.Domain;

namespace TutorFlow.Modules.Availability.Persistence;

internal sealed class TutorBookabilityConfiguration : IEntityTypeConfiguration<TutorBookability>
{
    public void Configure(EntityTypeBuilder<TutorBookability> builder)
    {
        builder.HasKey(b => b.TutorUserId);
    }
}
