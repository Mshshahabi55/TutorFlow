using TutorFlow.Application.Identity.Commands;
using TutorFlow.Application.Identity.Handlers;
using TutorFlow.Application.Tests.TestDoubles;
using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Tests.Identity;

public class ConfirmRelationshipCommandHandlerTests
{
    [Fact]
    public async Task Handle_confirms_existing_relationship_and_calls_SaveChanges()
    {
        var repository = new InMemoryRelationshipRepository();
        var unitOfWork = new FakeUnitOfWork();
        var parentGuardianId = AccountId.New();
        var studentId = AccountId.New();
        var relationship = Relationship.Invite(parentGuardianId, studentId, parentGuardianId);
        await repository.AddAsync(relationship);
        // The counterparty (Student) — not the inviter (Parent/Guardian) —
        // confirms (AUTHORIZATION_MATRIX.md §4.1).
        var handler = new ConfirmRelationshipCommandHandler(
            repository, StubCurrentUserProvider.AsStudent(studentId.Value), unitOfWork);

        var result = await handler.Handle(new ConfirmRelationshipCommand(relationship.Id.Value));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_returns_failure_when_relationship_not_found()
    {
        var repository = new InMemoryRelationshipRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new ConfirmRelationshipCommandHandler(
            repository, StubCurrentUserProvider.AsStudent(Guid.NewGuid()), unitOfWork);

        var result = await handler.Handle(new ConfirmRelationshipCommand(Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_translates_domain_error_into_failure()
    {
        var repository = new InMemoryRelationshipRepository();
        var unitOfWork = new FakeUnitOfWork();
        var parentGuardianId = AccountId.New();
        var studentId = AccountId.New();
        var relationship = Relationship.Invite(parentGuardianId, studentId, parentGuardianId);
        relationship.Confirm();
        await repository.AddAsync(relationship);
        var handler = new ConfirmRelationshipCommandHandler(
            repository, StubCurrentUserProvider.AsStudent(studentId.Value), unitOfWork);

        var result = await handler.Handle(new ConfirmRelationshipCommand(relationship.Id.Value));

        Assert.True(result.IsFailure);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_returns_forbidden_when_the_inviter_attempts_to_confirm()
    {
        var repository = new InMemoryRelationshipRepository();
        var unitOfWork = new FakeUnitOfWork();
        var parentGuardianId = AccountId.New();
        var studentId = AccountId.New();
        var relationship = Relationship.Invite(parentGuardianId, studentId, parentGuardianId);
        await repository.AddAsync(relationship);
        var handler = new ConfirmRelationshipCommandHandler(
            repository, StubCurrentUserProvider.AsParentGuardian(parentGuardianId.Value), unitOfWork);

        var result = await handler.Handle(new ConfirmRelationshipCommand(relationship.Id.Value));

        Assert.True(result.IsFailure);
        Assert.Equal(TutorFlow.Application.Common.ErrorType.Authorization, result.Error.Type);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_returns_forbidden_when_caller_is_neither_named_party()
    {
        var repository = new InMemoryRelationshipRepository();
        var unitOfWork = new FakeUnitOfWork();
        var parentGuardianId = AccountId.New();
        var studentId = AccountId.New();
        var relationship = Relationship.Invite(parentGuardianId, studentId, parentGuardianId);
        await repository.AddAsync(relationship);
        var handler = new ConfirmRelationshipCommandHandler(
            repository, StubCurrentUserProvider.AsStudent(Guid.NewGuid()), unitOfWork);

        var result = await handler.Handle(new ConfirmRelationshipCommand(relationship.Id.Value));

        Assert.True(result.IsFailure);
        Assert.Equal(TutorFlow.Application.Common.ErrorType.Authorization, result.Error.Type);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }
}
