using TutorFlow.Application.Identity.Commands;
using TutorFlow.Application.Identity.Handlers;
using TutorFlow.Application.Tests.TestDoubles;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Tests.Identity;

public class RegisterStudentCommandHandlerTests
{
    [Fact]
    public async Task Handle_adds_student_to_repository_and_calls_SaveChanges()
    {
        var repository = new InMemoryStudentRepository();
        var unitOfWork = new FakeUnitOfWork();
        var passwordHasher = new FakePasswordHasher();
        var handler = new RegisterStudentCommandHandler(repository, unitOfWork, passwordHasher);

        var result = await handler.Handle(
            new RegisterStudentCommand(TestCredentials.Email().Value, "Test-Password-123!", IsMinor: false));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);

        var stored = await repository.GetByIdAsync(AccountId.From(result.Value.StudentId));
        Assert.NotNull(stored);
    }
}
