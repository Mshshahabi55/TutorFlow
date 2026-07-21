using TutorFlow.Application.Identity.Handlers;
using TutorFlow.Application.Identity.Queries;
using TutorFlow.Application.Tests.TestDoubles;
using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Tests.Identity;

public class GetRelationshipsByAccountIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_returns_relationships_for_the_inviting_parent_guardian_themselves()
    {
        var repository = new InMemoryRelationshipRepository();
        var parentGuardianId = AccountId.New();
        var relationship = Relationship.Invite(parentGuardianId, AccountId.New(), parentGuardianId);
        await repository.AddAsync(relationship);
        var handler = new GetRelationshipsByAccountIdQueryHandler(
            repository, StubCurrentUserProvider.AsParentGuardian(parentGuardianId.Value));

        var result = await handler.Handle(new GetRelationshipsByAccountIdQuery(parentGuardianId.Value));

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value, r => r.RelationshipId == relationship.Id.Value);
    }

    [Fact]
    public async Task Handle_returns_relationships_for_the_invited_student_themselves()
    {
        var repository = new InMemoryRelationshipRepository();
        var studentId = AccountId.New();
        var relationship = Relationship.Invite(AccountId.New(), studentId, studentId);
        await repository.AddAsync(relationship);
        var handler = new GetRelationshipsByAccountIdQueryHandler(
            repository, StubCurrentUserProvider.AsStudent(studentId.Value));

        var result = await handler.Handle(new GetRelationshipsByAccountIdQuery(studentId.Value));

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value, r => r.RelationshipId == relationship.Id.Value);
    }

    [Fact]
    public async Task Handle_returns_relationships_for_admin()
    {
        var repository = new InMemoryRelationshipRepository();
        var accountId = AccountId.New();
        var handler = new GetRelationshipsByAccountIdQueryHandler(
            repository, StubCurrentUserProvider.AsAdminStaff(Guid.NewGuid()));

        var result = await handler.Handle(new GetRelationshipsByAccountIdQuery(accountId.Value));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_returns_forbidden_for_a_different_account()
    {
        var repository = new InMemoryRelationshipRepository();
        var accountId = AccountId.New();
        var handler = new GetRelationshipsByAccountIdQueryHandler(
            repository, StubCurrentUserProvider.AsStudent(Guid.NewGuid()));

        var result = await handler.Handle(new GetRelationshipsByAccountIdQuery(accountId.Value));

        Assert.True(result.IsFailure);
        Assert.Equal(TutorFlow.Application.Common.ErrorType.Authorization, result.Error.Type);
    }

    [Fact]
    public async Task Handle_returns_unauthenticated_error_for_an_anonymous_caller()
    {
        var repository = new InMemoryRelationshipRepository();
        var accountId = AccountId.New();
        var handler = new GetRelationshipsByAccountIdQueryHandler(repository, StubCurrentUserProvider.Unauthenticated());

        var result = await handler.Handle(new GetRelationshipsByAccountIdQuery(accountId.Value));

        Assert.True(result.IsFailure);
        Assert.Equal("GetRelationshipsByAccountIdQuery.Unauthenticated", result.Error.Code);
    }

    [Fact]
    public async Task Handle_returns_empty_collection_for_an_account_with_no_relationships()
    {
        var repository = new InMemoryRelationshipRepository();
        var accountId = AccountId.New();
        var handler = new GetRelationshipsByAccountIdQueryHandler(
            repository, StubCurrentUserProvider.AsStudent(accountId.Value));

        var result = await handler.Handle(new GetRelationshipsByAccountIdQuery(accountId.Value));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task Handle_returns_failure_for_empty_account_id()
    {
        var repository = new InMemoryRelationshipRepository();
        var handler = new GetRelationshipsByAccountIdQueryHandler(
            repository, StubCurrentUserProvider.AsAdminStaff(Guid.NewGuid()));

        var result = await handler.Handle(new GetRelationshipsByAccountIdQuery(Guid.Empty));

        Assert.True(result.IsFailure);
    }
}
