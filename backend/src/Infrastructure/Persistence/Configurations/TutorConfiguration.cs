using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Infrastructure.Persistence.Configurations;

internal sealed class TutorConfiguration : IEntityTypeConfiguration<Tutor>
{
    public void Configure(EntityTypeBuilder<Tutor> builder)
    {
        builder.ToTable("Tutors");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .HasConversion(new ValueConverter<AccountId, Guid>(id => id.Value, value => AccountId.From(value)))
            .ValueGeneratedNever();

        builder.Property(t => t.Email)
            .HasConversion(new ValueConverter<EmailAddress, string>(email => email.Value, value => EmailAddress.Of(value)))
            .IsRequired();
        builder.HasIndex(t => t.Email).IsUnique();

        builder.Property(t => t.PasswordHash)
            .HasConversion(new ValueConverter<PasswordHash, string>(hash => hash.Value, value => PasswordHash.Of(value)))
            .IsRequired();

        builder.Property(t => t.FailedLoginAttemptCount);
        builder.Property(t => t.LockedUntilUtc);

        builder.Property(t => t.IsApproved);
        builder.Property(t => t.IsSuspended);
        builder.Ignore(t => t.IsDiscoverable);

        // ADR-019: Rial has no minor unit — scale 0, not the old scale-2
        // (numeric(10,2), a leftover from before currency/precision were
        // decided). Precision 12 matches HourlyRate.MaxAmount's ceiling
        // exactly (twelve nines).
        builder.Property(t => t.HourlyRate)
            .HasConversion(new ValueConverter<HourlyRate?, decimal?>(
                rate => rate == null ? null : rate.Amount,
                amount => amount == null ? null : HourlyRate.Of(amount.Value)))
            .HasPrecision(12, 0);

        builder.Property(t => t.Subject)
            .HasConversion(new ValueConverter<Subject?, string?>(
                subject => subject == null ? null : subject.Value,
                value => value == null ? null : Subject.Of(value)));

        builder.Property(t => t.Language)
            .HasConversion(new ValueConverter<Language?, string?>(
                language => language == null ? null : language.Value,
                value => value == null ? null : Language.Of(value)));

        builder.Property(t => t.Location)
            .HasConversion(new ValueConverter<Location?, string?>(
                location => location == null ? null : location.Value,
                value => value == null ? null : Location.Of(value)));

        // OfferedDurations exposes only a getter over a private backing field
        // (no public setter) — field access is required for materialization.
        builder.Property<List<TimeSpan>>("_offeredDurations")
            .HasField("_offeredDurations")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("OfferedDurations")
            .HasConversion(
                new ValueConverter<List<TimeSpan>, string>(
                    durations => string.Join(',', durations.Select(d => d.Ticks)),
                    value => string.IsNullOrEmpty(value)
                        ? new List<TimeSpan>()
                        : value.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(t => new TimeSpan(long.Parse(t))).ToList()),
                new ValueComparer<List<TimeSpan>>(
                    (a, b) => a!.SequenceEqual(b!),
                    durations => durations.Aggregate(0, (hash, d) => HashCode.Combine(hash, d)),
                    durations => durations.ToList()));

        builder.Ignore(t => t.OfferedDurations);
        builder.Ignore(t => t.DomainEvents);
    }
}
