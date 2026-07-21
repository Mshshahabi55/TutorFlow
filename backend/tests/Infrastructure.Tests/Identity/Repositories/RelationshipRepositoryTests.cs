using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Identity.ValueObjects;
using TutorFlow.Infrastructure.Identity.Repositories;
using TutorFlow.Infrastructure.Tests.Persistence;

namespace TutorFlow.Infrastructure.Tests.Identity.Repositories;

public class RelationshipRepositoryTests
{
    [Fact]
    public async Task AddAsync_then_GetByIdAsync_returns_the_same_relationship()
    {
        using var sqlite = new SqliteTestDbContext();
        var repository = new RelationshipRepository(sqlite.DbContext);
        var relationship = Relationship.Invite(AccountId.New(), AccountId.New(), AccountId.New());

        await repository.AddAsync(relationship);
        await sqlite.DbContext.SaveChangesAsync();

        var stored = await repository.GetByIdAsync(relationship.Id);
        Assert.NotNull(stored);
        Assert.Equal(relationship.Id, stored!.Id);
        Assert.Equal(RelationshipStatus.Invited, stored.Status);
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_for_unknown_id()
    {
        using var sqlite = new SqliteTestDbContext();
        var repository = new RelationshipRepository(sqlite.DbContext);

        var result = await repository.GetByIdAsync(RelationshipId.New());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByAccountIdAsync_returns_relationships_for_either_party()
    {
        using var sqlite = new SqliteTestDbContext();
        var repository = new RelationshipRepository(sqlite.DbContext);

        var parentGuardianId = AccountId.New();
        var studentId = AccountId.New();
        var asParent = Relationship.Invite(parentGuardianId, AccountId.New(), parentGuardianId);
        var asStudent = Relationship.Invite(AccountId.New(), studentId, studentId);
        var unrelated = Relationship.Invite(AccountId.New(), AccountId.New(), AccountId.New());

        await repository.AddAsync(asParent);
        await repository.AddAsync(asStudent);
        await repository.AddAsync(unrelated);
        await sqlite.DbContext.SaveChangesAsync();

        var parentResult = await repository.GetByAccountIdAsync(parentGuardianId);
        var studentResult = await repository.GetByAccountIdAsync(studentId);

        Assert.Single(parentResult, r => r.Id == asParent.Id);
        Assert.Single(studentResult, r => r.Id == asStudent.Id);
    }

    [Fact]
    public async Task GetByAccountIdAsync_returns_empty_collection_for_an_unrelated_account()
    {
        using var sqlite = new SqliteTestDbContext();
        var repository = new RelationshipRepository(sqlite.DbContext);
        var relationship = Relationship.Invite(AccountId.New(), AccountId.New(), AccountId.New());
        await repository.AddAsync(relationship);
        await sqlite.DbContext.SaveChangesAsync();

        var result = await repository.GetByAccountIdAsync(AccountId.New());

        Assert.Empty(result);
    }
}
