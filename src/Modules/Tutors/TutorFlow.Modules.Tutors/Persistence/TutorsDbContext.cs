using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Tutors.Domain;
using Wolverine.EntityFrameworkCore;

namespace TutorFlow.Modules.Tutors.Persistence;

public sealed class TutorsDbContext(DbContextOptions<TutorsDbContext> options) : DbContext(options)
{
    public DbSet<TutorApplication> TutorApplications => Set<TutorApplication>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("tutors");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TutorsDbContext).Assembly);

        // A44: Tutors' own durable outbox/inbox tables, in its own schema — registered as an
        // Ancillary Wolverine message store (Program.cs) for SuspendApplication/
        // RejectCertificate/PublishApplication.
        modelBuilder.MapWolverineEnvelopeStorage();
    }
}
