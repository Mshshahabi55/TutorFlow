using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TutorFlow.Modules.Availability.Domain;

namespace TutorFlow.Modules.Availability.Persistence;

internal sealed class BookingSettingsConfiguration : IEntityTypeConfiguration<BookingSettings>
{
    public void Configure(EntityTypeBuilder<BookingSettings> builder)
    {
        builder.HasKey(s => s.TutorUserId);
        builder.Property(s => s.TimeZoneId).HasMaxLength(100);
    }
}
