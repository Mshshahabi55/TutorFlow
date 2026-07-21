using TutorFlow.Application.Identity.Commands;
using TutorFlow.Application.Identity.Handlers;
using TutorFlow.Application.Tests.TestDoubles;
using TutorFlow.Domain.Identity;

namespace TutorFlow.Application.Tests.Identity;

public class CreateRelationshipInvitationCommandHandlerTests
{
    private static (
        InMemoryRelationshipRepository RelationshipRepository,
        InMemoryParentGuardianRepository ParentGuardianRepository,
        InMemoryStudentRepository StudentRepository,
        FakeUnitOfWork UnitOfWork,
        CreateRelationshipInvitationCommandHandler Handler) CreateSut(Guid callerId, string callerRole)
    {
        var relationshipRepository = new InMemoryRelationshipRepository();
        var parentGuardianRepository = new InMemoryParentGuardianRepository();
        var studentRepository = new InMemoryStudentRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new CreateRelationshipInvitationCommandHandler(
            relationshipRepository,
            parentGuardianRepository,
            studentRepository,
            StubCurrentUserProvider.As(callerId, callerRole),
            unitOfWork);

        return (relationshipRepository, parentGuardianRepository, studentRepository, unitOfWork, handler);
    }

    [Fact]
    public async Task Handle_creates_relationship_when_both_accounts_exist()
    {
        var parentGuardian = ParentGuardian.Register(TestCredentials.Email(), TestCredentials.Hash());
        var student = Student.Register(TestCredentials.Email(), TestCredentials.Hash(), isMinor: false);
        var sut = CreateSut(parentGuardian.Id.Value, "ParentGuardian");
        await sut.ParentGuardianRepository.AddAsync(parentGuardian);
        await sut.StudentRepository.AddAsync(student);

        var result = await sut.Handler.Handle(
            new CreateRelationshipInvitationCommand(parentGuardian.Id.Value, student.Id.Value));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, sut.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_returns_failure_when_parent_guardian_not_found()
    {
        var student = Student.Register(TestCredentials.Email(), TestCredentials.Hash(), isMinor: false);
        var sut = CreateSut(student.Id.Value, "Student");
        await sut.StudentRepository.AddAsync(student);

        var result = await sut.Handler.Handle(
            new CreateRelationshipInvitationCommand(Guid.NewGuid(), student.Id.Value));

        Assert.True(result.IsFailure);
        Assert.Equal(0, sut.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_returns_failure_when_student_not_found()
    {
        var parentGuardian = ParentGuardian.Register(TestCredentials.Email(), TestCredentials.Hash());
        var sut = CreateSut(parentGuardian.Id.Value, "ParentGuardian");
        await sut.ParentGuardianRepository.AddAsync(parentGuardian);

        var result = await sut.Handler.Handle(
            new CreateRelationshipInvitationCommand(parentGuardian.Id.Value, Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.Equal(0, sut.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_returns_failure_for_empty_ids_without_touching_repositories()
    {
        var sut = CreateSut(Guid.NewGuid(), "ParentGuardian");

        var result = await sut.Handler.Handle(new CreateRelationshipInvitationCommand(Guid.Empty, Guid.Empty));

        Assert.True(result.IsFailure);
        Assert.Equal(0, sut.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_returns_forbidden_when_caller_is_neither_named_party()
    {
        var parentGuardian = ParentGuardian.Register(TestCredentials.Email(), TestCredentials.Hash());
        var student = Student.Register(TestCredentials.Email(), TestCredentials.Hash(), isMinor: false);
        var sut = CreateSut(Guid.NewGuid(), "ParentGuardian");
        await sut.ParentGuardianRepository.AddAsync(parentGuardian);
        await sut.StudentRepository.AddAsync(student);

        var result = await sut.Handler.Handle(
            new CreateRelationshipInvitationCommand(parentGuardian.Id.Value, student.Id.Value));

        Assert.True(result.IsFailure);
        Assert.Equal(TutorFlow.Application.Common.ErrorType.Authorization, result.Error.Type);
        Assert.Equal(0, sut.UnitOfWork.SaveChangesCallCount);
    }
}
