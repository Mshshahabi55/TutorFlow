using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TutorFlow.Application.Common;
using TutorFlow.Domain.Common;
using TutorFlow.Domain.Scheduling;
using TutorFlow.Domain.Scheduling.ValueObjects;
using TutorFlow.Infrastructure.Common;
using TutorFlow.Infrastructure.Persistence;

namespace TutorFlow.Infrastructure.Tests.Persistence;

// The test the entire Persistence & Concurrency roadmap phase exists to
// satisfy: proves that when two "requests" each read the same Availability
// Slot before either has committed (the exact race the in-memory
// implementation could not prevent), at most one booking succeeds and the
// other is rejected explicitly — CONST-1, enforced physically by the
// database, per docs/adr/ADR-014-concurrency-control-strategy.md.
public class ConcurrencyEnforcementTests
{
    [Fact]
    public async Task Concurrent_bookings_against_the_same_slot_allow_exactly_one_success()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<TutorFlowDbContext>().UseSqlite(connection).Options;

        await using (var bootstrap = new TutorFlowDbContext(options))
        {
            await bootstrap.Database.EnsureCreatedAsync();
        }

        var tutorId = TutorId.From(Guid.NewGuid());
        var slotId = AvailabilitySlotId.New();

        await using (var seed = new TutorFlowDbContext(options))
        {
            var seedSlot = AvailabilitySlot.Declare(
                tutorId,
                DateTime.UtcNow.AddDays(1),
                SessionDuration.Of(TimeSpan.FromHours(1)),
                DeliveryMode.Online);
            seed.AvailabilitySlots.Add(seedSlot);
            await seed.SaveChangesAsync();
            slotId = seedSlot.Id;
        }

        // Two separate DbContexts, mimicking two separate concurrent
        // requests, each reading the slot while it is still unconsumed.
        await using var contextA = new TutorFlowDbContext(options);
        await using var contextB = new TutorFlowDbContext(options);

        var slotA = await contextA.AvailabilitySlots.SingleAsync(s => s.Id == slotId);
        var slotB = await contextB.AvailabilitySlots.SingleAsync(s => s.Id == slotId);

        var sessionA = slotA.Book(StudentId.From(Guid.NewGuid()), null);
        var sessionB = slotB.Book(StudentId.From(Guid.NewGuid()), null);

        contextA.Sessions.Add(sessionA);
        contextB.Sessions.Add(sessionB);

        var unitOfWorkA = new EfUnitOfWork(contextA, new DomainEventDispatcher(Enumerable.Empty<IDomainEventHandler>()));
        var unitOfWorkB = new EfUnitOfWork(contextB, new DomainEventDispatcher(Enumerable.Empty<IDomainEventHandler>()));

        // First writer commits successfully.
        await unitOfWorkA.SaveChangesAsync(new IAggregateRoot[] { slotA, sessionA });

        // Second writer's commit is rejected by the database's own unique
        // constraint on Session.AvailabilitySlotId — neither writer's
        // in-memory IsConsumed check could have caught this, since contextB
        // read the slot before contextA's commit.
        await Assert.ThrowsAsync<ConcurrencyConflictException>(
            () => unitOfWorkB.SaveChangesAsync(new IAggregateRoot[] { slotB, sessionB }));

        await using var verify = new TutorFlowDbContext(options);
        var sessionsForSlot = await verify.Sessions.Where(s => s.AvailabilitySlotId == slotId).ToListAsync();
        Assert.Single(sessionsForSlot);
        Assert.Equal(sessionA.Id, sessionsForSlot[0].Id);
    }
}
