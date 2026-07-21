using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Infrastructure.Persistence.Configurations;

internal sealed class RelationshipConfiguration : IEntityTypeConfiguration<Relationship>
{
    public void Configure(EntityTypeBuilder<Relationship> builder)
    {
        builder.ToTable("Relationships");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .HasConversion(new ValueConverter<RelationshipId, Guid>(id => id.Value, value => RelationshipId.From(value)))
            .ValueGeneratedNever();

        builder.Property(r => r.ParentGuardianId)
            .HasConversion(new ValueConverter<AccountId, Guid>(id => id.Value, value => AccountId.From(value)));

        builder.Property(r => r.StudentId)
            .HasConversion(new ValueConverter<AccountId, Guid>(id => id.Value, value => AccountId.From(value)));

        builder.Property(r => r.InvitedByAccountId)
            .HasConversion(new ValueConverter<AccountId, Guid>(id => id.Value, value => AccountId.From(value)));

        builder.Property(r => r.Status);

        builder.Ignore(r => r.DomainEvents);
    }
}
