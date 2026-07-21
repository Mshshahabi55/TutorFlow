using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TutorFlow.Domain.Scheduling;
using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Infrastructure.Persistence.Configurations;

internal sealed class AvailabilitySlotConfiguration : IEntityTypeConfiguration<AvailabilitySlot>
{
    public void Configure(EntityTypeBuilder<AvailabilitySlot> builder)
    {
        builder.ToTable("AvailabilitySlots");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .HasConversion(new ValueConverter<AvailabilitySlotId, Guid>(id => id.Value, value => AvailabilitySlotId.From(value)))
            .ValueGeneratedNever();

        builder.Property(s => s.TutorId)
            .HasConversion(new ValueConverter<TutorId, Guid>(id => id.Value, value => TutorId.From(value)));

        builder.Property(s => s.StartTimeUtc);

        builder.Property(s => s.Duration)
            .HasConversion(new ValueConverter<SessionDuration, TimeSpan>(d => d.Value, value => SessionDuration.Of(value)));

        builder.Ignore(s => s.EndTimeUtc);

        builder.Property(s => s.DeliveryMode);

        builder.Property(s => s.IsConsumed);

        builder.Ignore(s => s.DomainEvents);
    }
}
