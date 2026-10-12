using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Lessons.Domain;

namespace TutorFlow.Modules.Lessons.Persistence;

// Public, not the usual internal-by-default convention — Wolverine's codegen generates direct
// construction code for a consumer's dependencies, and an internal concrete type can't be
// referenced from that generated code (same reason PaymentsDbContext/BookingsDbContext are
// public; see ConfirmBooking.cs's own comment for the precedent).
public sealed class LessonsDbContext(DbContextOptions<LessonsDbContext> options) : DbContext(options)
{
    public DbSet<Lesson> Lessons => Set<Lesson>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("lessons");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LessonsDbContext).Assembly);
    }
}
