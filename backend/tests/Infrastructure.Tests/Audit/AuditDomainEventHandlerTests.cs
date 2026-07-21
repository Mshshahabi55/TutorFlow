using TutorFlow.Domain.Identity.Events;
using TutorFlow.Domain.Scheduling.ValueObjects;
using TutorFlow.Infrastructure.Audit;
using TutorFlow.Infrastructure.Common;
using TutorFlow.Infrastructure.Tests.Persistence;

namespace TutorFlow.Infrastructure.Tests.Audit;

// Unit-level: verifies AuditDomainEventHandler's own mapping/filtering logic
// in isolation, using SaveChangesAsync directly (not through EfUnitOfWork) so
// these tests are about the handler's behavior, not the dispatch pipeline —
// see AuditDurabilityTests for the full same-transaction integration proof
// (docs/adr/ADR-016-audit-durability-strategy.md).
public class AuditDomainEventHandlerTests
{
    [Fact]
    public async Task HandleAsync_stages_an_audit_entry_for_an_in_scope_event()
    {
        using var sqlite = new SqliteTestDbContext();
        var handler = new AuditDomainEventHandler(sqlite.DbContext, new NullCurrentUserProvider());
        var sessionId = SessionId.New();
        var tutorId = TutorId.From(Guid.NewGuid());
        var studentId = StudentId.From(Guid.NewGuid());
        var slotId = AvailabilitySlotId.New();

        await handler.HandleAsync(new Domain.Scheduling.Events.SessionBooked(sessionId, tutorId, studentId, slotId));
        await sqlite.DbContext.SaveChangesAsync();

        var entry = Assert.Single(sqlite.DbContext.AuditEntries);
        Assert.Equal("SessionBooked", entry.Action);
        Assert.Equal(sessionId.Value, entry.SubjectId);
        Assert.Null(entry.ActorId);
        Assert.Null(entry.ActorRole);
    }

    [Fact]
    public async Task HandleAsync_stages_an_audit_entry_for_TutorApproved()
    {
        using var sqlite = new SqliteTestDbContext();
        var handler = new AuditDomainEventHandler(sqlite.DbContext, new NullCurrentUserProvider());
        var tutorId = Domain.Identity.ValueObjects.AccountId.New();

        await handler.HandleAsync(new TutorApproved(tutorId));
        await sqlite.DbContext.SaveChangesAsync();

        var entry = Assert.Single(sqlite.DbContext.AuditEntries);
        Assert.Equal("TutorApproved", entry.Action);
        Assert.Equal(tutorId.Value, entry.SubjectId);
    }

    [Fact]
    public async Task HandleAsync_does_not_stage_an_entry_for_an_out_of_scope_event()
    {
        using var sqlite = new SqliteTestDbContext();
        var handler = new AuditDomainEventHandler(sqlite.DbContext, new NullCurrentUserProvider());

        await handler.HandleAsync(new RelationshipInvited(
            Domain.Identity.ValueObjects.RelationshipId.New(),
            Domain.Identity.ValueObjects.AccountId.New(),
            Domain.Identity.ValueObjects.AccountId.New()));
        await sqlite.DbContext.SaveChangesAsync();

        Assert.Empty(sqlite.DbContext.AuditEntries);
    }

    [Fact]
    public async Task HandleAsync_does_not_stage_an_entry_for_TutorRegistered()
    {
        using var sqlite = new SqliteTestDbContext();
        var handler = new AuditDomainEventHandler(sqlite.DbContext, new NullCurrentUserProvider());

        await handler.HandleAsync(new TutorRegistered(Domain.Identity.ValueObjects.AccountId.New()));
        await sqlite.DbContext.SaveChangesAsync();

        Assert.Empty(sqlite.DbContext.AuditEntries);
    }
}
