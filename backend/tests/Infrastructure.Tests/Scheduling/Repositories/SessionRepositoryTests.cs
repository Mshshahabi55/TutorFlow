using TutorFlow.Application.Common;
using TutorFlow.Domain.Scheduling;
using TutorFlow.Domain.Scheduling.ValueObjects;
using TutorFlow.Infrastructure.Scheduling.Repositories;
using TutorFlow.Infrastructure.Tests.Persistence;

namespace TutorFlow.Infrastructure.Tests.Scheduling.Repositories;

public class SessionRepositoryTests
{
    private static AvailabilitySlot DeclareSlot(TutorId tutorId) =>
        AvailabilitySlot.Declare(tutorId, DateTime.UtcNow.AddDays(1), SessionDuration.Of(TimeSpan.FromHours(1)), DeliveryMode.Online);

    [Fact]
    public async Task AddAsync_then_GetByIdAsync_returns_the_same_session()
    {
        using var sqlite = new SqliteTestDbContext();
        var repository = new SessionRepository(sqlite.DbContext);
        var slot = DeclareSlot(TutorId.From(Guid.NewGuid()));
        sqlite.DbContext.AvailabilitySlots.Add(slot);
        var session = slot.Book(StudentId.From(Guid.NewGuid()), null);

        await repository.AddAsync(session);
        await sqlite.DbContext.SaveChangesAsync();

        var stored = await repository.GetByIdAsync(session.Id);
        Assert.NotNull(stored);
        Assert.Equal(session.Id, stored!.Id);
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_for_unknown_id()
    {
        using var sqlite = new SqliteTestDbContext();
        var repository = new SessionRepository(sqlite.DbContext);

        var result = await repository.GetByIdAsync(SessionId.New());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByTutorIdAsync_returns_only_that_tutors_sessions()
    {
        using var sqlite = new SqliteTestDbContext();
        var repository = new SessionRepository(sqlite.DbContext);

        var tutorId = TutorId.From(Guid.NewGuid());
        var slot1 = DeclareSlot(tutorId);
        var slot2 = DeclareSlot(TutorId.From(Guid.NewGuid()));
        sqlite.DbContext.AvailabilitySlots.AddRange(slot1, slot2);

        var ownSession = slot1.Book(StudentId.From(Guid.NewGuid()), null);
        var otherSession = slot2.Book(StudentId.From(Guid.NewGuid()), null);
        await repository.AddAsync(ownSession);
        await repository.AddAsync(otherSession);
        await sqlite.DbContext.SaveChangesAsync();

        var result = await repository.GetByTutorIdAsync(tutorId);

        Assert.Single(result, s => s.Id == ownSession.Id);
        Assert.DoesNotContain(result, s => s.Id == otherSession.Id);
    }

    [Fact]
    public async Task GetByStudentIdAsync_returns_only_that_students_sessions()
    {
        using var sqlite = new SqliteTestDbContext();
        var repository = new SessionRepository(sqlite.DbContext);

        var studentId = StudentId.From(Guid.NewGuid());
        var slot1 = DeclareSlot(TutorId.From(Guid.NewGuid()));
        var slot2 = DeclareSlot(TutorId.From(Guid.NewGuid()));
        sqlite.DbContext.AvailabilitySlots.AddRange(slot1, slot2);

        var ownSession = slot1.Book(studentId, null);
        var otherSession = slot2.Book(StudentId.From(Guid.NewGuid()), null);
        await repository.AddAsync(ownSession);
        await repository.AddAsync(otherSession);
        await sqlite.DbContext.SaveChangesAsync();

        var result = await repository.GetByStudentIdAsync(studentId);

        Assert.Single(result, s => s.Id == ownSession.Id);
        Assert.DoesNotContain(result, s => s.Id == otherSession.Id);
    }

    [Fact]
    public async Task GetAllAsync_returns_sessions_across_every_tutor_and_student_and_reports_total_count()
    {
        using var sqlite = new SqliteTestDbContext();
        var repository = new SessionRepository(sqlite.DbContext);

        var slot1 = DeclareSlot(TutorId.From(Guid.NewGuid()));
        var slot2 = DeclareSlot(TutorId.From(Guid.NewGuid()));
        sqlite.DbContext.AvailabilitySlots.AddRange(slot1, slot2);

        var sessionA = slot1.Book(StudentId.From(Guid.NewGuid()), null);
        var sessionB = slot2.Book(StudentId.From(Guid.NewGuid()), null);
        await repository.AddAsync(sessionA);
        await repository.AddAsync(sessionB);
        await sqlite.DbContext.SaveChangesAsync();

        var (items, totalCount) = await repository.GetAllAsync(new PageRequest(1, 20));

        Assert.Equal(2, totalCount);
        Assert.Contains(items, s => s.Id == sessionA.Id);
        Assert.Contains(items, s => s.Id == sessionB.Id);
    }

    [Fact]
    public async Task GetAllAsync_paginates_the_result()
    {
        using var sqlite = new SqliteTestDbContext();
        var repository = new SessionRepository(sqlite.DbContext);

        for (var i = 0; i < 3; i++)
        {
            var slot = DeclareSlot(TutorId.From(Guid.NewGuid()));
            sqlite.DbContext.AvailabilitySlots.Add(slot);
            var session = slot.Book(StudentId.From(Guid.NewGuid()), null);
            await repository.AddAsync(session);
        }

        await sqlite.DbContext.SaveChangesAsync();

        var (items, totalCount) = await repository.GetAllAsync(new PageRequest(1, 2));

        Assert.Equal(3, totalCount);
        Assert.Equal(2, items.Count);
    }

    [Fact]
    public async Task GetStatusCountsAsync_groups_sessions_by_status()
    {
        using var sqlite = new SqliteTestDbContext();
        var repository = new SessionRepository(sqlite.DbContext);

        var scheduledSlot = DeclareSlot(TutorId.From(Guid.NewGuid()));
        var completedSlot = DeclareSlot(TutorId.From(Guid.NewGuid()));
        var cancelledSlot = DeclareSlot(TutorId.From(Guid.NewGuid()));
        var noShowSlot = DeclareSlot(TutorId.From(Guid.NewGuid()));
        sqlite.DbContext.AvailabilitySlots.AddRange(scheduledSlot, completedSlot, cancelledSlot, noShowSlot);

        var scheduled = scheduledSlot.Book(StudentId.From(Guid.NewGuid()), null);
        var completed = completedSlot.Book(StudentId.From(Guid.NewGuid()), null);
        completed.Complete();
        var cancelled = cancelledSlot.Book(StudentId.From(Guid.NewGuid()), null);
        cancelled.Cancel();
        var noShow = noShowSlot.Book(StudentId.From(Guid.NewGuid()), null);
        noShow.MarkNoShow();

        await repository.AddAsync(scheduled);
        await repository.AddAsync(completed);
        await repository.AddAsync(cancelled);
        await repository.AddAsync(noShow);
        await sqlite.DbContext.SaveChangesAsync();

        var counts = await repository.GetStatusCountsAsync();

        Assert.Equal(1, counts[SessionStatus.Scheduled]);
        Assert.Equal(1, counts[SessionStatus.Completed]);
        Assert.Equal(1, counts[SessionStatus.Cancelled]);
        Assert.Equal(1, counts[SessionStatus.NoShow]);
    }
}
