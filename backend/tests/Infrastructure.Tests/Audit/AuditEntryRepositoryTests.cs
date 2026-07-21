using TutorFlow.Application.Common;
using TutorFlow.Infrastructure.Audit;
using TutorFlow.Infrastructure.Tests.Persistence;

namespace TutorFlow.Infrastructure.Tests.Audit;

public class AuditEntryRepositoryTests
{
    [Fact]
    public async Task GetAllAsync_returns_entries_ordered_most_recent_first()
    {
        using var sqlite = new SqliteTestDbContext();
        var older = new AuditEntry(Guid.NewGuid(), DateTime.UtcNow.AddMinutes(-10), "SessionBooked", Guid.NewGuid(), "actor-1", "Tutor");
        var newer = new AuditEntry(Guid.NewGuid(), DateTime.UtcNow, "TutorApproved", Guid.NewGuid(), "actor-2", "AdminStaff");
        sqlite.DbContext.Add(older);
        sqlite.DbContext.Add(newer);
        await sqlite.DbContext.SaveChangesAsync();

        var repository = new AuditEntryRepository(sqlite.DbContext);

        var (items, totalCount) = await repository.GetAllAsync(new PageRequest(1, 20), subjectId: null);

        Assert.Equal(2, totalCount);
        var ordered = items.ToList();
        Assert.Equal(newer.Id, ordered[0].Id);
        Assert.Equal(older.Id, ordered[1].Id);
    }

    [Fact]
    public async Task GetAllAsync_filters_by_subject_id_when_provided()
    {
        using var sqlite = new SqliteTestDbContext();
        var subjectId = Guid.NewGuid();
        var matching = new AuditEntry(Guid.NewGuid(), DateTime.UtcNow, "SessionBooked", subjectId, "actor-1", "Tutor");
        var other = new AuditEntry(Guid.NewGuid(), DateTime.UtcNow, "SessionBooked", Guid.NewGuid(), "actor-1", "Tutor");
        sqlite.DbContext.Add(matching);
        sqlite.DbContext.Add(other);
        await sqlite.DbContext.SaveChangesAsync();

        var repository = new AuditEntryRepository(sqlite.DbContext);

        var (items, totalCount) = await repository.GetAllAsync(new PageRequest(1, 20), subjectId);

        Assert.Equal(1, totalCount);
        Assert.Equal(matching.Id, items.Single().Id);
    }

    [Fact]
    public async Task GetAllAsync_paginates_the_result()
    {
        using var sqlite = new SqliteTestDbContext();
        for (var i = 0; i < 3; i++)
        {
            sqlite.DbContext.Add(new AuditEntry(
                Guid.NewGuid(), DateTime.UtcNow.AddMinutes(-i), "SessionBooked", Guid.NewGuid(), "actor-1", "Tutor"));
        }
        await sqlite.DbContext.SaveChangesAsync();

        var repository = new AuditEntryRepository(sqlite.DbContext);

        var (items, totalCount) = await repository.GetAllAsync(new PageRequest(1, 2), subjectId: null);

        Assert.Equal(3, totalCount);
        Assert.Equal(2, items.Count);
    }

    [Fact]
    public async Task GetAllAsync_returns_empty_when_no_entries_exist()
    {
        using var sqlite = new SqliteTestDbContext();
        var repository = new AuditEntryRepository(sqlite.DbContext);

        var (items, totalCount) = await repository.GetAllAsync(new PageRequest(1, 20), subjectId: null);

        Assert.Empty(items);
        Assert.Equal(0, totalCount);
    }
}
