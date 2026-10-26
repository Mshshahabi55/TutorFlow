using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TutorFlow.Modules.Tutors.Domain;

namespace TutorFlow.Modules.Tutors.Persistence;

internal sealed class TutorApplicationConfiguration : IEntityTypeConfiguration<TutorApplication>
{
    public void Configure(EntityTypeBuilder<TutorApplication> builder)
    {
        builder.HasKey(a => a.Id);

        // Remediation for audit finding F1 — CertificateEntries is a single owned-JSON column
        // (see the OwnsMany block below): without a concurrency token, EF overwrites the *entire*
        // column on every SaveChangesAsync from whatever snapshot the caller loaded, so two
        // concurrent admin decisions on the same application (even on two different certificates)
        // silently lost-update one another — the last SaveChangesAsync to commit wins, with no
        // exception and no log, even though both HTTP calls report success. Postgres's own
        // `xmin` system column (bumped by Postgres itself on every UPDATE, never something this
        // app has to maintain) is the Npgsql-documented, zero-migration-risk way to get optimistic
        // concurrency here: a shadow property, not a real column, so there is nothing to add or
        // backfill. EF includes it in the UPDATE's WHERE clause and throws
        // DbUpdateConcurrencyException if the row changed since it was read — every
        // certificate-mutating handler (Verify/Reject/Withdraw/Submit/UpdateDraft) now catches
        // that and the endpoint layer maps it to 409 Conflict, instead of a silent overwrite.
        builder.Property<uint>("xmin").IsRowVersion();

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

        builder.OwnsMany(a => a.SpokenLanguages, l =>
        {
            l.ToJson("spoken_languages");
            // Without an explicit conversion, this enum's on-disk JSON representation was an
            // unenforced implementation detail — a direct read-only query against the dev DB
            // found existing rows stored as both a string ("Native") and a raw int (2), under
            // this exact, unmodified configuration. Explicit string conversion, matching Status
            // above, makes the representation deterministic going forward (backfilled separately
            // for pre-existing rows — see docs/product/preply-reverse-engineering/26-...md).
            l.Property(x => x.Proficiency).HasConversion<string>();
        });
        builder.PrimitiveCollection(a => a.SubjectIds).HasColumnName("subject_ids");
        builder.PrimitiveCollection(a => a.SpecialtyIds).HasColumnName("specialty_ids");
        builder.Property(a => a.CountryOfBirthCode).HasMaxLength(2);
        builder.Property(a => a.PhoneNumber).HasMaxLength(32);

        builder.OwnsMany(a => a.EducationEntries, education =>
        {
            education.ToJson("education_entries");
        });

        builder.OwnsMany(a => a.CertificateEntries, certificates =>
        {
            certificates.ToJson("certificate_entries");
            // EF Core does not support an explicitly configured key on an owned type stored in a
            // JSON collection (confirmed: throws "Explicitly configured primary keys are not
            // supported on entities stored in JSON collections" at model-build time) — it always
            // uses its own synthesized ordinal key internally regardless. That's fine: every
            // Stage 2 reconciliation/lifecycle operation (ApplyDraftUpdate, SubmitCertificateDocument,
            // VerifyCertificate, etc.) identifies entries purely by comparing the domain
            // CertificateEntry.Id property in C#, never via EF's own internal key.
            // Explicit string conversion — same precedent, and same reason, as
            // SpokenLanguages.Proficiency above: never leave an enum's on-disk JSON
            // representation as an unenforced int-vs-string implementation detail.
            certificates.Property(c => c.VerificationStatus).HasConversion<string>();
        });

        builder.Ignore(a => a.HourlyRate);
        builder.Ignore(a => a.PendingFileCleanup);

        builder.Property(a => a.PublicSlug).HasMaxLength(200);
        // Partial unique index (Draft applications have a null slug; Postgres unique indexes
        // already treat multiple NULLs as distinct, so no extra filter is needed here).
        builder.HasIndex(a => a.PublicSlug).IsUnique();

        builder.Property(a => a.IsAcceptingNewStudents).HasDefaultValue(true);
    }
}
