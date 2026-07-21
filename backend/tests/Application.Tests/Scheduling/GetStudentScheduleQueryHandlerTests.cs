using TutorFlow.Application.Scheduling.Handlers;
using TutorFlow.Application.Scheduling.Queries;
using TutorFlow.Application.Tests.TestDoubles;
using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Identity.ValueObjects;
using TutorFlow.Domain.Scheduling;
using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Application.Tests.Scheduling;

public class GetStudentScheduleQueryHandlerTests
{
    [Fact]
    public async Task Handle_returns_every_session_for_the_student_regardless_of_status()
    {
        var studentRepository = new InMemoryStudentRepository();
        var sessionRepository = new InMemorySessionRepository();
        var relationshipRepository = new InMemoryRelationshipRepository();

        var student = Student.Register(TestCredentials.Email(), TestCredentials.Hash(), isMinor: false);
        await studentRepository.AddAsync(student);
        var studentId = StudentId.From(student.Id.Value);

        var slot = AvailabilitySlot.Declare(TutorId.From(Guid.NewGuid()), DateTime.UtcNow.AddDays(1), SessionDuration.Of(TimeSpan.FromHours(1)), DeliveryMode.Online);
        var session = slot.Book(studentId, null);
        await sessionRepository.AddAsync(session);

        var otherStudentSlot = AvailabilitySlot.Declare(TutorId.From(Guid.NewGuid()), DateTime.UtcNow.AddDays(1), SessionDuration.Of(TimeSpan.FromHours(1)), DeliveryMode.Online);
        var otherStudentSession = otherStudentSlot.Book(StudentId.From(Guid.NewGuid()), null);
        await sessionRepository.AddAsync(otherStudentSession);

        var handler = new GetStudentScheduleQueryHandler(
            studentRepository, sessionRepository, relationshipRepository, StubCurrentUserProvider.AsStudent(student.Id.Value));

        var result = await handler.Handle(new GetStudentScheduleQuery(student.Id.Value));

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value, s => s.SessionId == session.Id.Value);
        Assert.DoesNotContain(result.Value, s => s.SessionId == otherStudentSession.Id.Value);
    }

    [Fact]
    public async Task Handle_returns_schedule_for_a_parent_guardian_with_a_confirmed_relationship()
    {
        var studentRepository = new InMemoryStudentRepository();
        var sessionRepository = new InMemorySessionRepository();
        var relationshipRepository = new InMemoryRelationshipRepository();
        var student = Student.Register(TestCredentials.Email(), TestCredentials.Hash(), isMinor: false);
        await studentRepository.AddAsync(student);
        var parentGuardianId = AccountId.New();
        var relationship = Relationship.Invite(parentGuardianId, student.Id, parentGuardianId);
        relationship.Confirm();
        await relationshipRepository.AddAsync(relationship);
        var handler = new GetStudentScheduleQueryHandler(
            studentRepository, sessionRepository, relationshipRepository,
            StubCurrentUserProvider.AsParentGuardian(parentGuardianId.Value));

        var result = await handler.Handle(new GetStudentScheduleQuery(student.Id.Value));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_returns_schedule_for_admin()
    {
        var studentRepository = new InMemoryStudentRepository();
        var sessionRepository = new InMemorySessionRepository();
        var relationshipRepository = new InMemoryRelationshipRepository();
        var student = Student.Register(TestCredentials.Email(), TestCredentials.Hash(), isMinor: false);
        await studentRepository.AddAsync(student);
        var handler = new GetStudentScheduleQueryHandler(
            studentRepository, sessionRepository, relationshipRepository, StubCurrentUserProvider.AsAdminStaff(Guid.NewGuid()));

        var result = await handler.Handle(new GetStudentScheduleQuery(student.Id.Value));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_returns_forbidden_for_a_parent_guardian_without_a_confirmed_relationship()
    {
        var studentRepository = new InMemoryStudentRepository();
        var sessionRepository = new InMemorySessionRepository();
        var relationshipRepository = new InMemoryRelationshipRepository();
        var student = Student.Register(TestCredentials.Email(), TestCredentials.Hash(), isMinor: false);
        await studentRepository.AddAsync(student);
        var parentGuardianId = AccountId.New();
        var relationship = Relationship.Invite(parentGuardianId, student.Id, parentGuardianId);
        await relationshipRepository.AddAsync(relationship);
        var handler = new GetStudentScheduleQueryHandler(
            studentRepository, sessionRepository, relationshipRepository,
            StubCurrentUserProvider.AsParentGuardian(parentGuardianId.Value));

        var result = await handler.Handle(new GetStudentScheduleQuery(student.Id.Value));

        Assert.True(result.IsFailure);
        Assert.Equal(TutorFlow.Application.Common.ErrorType.Authorization, result.Error.Type);
    }

    [Fact]
    public async Task Handle_returns_unauthenticated_error_for_an_anonymous_caller()
    {
        var studentRepository = new InMemoryStudentRepository();
        var sessionRepository = new InMemorySessionRepository();
        var relationshipRepository = new InMemoryRelationshipRepository();
        var student = Student.Register(TestCredentials.Email(), TestCredentials.Hash(), isMinor: false);
        await studentRepository.AddAsync(student);
        var handler = new GetStudentScheduleQueryHandler(
            studentRepository, sessionRepository, relationshipRepository, StubCurrentUserProvider.Unauthenticated());

        var result = await handler.Handle(new GetStudentScheduleQuery(student.Id.Value));

        Assert.True(result.IsFailure);
        Assert.Equal("GetStudentScheduleQuery.Unauthenticated", result.Error.Code);
    }

    [Fact]
    public async Task Handle_returns_not_found_for_unknown_student()
    {
        var studentRepository = new InMemoryStudentRepository();
        var sessionRepository = new InMemorySessionRepository();
        var relationshipRepository = new InMemoryRelationshipRepository();
        var handler = new GetStudentScheduleQueryHandler(
            studentRepository, sessionRepository, relationshipRepository, StubCurrentUserProvider.AsAdminStaff(Guid.NewGuid()));

        var result = await handler.Handle(new GetStudentScheduleQuery(Guid.NewGuid()));

        Assert.True(result.IsFailure);
    }
}
