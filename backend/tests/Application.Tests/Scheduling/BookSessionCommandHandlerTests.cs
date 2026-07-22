using TutorFlow.Application.Scheduling.Commands;
using TutorFlow.Application.Scheduling.Handlers;
using TutorFlow.Application.Tests.TestDoubles;
using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Identity.ValueObjects;
using TutorFlow.Domain.Scheduling;
using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Application.Tests.Scheduling;

public class BookSessionCommandHandlerTests
{
    // Session.Book(...) is internal to TutorFlow.Domain (Phase 11); this test
    // project, a separate assembly, has no way to call it even if it wanted
    // to. The only path to a Session is AvailabilitySlot.Book(...) — these
    // tests verify the handler uses exactly that path.
    private static AvailabilitySlot DeclareSlot(TutorId tutorId) => AvailabilitySlot.Declare(
        tutorId,
        DateTime.UtcNow.AddDays(1),
        SessionDuration.Of(TimeSpan.FromHours(1)),
        DeliveryMode.Online);

    // An adult Student booking for themselves is the simplest valid caller
    // (IDR-5) — no confirmed Relationship needed, unlike the Parent/Guardian
    // path (IDR-3, IDR-4), which is covered by its own dedicated test below.
    private static async Task<(
        InMemoryAvailabilitySlotRepository SlotRepository,
        InMemorySessionRepository SessionRepository,
        InMemoryStudentRepository StudentRepository,
        InMemoryRelationshipRepository RelationshipRepository,
        InMemoryTutorRepository TutorRepository,
        FakeUnitOfWork UnitOfWork,
        Student Student)> CreateSutPartsAsync()
    {
        var slotRepository = new InMemoryAvailabilitySlotRepository();
        var sessionRepository = new InMemorySessionRepository();
        var studentRepository = new InMemoryStudentRepository();
        var relationshipRepository = new InMemoryRelationshipRepository();
        var tutorRepository = new InMemoryTutorRepository();
        var unitOfWork = new FakeUnitOfWork();
        var student = Student.Register(TestCredentials.Email(), TestCredentials.Hash(), isMinor: false);
        await studentRepository.AddAsync(student);

        return (slotRepository, sessionRepository, studentRepository, relationshipRepository, tutorRepository, unitOfWork, student);
    }

    private static BookSessionCommandHandler CreateHandler(
        InMemoryAvailabilitySlotRepository slotRepository,
        InMemorySessionRepository sessionRepository,
        InMemoryStudentRepository studentRepository,
        InMemoryRelationshipRepository relationshipRepository,
        InMemoryTutorRepository tutorRepository,
        FakeUnitOfWork unitOfWork,
        Guid callerId,
        string callerRole) =>
        new(slotRepository, sessionRepository, studentRepository, relationshipRepository, tutorRepository,
            StubCurrentUserProvider.As(callerId, callerRole), unitOfWork);

    [Fact]
    public async Task Handle_books_session_against_slot_and_calls_SaveChanges()
    {
        var sut = await CreateSutPartsAsync();
        var slot = DeclareSlot(TutorId.From(Guid.NewGuid()));
        await sut.SlotRepository.AddAsync(slot);
        var handler = CreateHandler(
            sut.SlotRepository, sut.SessionRepository, sut.StudentRepository, sut.RelationshipRepository,
            sut.TutorRepository, sut.UnitOfWork, sut.Student.Id.Value, "Student");

        var result = await handler.Handle(new BookSessionCommand(slot.Id.Value, sut.Student.Id.Value, null));

        Assert.True(result.IsSuccess);
        Assert.Equal(slot.Id.Value, result.Value.AvailabilitySlotId);
        Assert.Equal(1, sut.UnitOfWork.SaveChangesCallCount);

        var storedSession = await sut.SessionRepository.GetByIdAsync(SessionId.From(result.Value.SessionId));
        Assert.NotNull(storedSession);
    }

