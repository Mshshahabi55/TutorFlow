using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Subjects.Domain;

namespace TutorFlow.Modules.Subjects.Persistence;

public sealed class SubjectsDbContext(DbContextOptions<SubjectsDbContext> options) : DbContext(options)
{
    public DbSet<Subject> Subjects => Set<Subject>();

    public DbSet<Specialty> Specialties => Set<Specialty>();

    public DbSet<Language> Languages => Set<Language>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("subjects");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SubjectsDbContext).Assembly);
    }
}
