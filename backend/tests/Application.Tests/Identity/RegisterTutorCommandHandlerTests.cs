using TutorFlow.Application.Identity.Commands;
using TutorFlow.Application.Identity.Handlers;
using TutorFlow.Application.Tests.TestDoubles;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Tests.Identity;

public class RegisterTutorCommandHandlerTests
{
    [Fact]
    public async Task Handle_adds_tutor_to_repository_and_calls_SaveChanges()
    {
        var repository = new InMemoryTutorRepository();
        var unitOfWork = new FakeUnitOfWork();
        var passwordHasher = new FakePasswordHasher();
        var handler = new RegisterTutorCommandHandler(repository, unitOfWork, passwordHasher);

        var result = await handler.Handle(new RegisterTutorCommand(TestCredentials.Email().Value, "Test-Password-123!"));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);

        var stored = await repository.GetByIdAsync(AccountId.From(result.Value.TutorId));
        Assert.NotNull(stored);
        Assert.Empty(stored.DomainEvents);
    }
}
