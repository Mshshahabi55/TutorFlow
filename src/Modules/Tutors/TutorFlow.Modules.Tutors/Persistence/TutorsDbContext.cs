using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Tutors.Domain;

namespace TutorFlow.Modules.Tutors.Persistence;

public sealed class TutorsDbContext(DbContextOptions<TutorsDbContext> options) : DbContext(options)
{
    public DbSet<TutorApplication> TutorApplications => Set<TutorApplication>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("tutors");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TutorsDbContext).Assembly);
    }
}
