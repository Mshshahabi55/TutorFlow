using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Students.Domain;

namespace TutorFlow.Modules.Students.Persistence;

public sealed class StudentsDbContext(DbContextOptions<StudentsDbContext> options) : DbContext(options)
{
    public DbSet<FavoriteTutor> FavoriteTutors => Set<FavoriteTutor>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("students");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(StudentsDbContext).Assembly);
    }
}
