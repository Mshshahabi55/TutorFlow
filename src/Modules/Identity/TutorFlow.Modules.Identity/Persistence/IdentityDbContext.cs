using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Identity.Domain;
using TutorFlow.Modules.Identity.Infrastructure;

namespace TutorFlow.Modules.Identity.Persistence;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options)
    : Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasDefaultSchema("identity");

        builder.Entity<IdentityRole<Guid>>().HasData(
            new IdentityRole<Guid>
            {
                Id = RoleIds.Student,
                Name = RoleNames.Student,
                NormalizedName = RoleNames.Student.ToUpperInvariant(),
                ConcurrencyStamp = "02a3f6ad-8b1a-4b2f-9f3b-6f8f2a7f9a01"
            },
            new IdentityRole<Guid>
            {
                Id = RoleIds.Tutor,
                Name = RoleNames.Tutor,
                NormalizedName = RoleNames.Tutor.ToUpperInvariant(),
                ConcurrencyStamp = "15e3b6b0-6b8a-4a0e-8e3a-2f6a8e9c4b02"
            },
            new IdentityRole<Guid>
            {
                Id = RoleIds.Admin,
                Name = RoleNames.Admin,
                NormalizedName = RoleNames.Admin.ToUpperInvariant(),
                ConcurrencyStamp = "9d4f2a3b-7c1e-4f5a-8b6d-3e9a1c2f7b04"
            });
    }
}
