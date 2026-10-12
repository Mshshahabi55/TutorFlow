using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TutorFlow.Modules.Availability.Domain;

namespace TutorFlow.Modules.Availability.Persistence;

internal sealed class TimeOffConfiguration : IEntityTypeConfiguration<TimeOff>
{
    public void Configure(EntityTypeBuilder<TimeOff> builder)
    {
        builder.HasKey(t => t.Id);
        builder.HasIndex(t => t.TutorUserId);
        builder.Property(t => t.Reason).HasMaxLength(500);
    }
}