    // Phase 4.6: the Session records what it cost, read from the Tutor's
    // HourlyRate at the moment of booking.
    [Fact]
    public async Task Handle_captures_the_Tutor_HourlyRate_as_the_Session_Price()
    {
        var sut = await CreateSutPartsAsync();
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        tutor.SetHourlyRate(TutorFlow.Domain.Identity.ValueObjects.HourlyRate.Of(500_000m));
        await sut.TutorRepository.AddAsync(tutor);
        var slot = DeclareSlot(TutorId.From(tutor.Id.Value));
        await sut.SlotRepository.AddAsync(slot);
        var handler = CreateHandler(
            sut.SlotRepository, sut.SessionRepository, sut.StudentRepository, sut.RelationshipRepository,
            sut.TutorRepository, sut.UnitOfWork, sut.Student.Id.Value, "Student");

        var result = await handler.Handle(new BookSessionCommand(slot.Id.Value, sut.Student.Id.Value, null));

        Assert.True(result.IsSuccess);
        var storedSession = await sut.SessionRepository.GetByIdAsync(SessionId.From(result.Value.SessionId));
        Assert.NotNull(storedSession!.Price);
        Assert.Equal(500_000m, storedSession.Price!.Amount);
    }

    // A Tutor with no HourlyRate configured is still bookable today (no
    // rule requires one) — the Session simply has no price to capture.
    [Fact]
    public async Task Handle_books_successfully_with_a_null_Price_when_the_Tutor_has_no_HourlyRate()
    {
        var sut = await CreateSutPartsAsync();
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        await sut.TutorRepository.AddAsync(tutor);
        var slot = DeclareSlot(TutorId.From(tutor.Id.Value));
        await sut.SlotRepository.AddAsync(slot);
        var handler = CreateHandler(
            sut.SlotRepository, sut.SessionRepository, sut.StudentRepository, sut.RelationshipRepository,
            sut.TutorRepository, sut.UnitOfWork, sut.Student.Id.Value, "Student");

        var result = await handler.Handle(new BookSessionCommand(slot.Id.Value, sut.Student.Id.Value, null));

        Assert.True(result.IsSuccess);
        var storedSession = await sut.SessionRepository.GetByIdAsync(SessionId.From(result.Value.SessionId));
        Assert.Null(storedSession!.Price);
    }

    [Fact]
    public async Task Handle_marks_the_slot_consumed_proving_AvailabilitySlot_Book_was_used()
    {
        var sut = await CreateSutPartsAsync();
        var slot = DeclareSlot(TutorId.From(Guid.NewGuid()));
        await sut.SlotRepository.AddAsync(slot);
        var handler = CreateHandler(
            sut.SlotRepository, sut.SessionRepository, sut.StudentRepository, sut.RelationshipRepository,
            sut.TutorRepository, sut.UnitOfWork, sut.Student.Id.Value, "Student");

        await handler.Handle(new BookSessionCommand(slot.Id.Value, sut.Student.Id.Value, null));

        var storedSlot = await sut.SlotRepository.GetByIdAsync(slot.Id);
        Assert.NotNull(storedSlot);
        Assert.True(storedSlot!.IsConsumed);
    }

