using TutorFlow.Application.Identity.Commands;
using TutorFlow.Application.Identity.Handlers;
using TutorFlow.Application.Tests.TestDoubles;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Tests.Identity;

public class RegisterParentGuardianCommandHandlerTests
{
    [Fact]
    public async Task Handle_adds_parent_guardian_to_repository_and_saves()
    {
        var repository = new InMemoryParentGuardianRepository();
        var unitOfWork = new FakeUnitOfWork();
        var passwordHasher = new FakePasswordHasher();
        var handler = new RegisterParentGuardianCommandHandler(repository, unitOfWork, passwordHasher);

        var result = await handler.Handle(
            new RegisterParentGuardianCommand(TestCredentials.Email().Value, "Test-Password-123!"));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);

        var stored = await repository.GetByIdAsync(AccountId.From(result.Value.ParentGuardianId));
        Assert.NotNull(stored);
    }
}
