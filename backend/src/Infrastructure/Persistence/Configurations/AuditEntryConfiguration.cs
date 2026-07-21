using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TutorFlow.Infrastructure.Audit;

namespace TutorFlow.Infrastructure.Persistence.Configurations;

internal sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("AuditEntries");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.OccurredOnUtc);

        builder.Property(a => a.Action)
            .IsRequired();

        builder.Property(a => a.SubjectId);

        builder.Property(a => a.ActorId);

        builder.Property(a => a.ActorRole);

        // Supports the eventual "reconstruct a Session's entire history"
        // traceability requirement (ADR-009: Traceability Principles) without
        // designing that read surface here.
        builder.HasIndex(a => a.SubjectId);
    }
}
