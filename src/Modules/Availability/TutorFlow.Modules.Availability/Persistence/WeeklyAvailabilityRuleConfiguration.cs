using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TutorFlow.Modules.Availability.Domain;

namespace TutorFlow.Modules.Availability.Persistence;

internal sealed class WeeklyAvailabilityRuleConfiguration : IEntityTypeConfiguration<WeeklyAvailabilityRule>
{
    public void Configure(EntityTypeBuilder<WeeklyAvailabilityRule> builder)
    {
        builder.HasKey(r => r.Id);
        builder.HasIndex(r => r.TutorUserId);
        builder.Property(r => r.DayOfWeek).HasConversion<string>().HasMaxLength(20);
    }
}
