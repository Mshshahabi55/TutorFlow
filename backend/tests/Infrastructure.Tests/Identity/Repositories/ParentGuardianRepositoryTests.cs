using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Identity.ValueObjects;
using TutorFlow.Infrastructure.Identity.Repositories;
using TutorFlow.Infrastructure.Tests.Persistence;

namespace TutorFlow.Infrastructure.Tests.Identity.Repositories;

public class ParentGuardianRepositoryTests
{
    [Fact]
    public async Task AddAsync_then_GetByIdAsync_returns_the_same_parent_guardian()
    {
        using var sqlite = new SqliteTestDbContext();
        var repository = new ParentGuardianRepository(sqlite.DbContext);
        var parentGuardian = ParentGuardian.Register(TestCredentials.Email(), TestCredentials.Hash());

        await repository.AddAsync(parentGuardian);
        await sqlite.DbContext.SaveChangesAsync();

        var stored = await repository.GetByIdAsync(parentGuardian.Id);
        Assert.NotNull(stored);
        Assert.Equal(parentGuardian.Id, stored!.Id);
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_for_unknown_id()
    {
        using var sqlite = new SqliteTestDbContext();
        var repository = new ParentGuardianRepository(sqlite.DbContext);

        var result = await repository.GetByIdAsync(AccountId.New());

        Assert.Null(result);
    }
}
