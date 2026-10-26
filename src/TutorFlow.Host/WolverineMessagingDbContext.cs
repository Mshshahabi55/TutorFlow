using Microsoft.EntityFrameworkCore;
using Wolverine.EntityFrameworkCore;

namespace TutorFlow.Host;

/// <summary>
/// Owns Wolverine's <c>MessageStoreRole.Main</c> durable message store (A44) — node/leader-
/// election bookkeeping, the control queue, and this app's own incoming/outgoing/dead-letter
/// envelope tables. Infrastructure-only: no business entities, and deliberately not owned by any
/// of the 14 feature modules, since node coordination is cross-cutting Host concern, not any one
/// module's business data. Not to be confused with TutorFlow.Modules.Messaging (the student/tutor
/// conversation feature) — this schema and its "messaging" name refer only to Wolverine's own
/// message-store terminology.
/// </summary>
public sealed class WolverineMessagingDbContext(DbContextOptions<WolverineMessagingDbContext> options)
    : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("messaging");
        modelBuilder.MapWolverineEnvelopeStorage();
    }
}
