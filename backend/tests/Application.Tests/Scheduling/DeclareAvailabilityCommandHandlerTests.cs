using TutorFlow.Application.Scheduling.Commands;
using TutorFlow.Application.Scheduling.Handlers;
using TutorFlow.Application.Tests.TestDoubles;
using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Application.Tests.Scheduling;

public class DeclareAvailabilityCommandHandlerTests
{
    [Fact]
    public async Task Handle_adds_slot_to_repository_and_saves()
    {
        var repository = new InMemoryAvailabilitySlotRepository();
        var unitOfWork = new FakeUnitOfWork();
        var tutorId = Guid.NewGuid();
        var handler = new DeclareAvailabilityCommandHandler(repository, StubCurrentUserProvider.AsTutor(tutorId), unitOfWork);

        var result = await handler.Handle(new DeclareAvailabilityCommand(
            tutorId,
            DateTime.UtcNow.AddDays(1),
            TimeSpan.FromHours(1),
            DeliveryMode.Online));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);

        var stored = await repository.GetByIdAsync(AvailabilitySlotId.From(result.Value.AvailabilitySlotId));
        Assert.NotNull(stored);
        Assert.Empty(stored.DomainEvents);
    }

    [Fact]
    public async Task Handle_returns_failure_for_empty_tutor_id_without_touching_repository()
    {
        var repository = new InMemoryAvailabilitySlotRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new DeclareAvailabilityCommandHandler(repository, StubCurrentUserProvider.AsTutor(Guid.NewGuid()), unitOfWork);

        var result = await handler.Handle(new DeclareAvailabilityCommand(
            Guid.Empty,
            DateTime.UtcNow.AddDays(1),
            TimeSpan.FromHours(1),
            DeliveryMode.Online));

        Assert.True(result.IsFailure);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_returns_forbidden_when_caller_is_not_the_claimed_tutor()
    {
        var repository = new InMemoryAvailabilitySlotRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new DeclareAvailabilityCommandHandler(repository, StubCurrentUserProvider.AsTutor(Guid.NewGuid()), unitOfWork);

        var result = await handler.Handle(new DeclareAvailabilityCommand(
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(1),
            TimeSpan.FromHours(1),
            DeliveryMode.Online));

        Assert.True(result.IsFailure);
        Assert.Equal(TutorFlow.Application.Common.ErrorType.Authorization, result.Error.Type);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }
}
