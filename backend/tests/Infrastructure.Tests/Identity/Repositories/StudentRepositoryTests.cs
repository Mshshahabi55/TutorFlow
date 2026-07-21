using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Identity.ValueObjects;
using TutorFlow.Infrastructure.Identity.Repositories;
using TutorFlow.Infrastructure.Tests.Persistence;

namespace TutorFlow.Infrastructure.Tests.Identity.Repositories;

public class StudentRepositoryTests
{
    [Fact]
    public async Task AddAsync_then_GetByIdAsync_returns_the_same_student()
    {
        using var sqlite = new SqliteTestDbContext();
        var repository = new StudentRepository(sqlite.DbContext);
        var student = Student.Register(TestCredentials.Email(), TestCredentials.Hash(), isMinor: true);

        await repository.AddAsync(student);
        await sqlite.DbContext.SaveChangesAsync();

        var stored = await repository.GetByIdAsync(student.Id);
        Assert.NotNull(stored);
        Assert.Equal(student.Id, stored!.Id);
        Assert.True(stored.IsMinor);
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_for_unknown_id()
    {
        using var sqlite = new SqliteTestDbContext();
        var repository = new StudentRepository(sqlite.DbContext);

        var result = await repository.GetByIdAsync(AccountId.New());

        Assert.Null(result);
    }
}
