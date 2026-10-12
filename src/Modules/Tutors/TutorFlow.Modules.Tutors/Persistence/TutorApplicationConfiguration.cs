using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TutorFlow.Modules.Tutors.Domain;

namespace TutorFlow.Modules.Tutors.Persistence;

internal sealed class TutorApplicationConfiguration : IEntityTypeConfiguration<TutorApplication>
{
    public void Configure(EntityTypeBuilder<TutorApplication> builder)
    {
        builder.HasKey(a => a.Id);

        builder.HasIndex(a => a.UserId).IsUnique();

        builder.Property(a => a.DisplayName).HasMaxLength(200);
        builder.Property(a => a.Headline).HasMaxLength(200);
        builder.Property(a => a.About).HasMaxLength(4000);
        builder.Property(a => a.ProfilePhotoReference).HasMaxLength(2000);
        builder.Property(a => a.IntroVideoReference).HasMaxLength(2000);
        builder.Property(a => a.ExperienceSummary).HasMaxLength(4000);
        builder.Property(a => a.RejectionReason).HasMaxLength(2000);
        builder.Property(a => a.SuspensionReason).HasMaxLength(2000);
        builder.Property(a => a.HourlyRateCurrency).HasMaxLength(3);

        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(50);

        builder.OwnsMany(a => a.SpokenLanguages, l => l.ToJson("spoken_languages"));
        builder.PrimitiveCollection(a => a.SubjectIds).HasColumnName("subject_ids");
        builder.PrimitiveCollection(a => a.SpecialtyIds).HasColumnName("specialty_ids");
        builder.Property(a => a.CountryOfBirthCode).HasMaxLength(2);

        builder.OwnsMany(a => a.EducationEntries, education =>
        {
            education.ToJson("education_entries");
        });

        builder.OwnsMany(a => a.CertificateEntries, certificates =>
        {
            certificates.ToJson("certificate_entries");
        });

        builder.Ignore(a => a.HourlyRate);

        builder.Property(a => a.PublicSlug).HasMaxLength(200);
        // Partial unique index (Draft applications have a null slug; Postgres unique indexes
        // already treat multiple NULLs as distinct, so no extra filter is needed here).
        builder.HasIndex(a => a.PublicSlug).IsUnique();

        builder.Property(a => a.IsAcceptingNewStudents).HasDefaultValue(true);
    }
}
