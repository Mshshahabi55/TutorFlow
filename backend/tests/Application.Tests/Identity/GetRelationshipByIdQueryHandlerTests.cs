using TutorFlow.Application.Identity.Handlers;
using TutorFlow.Application.Identity.Queries;
using TutorFlow.Application.Tests.TestDoubles;
using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Tests.Identity;

public class GetRelationshipByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_returns_relationship_for_the_parent_guardian_party_while_still_invited()
    {
        var repository = new InMemoryRelationshipRepository();
        var parentGuardianId = AccountId.New();
        var relationship = Relationship.Invite(parentGuardianId, AccountId.New(), parentGuardianId);
        await repository.AddAsync(relationship);
        var handler = new GetRelationshipByIdQueryHandler(
            repository, StubCurrentUserProvider.AsParentGuardian(parentGuardianId.Value));

        var result = await handler.Handle(new GetRelationshipByIdQuery(relationship.Id.Value));

        Assert.True(result.IsSuccess);
        Assert.Equal(relationship.Id.Value, result.Value.RelationshipId);
    }

    [Fact]
    public async Task Handle_returns_relationship_for_the_student_party()
    {
        var repository = new InMemoryRelationshipRepository();
        var studentId = AccountId.New();
        var relationship = Relationship.Invite(AccountId.New(), studentId, studentId);
        await repository.AddAsync(relationship);
        var handler = new GetRelationshipByIdQueryHandler(
            repository, StubCurrentUserProvider.AsStudent(studentId.Value));

        var result = await handler.Handle(new GetRelationshipByIdQuery(relationship.Id.Value));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_returns_relationship_for_admin()
    {
        var repository = new InMemoryRelationshipRepository();
        var relationship = Relationship.Invite(AccountId.New(), AccountId.New(), AccountId.New());
        await repository.AddAsync(relationship);
        var handler = new GetRelationshipByIdQueryHandler(
            repository, StubCurrentUserProvider.AsAdminStaff(Guid.NewGuid()));

        var result = await handler.Handle(new GetRelationshipByIdQuery(relationship.Id.Value));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_returns_forbidden_for_an_unrelated_caller()
    {
        var repository = new InMemoryRelationshipRepository();
        var relationship = Relationship.Invite(AccountId.New(), AccountId.New(), AccountId.New());
        await repository.AddAsync(relationship);
        var handler = new GetRelationshipByIdQueryHandler(
            repository, StubCurrentUserProvider.AsStudent(Guid.NewGuid()));

        var result = await handler.Handle(new GetRelationshipByIdQuery(relationship.Id.Value));

        Assert.True(result.IsFailure);
        Assert.Equal(TutorFlow.Application.Common.ErrorType.Authorization, result.Error.Type);
    }

    [Fact]
    public async Task Handle_returns_unauthenticated_error_for_an_anonymous_caller()
    {
        var repository = new InMemoryRelationshipRepository();
        var relationship = Relationship.Invite(AccountId.New(), AccountId.New(), AccountId.New());
        await repository.AddAsync(relationship);
        var handler = new GetRelationshipByIdQueryHandler(repository, StubCurrentUserProvider.Unauthenticated());

        var result = await handler.Handle(new GetRelationshipByIdQuery(relationship.Id.Value));

        Assert.True(result.IsFailure);
        Assert.Equal("GetRelationshipByIdQuery.Unauthenticated", result.Error.Code);
    }

    [Fact]
    public async Task Handle_returns_not_found_for_unknown_relationship()
    {
        var repository = new InMemoryRelationshipRepository();
        var handler = new GetRelationshipByIdQueryHandler(repository, StubCurrentUserProvider.AsAdminStaff(Guid.NewGuid()));

        var result = await handler.Handle(new GetRelationshipByIdQuery(Guid.NewGuid()));

        Assert.True(result.IsFailure);
    }
}
