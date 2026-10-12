using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TutorFlow.Modules.Search.Domain;

namespace TutorFlow.Modules.Search.Persistence;

internal sealed class TutorSearchDocumentConfiguration : IEntityTypeConfiguration<TutorSearchDocument>
{
    public void Configure(EntityTypeBuilder<TutorSearchDocument> builder)
    {
        builder.HasKey(d => d.TutorUserId);

        builder.Property(d => d.Slug).HasMaxLength(200);
        builder.Property(d => d.DisplayName).HasMaxLength(200);
        builder.Property(d => d.Headline).HasMaxLength(200);
        builder.Property(d => d.PhotoUrl).HasMaxLength(2000);
        builder.Property(d => d.Currency).HasMaxLength(3);
        builder.Property(d => d.CountryOfBirthCode).HasMaxLength(2);

        builder.PrimitiveCollection(d => d.SubjectIds).HasColumnName("subject_ids");
        builder.PrimitiveCollection(d => d.SpecialtyIds).HasColumnName("specialty_ids");
        builder.PrimitiveCollection(d => d.SpokenLanguageCodes).HasColumnName("spoken_language_codes");
        builder.PrimitiveCollection(d => d.NativeLanguageCodes).HasColumnName("native_language_codes");

        builder.HasIndex(d => d.IsPublished);
        builder.HasIndex(d => d.AverageRating);
        builder.HasIndex(d => d.HourlyRateMinorUnits);
        builder.HasIndex(d => d.PublishedAtUtc);
        builder.HasIndex(d => d.CountryOfBirthCode);
        builder.HasIndex(d => d.IsProfessionalTutor);
        builder.HasIndex(d => d.IsSuperTutor);

        // GIN index on SubjectIds/SpecialtyIds for fast array-containment filtering
        // (`subject_ids @> ARRAY[...]`-style queries) — Postgres' default GIN opclass for
        // array columns, no extension required (unlike the trigram index below).
        builder.HasIndex(d => d.SubjectIds).HasMethod("gin");
        builder.HasIndex(d => d.SpecialtyIds).HasMethod("gin");

        // Full-text/trigram search over DisplayName + Headline (ADR-0007). pg_trgm is a
        // trusted extension — tutorflow_app can create it without superuser, same as
        // Bookings' btree_gist (docker/postgres/init/01-create-app-role.sh).
        builder.HasIndex(d => new { d.DisplayName, d.Headline })
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops", "gin_trgm_ops")
            .HasDatabaseName("ix_tutor_search_documents_name_headline_trgm");
    }
}
