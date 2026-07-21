using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TutorFlow.Application.Common;
using TutorFlow.Domain.Common;
using TutorFlow.Domain.Scheduling;
using TutorFlow.Domain.Scheduling.ValueObjects;
using TutorFlow.Infrastructure.Audit;
using TutorFlow.Infrastructure.Common;
using TutorFlow.Infrastructure.Persistence;
using TutorFlow.Infrastructure.Tests.Persistence;

namespace TutorFlow.Infrastructure.Tests.Audit;

// Proves docs/adr/ADR-016-audit-durability-strategy.md's accepted decision —
// same-transaction durability — through the real pipeline: EfUnitOfWork,
// DomainEventDispatcher, and AuditDomainEventHandler sharing one DbContext,
// exactly as wired in Infrastructure/DependencyInjection.cs.
public class AuditDurabilityTests
{
    private static IDomainEventDispatcher DispatcherFor(TutorFlowDbContext dbContext) =>
        new DomainEventDispatcher(new IDomainEventHandler[]
        {
            new AuditDomainEventHandler(dbContext, new NullCurrentUserProvider()),
        });

    [Fact]
    public async Task Booking_a_session_persists_the_session_and_an_audit_entry_together()
    {
        using var sqlite = new SqliteTestDbContext();
        var unitOfWork = new EfUnitOfWork(sqlite.DbContext, DispatcherFor(sqlite.DbContext));

        // Declared and saved in its own prior transaction, exactly as
        // production does (DeclareAvailabilityCommandHandler runs separately
        // from BookSessionCommandHandler) — so AvailabilityDeclared is
        // already dispatched and cleared before booking is attempted below.
        var slot = AvailabilitySlot.Declare(
            TutorId.From(Guid.NewGuid()),
            DateTime.UtcNow.AddDays(1),
            SessionDuration.Of(TimeSpan.FromHours(1)),
            DeliveryMode.Online);
        sqlite.DbContext.AvailabilitySlots.Add(slot);
        await unitOfWork.SaveChangesAsync(new IAggregateRoot[] { slot });

        // AvailabilityDeclared is itself one of ADR-009's six audited
        // actions ("changing Availability"), so declaring already produced
        // its own audit entry in that first transaction.
        Assert.Single(sqlite.DbContext.AuditEntries, e => e.Action == "AvailabilityDeclared");

        var session = slot.Book(StudentId.From(Guid.NewGuid()), null);
        sqlite.DbContext.Sessions.Add(session);

        await unitOfWork.SaveChangesAsync(new IAggregateRoot[] { slot, session });

        Assert.Single(sqlite.DbContext.Sessions);
        var bookedEntry = Assert.Single(sqlite.DbContext.AuditEntries, e => e.Action == "SessionBooked");
        Assert.Equal(session.Id.Value, bookedEntry.SubjectId);
    }

    [Fact]
    public async Task A_failed_transaction_persists_neither_the_mutation_nor_its_audit_entry()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<TutorFlowDbContext>().UseSqlite(connection).Options;

        await using (var bootstrap = new TutorFlowDbContext(options))
        {
            await bootstrap.Database.EnsureCreatedAsync();
        }

        var tutorId = TutorId.From(Guid.NewGuid());
        Guid slotId;
        await using (var seed = new TutorFlowDbContext(options))
        {
            var seedSlot = AvailabilitySlot.Declare(
                tutorId, DateTime.UtcNow.AddDays(1), SessionDuration.Of(TimeSpan.FromHours(1)), DeliveryMode.Online);
            seed.AvailabilitySlots.Add(seedSlot);
            await seed.SaveChangesAsync();
            slotId = seedSlot.Id.Value;
        }

        // Two concurrent "requests" against the same slot — the same race
        // ConcurrencyEnforcementTests proves at the persistence layer, here
        // asserting the audit entry the losing attempt staged is rolled back
        // together with its Session, never left as an orphaned audit record.
        await using var contextA = new TutorFlowDbContext(options);
        await using var contextB = new TutorFlowDbContext(options);

        var slotA = await contextA.AvailabilitySlots.SingleAsync(s => s.Id == AvailabilitySlotId.From(slotId));
        var slotB = await contextB.AvailabilitySlots.SingleAsync(s => s.Id == AvailabilitySlotId.From(slotId));

        var sessionA = slotA.Book(StudentId.From(Guid.NewGuid()), null);
        var sessionB = slotB.Book(StudentId.From(Guid.NewGuid()), null);
        contextA.Sessions.Add(sessionA);
        contextB.Sessions.Add(sessionB);

        var unitOfWorkA = new EfUnitOfWork(contextA, DispatcherFor(contextA));
        var unitOfWorkB = new EfUnitOfWork(contextB, DispatcherFor(contextB));

        await unitOfWorkA.SaveChangesAsync(new IAggregateRoot[] { slotA, sessionA });

        await Assert.ThrowsAsync<ConcurrencyConflictException>(
            () => unitOfWorkB.SaveChangesAsync(new IAggregateRoot[] { slotB, sessionB }));

        await using var verify = new TutorFlowDbContext(options);
        var sessions = await verify.Sessions.ToListAsync();
        var auditEntries = await verify.AuditEntries.ToListAsync();

        Assert.Single(sessions);
        Assert.Equal(sessionA.Id, sessions[0].Id);
        Assert.Single(auditEntries);
        Assert.Equal(sessionA.Id.Value, auditEntries[0].SubjectId);
    }
}
