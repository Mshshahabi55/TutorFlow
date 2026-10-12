using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Availability.Domain;

namespace TutorFlow.Modules.Availability.Persistence;

public sealed class AvailabilityDbContext(DbContextOptions<AvailabilityDbContext> options) : DbContext(options)
{
    public DbSet<WeeklyAvailabilityRule> WeeklyAvailabilityRules => Set<WeeklyAvailabilityRule>();

    public DbSet<TimeOff> TimeOffPeriods => Set<TimeOff>();

    public DbSet<BookingSettings> BookingSettings => Set<BookingSettings>();

    public DbSet<TutorBookability> TutorBookabilityRecords => Set<TutorBookability>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("availability");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AvailabilityDbContext).Assembly);
    }
}
