using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Infrastructure.Persistence.Configurations;

internal sealed class ParentGuardianConfiguration : IEntityTypeConfiguration<ParentGuardian>
{
    public void Configure(EntityTypeBuilder<ParentGuardian> builder)
    {
        builder.ToTable("ParentGuardians");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasConversion(new ValueConverter<AccountId, Guid>(id => id.Value, value => AccountId.From(value)))
            .ValueGeneratedNever();

        builder.Property(p => p.Email)
            .HasConversion(new ValueConverter<EmailAddress, string>(email => email.Value, value => EmailAddress.Of(value)))
            .IsRequired();
        builder.HasIndex(p => p.Email).IsUnique();

        builder.Property(p => p.PasswordHash)
            .HasConversion(new ValueConverter<PasswordHash, string>(hash => hash.Value, value => PasswordHash.Of(value)))
            .IsRequired();

        builder.Property(p => p.FailedLoginAttemptCount);
        builder.Property(p => p.LockedUntilUtc);

        builder.Ignore(p => p.DomainEvents);
    }
}
