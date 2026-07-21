using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Infrastructure.Persistence.Configurations;

internal sealed class AuthTokenConfiguration : IEntityTypeConfiguration<AuthToken>
{
    public void Configure(EntityTypeBuilder<AuthToken> builder)
    {
        builder.ToTable("AuthTokens");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .HasConversion(new ValueConverter<AuthTokenId, Guid>(id => id.Value, value => AuthTokenId.From(value)))
            .ValueGeneratedNever();

        // No physical foreign key: AccountId is not a key in any single
        // table (email is unique per role, so each role's Account lives in
        // its own table) — a valid, enforced-in-code-only reference, the
        // same situation Session/AvailabilitySlot already navigate for
        // cross-aggregate references.
        builder.Property(t => t.AccountId)
            .HasConversion(new ValueConverter<AccountId, Guid>(id => id.Value, value => AccountId.From(value)))
            .IsRequired();
        builder.HasIndex(t => t.AccountId);

        builder.Property(t => t.Role).IsRequired();

        builder.Property(t => t.TokenHash).IsRequired();
        builder.HasIndex(t => t.TokenHash).IsUnique();

        builder.Property(t => t.CreatedAtUtc).IsRequired();
        builder.Property(t => t.ExpiresAtUtc).IsRequired();
        builder.Property(t => t.AbsoluteExpiresAtUtc).IsRequired();
        builder.Property(t => t.RevokedAtUtc);

        builder.Ignore(t => t.DomainEvents);
    }
}
