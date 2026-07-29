using Microsoft.EntityFrameworkCore;
using TutorFlow.Domain.Communication;
using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Meetings;
using TutorFlow.Domain.Scheduling;
using TutorFlow.Infrastructure.Audit;

namespace TutorFlow.Infrastructure.Persistence;

// Real persistence per ADR-013 (PostgreSQL) and ADR-014 (unique-constraint
// concurrency enforcement) — replaces the temporary in-memory repositories.
// Maps exactly the aggregates each Application-layer repository interface
// already exposes; introduces no new aggregate or business rule.
public sealed class TutorFlowDbContext : DbContext
{
    public TutorFlowDbContext(DbContextOptions<TutorFlowDbContext> options) : base(options)
    {
    }

    public DbSet<Tutor> Tutors => Set<Tutor>();

    public DbSet<Student> Students => Set<Student>();

    public DbSet<ParentGuardian> ParentGuardians => Set<ParentGuardian>();

    public DbSet<AdminStaff> AdminStaffs => Set<AdminStaff>();

    public DbSet<AuthToken> AuthTokens => Set<AuthToken>();

    public DbSet<Relationship> Relationships => Set<Relationship>();

    public DbSet<AvailabilitySlot> AvailabilitySlots => Set<AvailabilitySlot>();

    public DbSet<Session> Sessions => Set<Session>();

    public DbSet<Conversation> Conversations => Set<Conversation>();

    public DbSet<Message> Messages => Set<Message>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<Meeting> Meetings => Set<Meeting>();

    // Internal: AuditEntry is Infrastructure-only derived data with no
    // Application-layer reader (ADR-016: Impact on DDD); AuditDomainEventHandler
    // reaches it via Add(...)/Set<AuditEntry>() directly, not through this
    // property specifically.
    internal DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TutorFlowDbContext).Assembly);
    }

    // Applies to every DateTime/DateTime? property on every entity, project-
    // wide, with no per-property configuration needed (see
    // UtcDateTimeValueConverter.cs for why this exists and what it does and
    // does not "fix").
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeValueConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<UtcNullableDateTimeValueConverter>();
    }
}
