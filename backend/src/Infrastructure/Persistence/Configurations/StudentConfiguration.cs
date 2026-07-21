using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Infrastructure.Persistence.Configurations;

internal sealed class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("Students");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .HasConversion(new ValueConverter<AccountId, Guid>(id => id.Value, value => AccountId.From(value)))
            .ValueGeneratedNever();

        builder.Property(s => s.Email)
            .HasConversion(new ValueConverter<EmailAddress, string>(email => email.Value, value => EmailAddress.Of(value)))
            .IsRequired();
        builder.HasIndex(s => s.Email).IsUnique();

        builder.Property(s => s.PasswordHash)
            .HasConversion(new ValueConverter<PasswordHash, string>(hash => hash.Value, value => PasswordHash.Of(value)))
            .IsRequired();

        builder.Property(s => s.FailedLoginAttemptCount);
        builder.Property(s => s.LockedUntilUtc);

        builder.Property(s => s.IsMinor);

        builder.Ignore(s => s.DomainEvents);
    }
}
