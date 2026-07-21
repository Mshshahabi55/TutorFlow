using TutorFlow.Application.Identity.Handlers;
using TutorFlow.Application.Identity.Queries;
using TutorFlow.Application.Tests.TestDoubles;
using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Tests.Identity;

public class GetStudentByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_returns_student_for_the_student_themselves()
    {
        var studentRepository = new InMemoryStudentRepository();
        var relationshipRepository = new InMemoryRelationshipRepository();
        var student = Student.Register(TestCredentials.Email(), TestCredentials.Hash(), isMinor: false);
        await studentRepository.AddAsync(student);
        var handler = new GetStudentByIdQueryHandler(
            studentRepository, relationshipRepository, StubCurrentUserProvider.AsStudent(student.Id.Value));

        var result = await handler.Handle(new GetStudentByIdQuery(student.Id.Value));

        Assert.True(result.IsSuccess);
        Assert.Equal(student.Id.Value, result.Value.StudentId);
    }

    [Fact]
    public async Task Handle_returns_student_for_admin()
    {
        var studentRepository = new InMemoryStudentRepository();
        var relationshipRepository = new InMemoryRelationshipRepository();
        var student = Student.Register(TestCredentials.Email(), TestCredentials.Hash(), isMinor: false);
        await studentRepository.AddAsync(student);
        var handler = new GetStudentByIdQueryHandler(
            studentRepository, relationshipRepository, StubCurrentUserProvider.AsAdminStaff(Guid.NewGuid()));

        var result = await handler.Handle(new GetStudentByIdQuery(student.Id.Value));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_returns_student_for_a_parent_guardian_with_a_confirmed_relationship()
    {
        var studentRepository = new InMemoryStudentRepository();
        var relationshipRepository = new InMemoryRelationshipRepository();
        var student = Student.Register(TestCredentials.Email(), TestCredentials.Hash(), isMinor: false);
        await studentRepository.AddAsync(student);
        var parentGuardianId = AccountId.New();
        var relationship = Relationship.Invite(parentGuardianId, student.Id, parentGuardianId);
        relationship.Confirm();
        await relationshipRepository.AddAsync(relationship);
        var handler = new GetStudentByIdQueryHandler(
            studentRepository, relationshipRepository, StubCurrentUserProvider.AsParentGuardian(parentGuardianId.Value));

        var result = await handler.Handle(new GetStudentByIdQuery(student.Id.Value));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_returns_forbidden_for_a_parent_guardian_without_a_confirmed_relationship()
    {
        var studentRepository = new InMemoryStudentRepository();
        var relationshipRepository = new InMemoryRelationshipRepository();
        var student = Student.Register(TestCredentials.Email(), TestCredentials.Hash(), isMinor: false);
        await studentRepository.AddAsync(student);
        var parentGuardianId = AccountId.New();
        var relationship = Relationship.Invite(parentGuardianId, student.Id, parentGuardianId);
        // Not confirmed.
        await relationshipRepository.AddAsync(relationship);
        var handler = new GetStudentByIdQueryHandler(
            studentRepository, relationshipRepository, StubCurrentUserProvider.AsParentGuardian(parentGuardianId.Value));

        var result = await handler.Handle(new GetStudentByIdQuery(student.Id.Value));

        Assert.True(result.IsFailure);
        Assert.Equal(TutorFlow.Application.Common.ErrorType.Authorization, result.Error.Type);
    }

    [Fact]
    public async Task Handle_returns_forbidden_for_an_unauthenticated_caller()
    {
        var studentRepository = new InMemoryStudentRepository();
        var relationshipRepository = new InMemoryRelationshipRepository();
        var student = Student.Register(TestCredentials.Email(), TestCredentials.Hash(), isMinor: false);
        await studentRepository.AddAsync(student);
        var handler = new GetStudentByIdQueryHandler(
            studentRepository, relationshipRepository, StubCurrentUserProvider.Unauthenticated());

        var result = await handler.Handle(new GetStudentByIdQuery(student.Id.Value));

        Assert.True(result.IsFailure);
        Assert.Equal(TutorFlow.Application.Common.ErrorType.Authorization, result.Error.Type);
    }

    [Fact]
    public async Task Handle_returns_not_found_for_unknown_student()
    {
        var studentRepository = new InMemoryStudentRepository();
        var relationshipRepository = new InMemoryRelationshipRepository();
        var handler = new GetStudentByIdQueryHandler(
            studentRepository, relationshipRepository, StubCurrentUserProvider.AsAdminStaff(Guid.NewGuid()));

        var result = await handler.Handle(new GetStudentByIdQuery(Guid.NewGuid()));

        Assert.True(result.IsFailure);
    }
}
