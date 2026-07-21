using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Infrastructure.Persistence.Configurations;

internal sealed class AdminStaffConfiguration : IEntityTypeConfiguration<AdminStaff>
{
    public void Configure(EntityTypeBuilder<AdminStaff> builder)
    {
        builder.ToTable("AdminStaffs");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .HasConversion(new ValueConverter<AccountId, Guid>(id => id.Value, value => AccountId.From(value)))
            .ValueGeneratedNever();

        builder.Property(a => a.Email)
            .HasConversion(new ValueConverter<EmailAddress, string>(email => email.Value, value => EmailAddress.Of(value)))
            .IsRequired();
        builder.HasIndex(a => a.Email).IsUnique();

        builder.Property(a => a.PasswordHash)
            .HasConversion(new ValueConverter<PasswordHash, string>(hash => hash.Value, value => PasswordHash.Of(value)))
            .IsRequired();

        builder.Property(a => a.FailedLoginAttemptCount);
        builder.Property(a => a.LockedUntilUtc);

        builder.Ignore(a => a.DomainEvents);
    }
}