    [Fact]
    public async Task Handle_returns_failure_when_slot_not_found()
    {
        var sut = await CreateSutPartsAsync();
        var handler = CreateHandler(
            sut.SlotRepository, sut.SessionRepository, sut.StudentRepository, sut.RelationshipRepository,
            sut.TutorRepository, sut.UnitOfWork, sut.Student.Id.Value, "Student");

        var result = await handler.Handle(new BookSessionCommand(Guid.NewGuid(), sut.Student.Id.Value, null));

        Assert.True(result.IsFailure);
        Assert.Equal(0, sut.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_translates_domain_error_when_slot_already_consumed()
    {
        var sut = await CreateSutPartsAsync();
        var slot = DeclareSlot(TutorId.From(Guid.NewGuid()));
        slot.Book(StudentId.From(Guid.NewGuid()), parentGuardianId: null); // already consumed
        await sut.SlotRepository.AddAsync(slot);
        var handler = CreateHandler(
            sut.SlotRepository, sut.SessionRepository, sut.StudentRepository, sut.RelationshipRepository,
            sut.TutorRepository, sut.UnitOfWork, sut.Student.Id.Value, "Student");

        var result = await handler.Handle(new BookSessionCommand(slot.Id.Value, sut.Student.Id.Value, null));

        Assert.True(result.IsFailure);
        Assert.Equal(0, sut.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_returns_forbidden_when_a_minor_Student_books_independently()
    {
        var sut = await CreateSutPartsAsync();
        var minorStudent = Student.Register(TestCredentials.Email(), TestCredentials.Hash(), isMinor: true);
        await sut.StudentRepository.AddAsync(minorStudent);
        var slot = DeclareSlot(TutorId.From(Guid.NewGuid()));
        await sut.SlotRepository.AddAsync(slot);
        var handler = CreateHandler(
            sut.SlotRepository, sut.SessionRepository, sut.StudentRepository, sut.RelationshipRepository,
            sut.TutorRepository, sut.UnitOfWork, minorStudent.Id.Value, "Student");

        var result = await handler.Handle(new BookSessionCommand(slot.Id.Value, minorStudent.Id.Value, null));

        Assert.True(result.IsFailure);
        Assert.Equal(TutorFlow.Application.Common.ErrorType.Authorization, result.Error.Type);
        Assert.Equal(0, sut.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_books_for_a_ParentGuardian_with_a_confirmed_relationship()
    {
        var sut = await CreateSutPartsAsync();
        var parentGuardian = ParentGuardian.Register(TestCredentials.Email(), TestCredentials.Hash());
        var relationship = Relationship.Invite(parentGuardian.Id, sut.Student.Id, parentGuardian.Id);
        relationship.Confirm();
        await sut.RelationshipRepository.AddAsync(relationship);
        var slot = DeclareSlot(TutorId.From(Guid.NewGuid()));
        await sut.SlotRepository.AddAsync(slot);
        var handler = CreateHandler(
            sut.SlotRepository, sut.SessionRepository, sut.StudentRepository, sut.RelationshipRepository,
            sut.TutorRepository, sut.UnitOfWork, parentGuardian.Id.Value, "ParentGuardian");

        var result = await handler.Handle(
            new BookSessionCommand(slot.Id.Value, sut.Student.Id.Value, parentGuardian.Id.Value));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, sut.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_returns_forbidden_for_a_ParentGuardian_without_a_confirmed_relationship()
    {
        var sut = await CreateSutPartsAsync();
        var parentGuardian = ParentGuardian.Register(TestCredentials.Email(), TestCredentials.Hash());
        // Invited, never confirmed — IDR-3/IDR-4 requires a *confirmed* Relationship.
        var relationship = Relationship.Invite(parentGuardian.Id, sut.Student.Id, parentGuardian.Id);
        await sut.RelationshipRepository.AddAsync(relationship);
        var slot = DeclareSlot(TutorId.From(Guid.NewGuid()));
        await sut.SlotRepository.AddAsync(slot);
        var handler = CreateHandler(
            sut.SlotRepository, sut.SessionRepository, sut.StudentRepository, sut.RelationshipRepository,
            sut.TutorRepository, sut.UnitOfWork, parentGuardian.Id.Value, "ParentGuardian");

        var result = await handler.Handle(
            new BookSessionCommand(slot.Id.Value, sut.Student.Id.Value, parentGuardian.Id.Value));

        Assert.True(result.IsFailure);
        Assert.Equal(TutorFlow.Application.Common.ErrorType.Authorization, result.Error.Type);
        Assert.Equal(0, sut.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_returns_forbidden_when_caller_is_neither_the_student_nor_the_parent_guardian()
    {
        var sut = await CreateSutPartsAsync();
        var slot = DeclareSlot(TutorId.From(Guid.NewGuid()));
        await sut.SlotRepository.AddAsync(slot);
        var handler = CreateHandler(
            sut.SlotRepository, sut.SessionRepository, sut.StudentRepository, sut.RelationshipRepository,
            sut.TutorRepository, sut.UnitOfWork, Guid.NewGuid(), "Student");

        var result = await handler.Handle(new BookSessionCommand(slot.Id.Value, sut.Student.Id.Value, null));

        Assert.True(result.IsFailure);
        Assert.Equal(TutorFlow.Application.Common.ErrorType.Authorization, result.Error.Type);
        Assert.Equal(0, sut.UnitOfWork.SaveChangesCallCount);
    }
}
