using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Infrastructure.Persistence.Configurations;

internal sealed class TutorConfiguration : IEntityTypeConfiguration<Tutor>
{
    // 20 entries (TutorProfileLimits.MaxCollectionEntries) of up to 200
    // characters (TutorProfileLimits.MaxSubjectEntryFieldLength/
    // Tutor.ShortFieldMaxLength) each, JSON-serialized, with generous
    // margin for quoting/escaping overhead.
    private const int CollectionColumnMaxLength = 5000;
    private const int TutorSubjectsColumnMaxLength = 10000;

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
        // decided). Precision 12 comfortably contains HourlyRate.MaxAmount
        // (999,999,999,990 — see HourlyRate.cs for why it's one Rial short
        // of numeric(12,0)'s own twelve-nines ceiling).
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

        // ADR-024 (Accepted, 2026-07-28) — Tutor Profile Enrichment. Plain
        // string/int/bool properties need no ValueConverter (EF maps them
        // natively); ProfileStatus is stored as its enum name for readable
        // ad hoc DB inspection, matching no existing precedent one way or
        // the other but chosen for the same reason table/column names
        // throughout this schema are already human-readable.
        builder.Property(t => t.ProfileStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(t => t.DisplayName).HasMaxLength(200);
        builder.Property(t => t.Headline).HasMaxLength(200);
        builder.Property(t => t.Biography).HasMaxLength(4000);
        builder.Property(t => t.Country).HasMaxLength(200);
        builder.Property(t => t.City).HasMaxLength(200);
        builder.Property(t => t.YearsOfExperience);
        builder.Property(t => t.Education).HasMaxLength(4000);
        builder.Property(t => t.Certifications).HasMaxLength(4000);
        builder.Property(t => t.TeachingMethodology).HasMaxLength(4000);
        builder.Property(t => t.PhotoUrl).HasMaxLength(200);
        builder.Property(t => t.IntroVideoUrl).HasMaxLength(200);
        builder.Property(t => t.TrialLessonAvailable);

        builder.Property(t => t.TrialLessonPrice)
            .HasConversion(new ValueConverter<HourlyRate?, decimal?>(
                rate => rate == null ? null : rate.Amount,
                amount => amount == null ? null : HourlyRate.Of(amount.Value)))
            .HasPrecision(12, 0);

        // String-list fields — same private-backing-field materialization
        // pattern as _offeredDurations above, JSON-serialized rather than
        // delimiter-joined since free text (unlike a TimeSpan's ticks) may
        // itself contain any character, including a delimiter. The
        // HasMaxLength below is a persistence-level backstop only (ADR-007
        // "Relationship to Persistence") — Tutor.EnforceMaxCount/
        // ShortFieldMaxLength are the real, authoritative invariant; this
        // column length is sized generously above what TutorProfileLimits.
        // MaxCollectionEntries (20) entries of up to 200 characters each,
        // JSON-serialized, could ever produce.
        ConfigureStringList(builder, "_otherLanguages", "OtherLanguages", CollectionColumnMaxLength);
        builder.Ignore(t => t.OtherLanguages);

        ConfigureStringList(builder, "_lessonSpecialties", "LessonSpecialties", CollectionColumnMaxLength);
        builder.Ignore(t => t.LessonSpecialties);

        ConfigureStringList(builder, "_galleryImageUrls", "GalleryImageUrls", CollectionColumnMaxLength);
        builder.Ignore(t => t.GalleryImageUrls);

        builder.Property<List<TutorSubjectEntry>>("_tutorSubjects")
            .HasField("_tutorSubjects")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("TutorSubjects")
            .HasMaxLength(TutorSubjectsColumnMaxLength)
            .HasConversion(
                new ValueConverter<List<TutorSubjectEntry>, string>(
                    entries => JsonSerializer.Serialize(
                        entries.Select(e => new TutorSubjectEntryDto(e.Subject, e.Level)), (JsonSerializerOptions?)null),
                    value => string.IsNullOrEmpty(value)
                        ? new List<TutorSubjectEntry>()
                        : (JsonSerializer.Deserialize<List<TutorSubjectEntryDto>>(value, (JsonSerializerOptions?)null) ?? new List<TutorSubjectEntryDto>())
                            .Select(dto => TutorSubjectEntry.Of(dto.Subject, dto.Level)).ToList()),
                new ValueComparer<List<TutorSubjectEntry>>(
                    (a, b) => a!.SequenceEqual(b!),
                    entries => entries.Aggregate(0, (hash, e) => HashCode.Combine(hash, e)),
                    entries => entries.ToList()));
        builder.Ignore(t => t.TutorSubjects);

        builder.Ignore(t => t.DomainEvents);
    }

    // A plain, publicly-constructible shape for System.Text.Json to
    // round-trip through — TutorSubjectEntry itself has only a private
    // constructor and a validating static factory (Domain invariant), which
    // JsonSerializer cannot call directly.
    private sealed record TutorSubjectEntryDto(string Subject, string? Level);

    private static void ConfigureStringList(EntityTypeBuilder<Tutor> builder, string fieldName, string columnName, int maxLength)
    {
        builder.Property<List<string>>(fieldName)
            .HasField(fieldName)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName(columnName)
            .HasMaxLength(maxLength)
            .HasConversion(
                new ValueConverter<List<string>, string>(
                    values => JsonSerializer.Serialize(values, (JsonSerializerOptions?)null),
                    value => string.IsNullOrEmpty(value)
                        ? new List<string>()
                        : JsonSerializer.Deserialize<List<string>>(value, (JsonSerializerOptions?)null) ?? new List<string>()),
                new ValueComparer<List<string>>(
                    (a, b) => a!.SequenceEqual(b!),
                    values => values.Aggregate(0, (hash, v) => HashCode.Combine(hash, v)),
                    values => values.ToList()));
    }
}
