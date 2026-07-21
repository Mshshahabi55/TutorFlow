using TutorFlow.Domain.Scheduling;
using TutorFlow.Domain.Scheduling.ValueObjects;
using TutorFlow.Infrastructure.Scheduling.Repositories;
using TutorFlow.Infrastructure.Tests.Persistence;

namespace TutorFlow.Infrastructure.Tests.Scheduling.Repositories;

public class AvailabilitySlotRepositoryTests
{
    [Fact]
    public async Task AddAsync_then_GetByIdAsync_returns_the_same_slot()
    {
        using var sqlite = new SqliteTestDbContext();
        var repository = new AvailabilitySlotRepository(sqlite.DbContext);
        var slot = AvailabilitySlot.Declare(
            TutorId.From(Guid.NewGuid()),
            DateTime.UtcNow.AddDays(1),
            SessionDuration.Of(TimeSpan.FromHours(1)),
            DeliveryMode.Online);

        await repository.AddAsync(slot);
        await sqlite.DbContext.SaveChangesAsync();

        var stored = await repository.GetByIdAsync(slot.Id);
        Assert.NotNull(stored);
        Assert.Equal(slot.Id, stored!.Id);
        Assert.False(stored.IsConsumed);
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_for_unknown_id()
    {
        using var sqlite = new SqliteTestDbContext();
        var repository = new AvailabilitySlotRepository(sqlite.DbContext);

        var result = await repository.GetByIdAsync(AvailabilitySlotId.New());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetTutorIdsWithOpenSlotFromAsync_excludes_consumed_and_earlier_slots()
    {
        using var sqlite = new SqliteTestDbContext();
        var repository = new AvailabilitySlotRepository(sqlite.DbContext);

        var fromUtc = DateTime.UtcNow;

        var futureOpenTutorId = TutorId.From(Guid.NewGuid());
        var futureOpen = AvailabilitySlot.Declare(
            futureOpenTutorId, fromUtc.AddDays(5), SessionDuration.Of(TimeSpan.FromHours(1)), DeliveryMode.Online);

        var pastOpenTutorId = TutorId.From(Guid.NewGuid());
        var pastOpen = AvailabilitySlot.Declare(
            pastOpenTutorId, fromUtc.AddDays(-5), SessionDuration.Of(TimeSpan.FromHours(1)), DeliveryMode.Online);

        var futureConsumedTutorId = TutorId.From(Guid.NewGuid());
        var futureConsumed = AvailabilitySlot.Declare(
            futureConsumedTutorId, fromUtc.AddDays(5), SessionDuration.Of(TimeSpan.FromHours(1)), DeliveryMode.Online);
        futureConsumed.Book(StudentId.From(Guid.NewGuid()), null);

        await repository.AddAsync(futureOpen);
        await repository.AddAsync(pastOpen);
        await repository.AddAsync(futureConsumed);
        await sqlite.DbContext.SaveChangesAsync();

        var result = await repository.GetTutorIdsWithOpenSlotFromAsync(fromUtc);

        Assert.Contains(futureOpenTutorId, result);
        Assert.DoesNotContain(pastOpenTutorId, result);
        Assert.DoesNotContain(futureConsumedTutorId, result);
    }

    [Fact]
    public async Task GetTutorIdsWithOpenSlotFromAsync_returns_each_tutor_only_once()
    {
        using var sqlite = new SqliteTestDbContext();
        var repository = new AvailabilitySlotRepository(sqlite.DbContext);
        var fromUtc = DateTime.UtcNow;
        var tutorId = TutorId.From(Guid.NewGuid());

        var slotA = AvailabilitySlot.Declare(tutorId, fromUtc.AddDays(1), SessionDuration.Of(TimeSpan.FromHours(1)), DeliveryMode.Online);
        var slotB = AvailabilitySlot.Declare(tutorId, fromUtc.AddDays(2), SessionDuration.Of(TimeSpan.FromHours(1)), DeliveryMode.Online);
        await repository.AddAsync(slotA);
        await repository.AddAsync(slotB);
        await sqlite.DbContext.SaveChangesAsync();

        var result = await repository.GetTutorIdsWithOpenSlotFromAsync(fromUtc);

        Assert.Single(result, id => id == tutorId);
    }

    [Fact]
    public async Task GetByTutorIdAsync_returns_every_slot_for_the_tutor_ordered_by_start_time()
    {
        using var sqlite = new SqliteTestDbContext();
        var repository = new AvailabilitySlotRepository(sqlite.DbContext);
        var tutorId = TutorId.From(Guid.NewGuid());
        var now = DateTime.UtcNow;

        var later = AvailabilitySlot.Declare(tutorId, now.AddDays(5), SessionDuration.Of(TimeSpan.FromHours(1)), DeliveryMode.Online);
        var earlier = AvailabilitySlot.Declare(tutorId, now.AddDays(1), SessionDuration.Of(TimeSpan.FromHours(1)), DeliveryMode.Online);
        var otherTutor = AvailabilitySlot.Declare(TutorId.From(Guid.NewGuid()), now.AddDays(1), SessionDuration.Of(TimeSpan.FromHours(1)), DeliveryMode.Online);

        await repository.AddAsync(later);
        await repository.AddAsync(earlier);
        await repository.AddAsync(otherTutor);
        await sqlite.DbContext.SaveChangesAsync();

        var result = await repository.GetByTutorIdAsync(tutorId);

        var ordered = result.ToList();
        Assert.Equal(2, ordered.Count);
        Assert.Equal(earlier.Id, ordered[0].Id);
        Assert.Equal(later.Id, ordered[1].Id);
    }

    [Fact]
    public async Task GetByTutorIdAsync_reflects_consumption_state()
    {
        using var sqlite = new SqliteTestDbContext();
        var repository = new AvailabilitySlotRepository(sqlite.DbContext);
        var tutorId = TutorId.From(Guid.NewGuid());

        var slot = AvailabilitySlot.Declare(tutorId, DateTime.UtcNow.AddDays(1), SessionDuration.Of(TimeSpan.FromHours(1)), DeliveryMode.Online);
        slot.Book(StudentId.From(Guid.NewGuid()), null);
        await repository.AddAsync(slot);
        await sqlite.DbContext.SaveChangesAsync();

        var result = await repository.GetByTutorIdAsync(tutorId);

        Assert.True(result.Single().IsConsumed);
    }
}
