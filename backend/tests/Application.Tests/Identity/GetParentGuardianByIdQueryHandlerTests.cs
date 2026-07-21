using TutorFlow.Application.Identity.Handlers;
using TutorFlow.Application.Identity.Queries;
using TutorFlow.Application.Tests.TestDoubles;
using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Tests.Identity;

public class GetParentGuardianByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_returns_parent_guardian_for_themselves()
    {
        var parentGuardianRepository = new InMemoryParentGuardianRepository();
        var relationshipRepository = new InMemoryRelationshipRepository();
        var parentGuardian = ParentGuardian.Register(TestCredentials.Email(), TestCredentials.Hash());
        await parentGuardianRepository.AddAsync(parentGuardian);
        var handler = new GetParentGuardianByIdQueryHandler(
            parentGuardianRepository, relationshipRepository, StubCurrentUserProvider.AsParentGuardian(parentGuardian.Id.Value));

        var result = await handler.Handle(new GetParentGuardianByIdQuery(parentGuardian.Id.Value));

        Assert.True(result.IsSuccess);
        Assert.Equal(parentGuardian.Id.Value, result.Value.ParentGuardianId);
    }

    [Fact]
    public async Task Handle_returns_parent_guardian_for_admin()
    {
        var parentGuardianRepository = new InMemoryParentGuardianRepository();
        var relationshipRepository = new InMemoryRelationshipRepository();
        var parentGuardian = ParentGuardian.Register(TestCredentials.Email(), TestCredentials.Hash());
        await parentGuardianRepository.AddAsync(parentGuardian);
        var handler = new GetParentGuardianByIdQueryHandler(
            parentGuardianRepository, relationshipRepository, StubCurrentUserProvider.AsAdminStaff(Guid.NewGuid()));

        var result = await handler.Handle(new GetParentGuardianByIdQuery(parentGuardian.Id.Value));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_returns_parent_guardian_for_the_linked_student_with_a_confirmed_relationship()
    {
        var parentGuardianRepository = new InMemoryParentGuardianRepository();
        var relationshipRepository = new InMemoryRelationshipRepository();
        var parentGuardian = ParentGuardian.Register(TestCredentials.Email(), TestCredentials.Hash());
        await parentGuardianRepository.AddAsync(parentGuardian);
        var studentId = AccountId.New();
        var relationship = Relationship.Invite(parentGuardian.Id, studentId, parentGuardian.Id);
        relationship.Confirm();
        await relationshipRepository.AddAsync(relationship);
        var handler = new GetParentGuardianByIdQueryHandler(
            parentGuardianRepository, relationshipRepository, StubCurrentUserProvider.AsStudent(studentId.Value));

        var result = await handler.Handle(new GetParentGuardianByIdQuery(parentGuardian.Id.Value));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_returns_unauthenticated_error_for_an_anonymous_caller()
    {
        var parentGuardianRepository = new InMemoryParentGuardianRepository();
        var relationshipRepository = new InMemoryRelationshipRepository();
        var parentGuardian = ParentGuardian.Register(TestCredentials.Email(), TestCredentials.Hash());
        await parentGuardianRepository.AddAsync(parentGuardian);
        var handler = new GetParentGuardianByIdQueryHandler(
            parentGuardianRepository, relationshipRepository, StubCurrentUserProvider.Unauthenticated());

        var result = await handler.Handle(new GetParentGuardianByIdQuery(parentGuardian.Id.Value));

        Assert.True(result.IsFailure);
        Assert.Equal(TutorFlow.Application.Common.ErrorType.Authorization, result.Error.Type);
        Assert.Equal("GetParentGuardianByIdQuery.Unauthenticated", result.Error.Code);
    }

    [Fact]
    public async Task Handle_returns_forbidden_for_an_unrelated_student()
    {
        var parentGuardianRepository = new InMemoryParentGuardianRepository();
        var relationshipRepository = new InMemoryRelationshipRepository();
        var parentGuardian = ParentGuardian.Register(TestCredentials.Email(), TestCredentials.Hash());
        await parentGuardianRepository.AddAsync(parentGuardian);
        var handler = new GetParentGuardianByIdQueryHandler(
            parentGuardianRepository, relationshipRepository, StubCurrentUserProvider.AsStudent(Guid.NewGuid()));

        var result = await handler.Handle(new GetParentGuardianByIdQuery(parentGuardian.Id.Value));

        Assert.True(result.IsFailure);
        Assert.Equal(TutorFlow.Application.Common.ErrorType.Authorization, result.Error.Type);
    }

    [Fact]
    public async Task Handle_returns_not_found_for_unknown_parent_guardian()
    {
        var parentGuardianRepository = new InMemoryParentGuardianRepository();
        var relationshipRepository = new InMemoryRelationshipRepository();
        var handler = new GetParentGuardianByIdQueryHandler(
            parentGuardianRepository, relationshipRepository, StubCurrentUserProvider.AsAdminStaff(Guid.NewGuid()));

        var result = await handler.Handle(new GetParentGuardianByIdQuery(Guid.NewGuid()));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_returns_failure_for_empty_id()
    {
        var parentGuardianRepository = new InMemoryParentGuardianRepository();
        var relationshipRepository = new InMemoryRelationshipRepository();
        var handler = new GetParentGuardianByIdQueryHandler(
            parentGuardianRepository, relationshipRepository, StubCurrentUserProvider.AsAdminStaff(Guid.NewGuid()));

        var result = await handler.Handle(new GetParentGuardianByIdQuery(Guid.Empty));

        Assert.True(result.IsFailure);
    }
}
