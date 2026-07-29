using TutorFlow.Application.Scheduling.Commands;
using TutorFlow.Application.Scheduling.Handlers;
using TutorFlow.Application.Tests.TestDoubles;
using TutorFlow.Domain.Scheduling;
using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Application.Tests.Scheduling;

public class RescheduleSessionCommandHandlerTests
{
    private static (AvailabilitySlot OldSlot, Session Session, TutorId TutorId) BookSession()
    {
        var tutorId = TutorId.From(Guid.NewGuid());
        var slot = AvailabilitySlot.Declare(
            tutorId,
            DateTime.UtcNow.AddDays(1),
            SessionDuration.Of(TimeSpan.FromHours(1)),
            DeliveryMode.Online);
        var session = slot.Book(StudentId.From(Guid.NewGuid()), parentGuardianId: null);
        return (slot, session, tutorId);
    }

    private static AvailabilitySlot DeclareOpenSlot(TutorId tutorId) => AvailabilitySlot.Declare(
        tutorId,
        DateTime.UtcNow.AddDays(2),
        SessionDuration.Of(TimeSpan.FromHours(1)),
        DeliveryMode.Online);

    [Fact]
    public async Task Handle_reschedules_existing_session_and_calls_SaveChanges()
    {
        var sessionRepository = new InMemorySessionRepository();
        var slotRepository = new InMemoryAvailabilitySlotRepository();
        var unitOfWork = new FakeUnitOfWork();
        var (oldSlot, session, tutorId) = BookSession();
        var newSlot = DeclareOpenSlot(tutorId);
        await sessionRepository.AddAsync(session);
        await slotRepository.AddAsync(oldSlot);
        await slotRepository.AddAsync(newSlot);
        var handler = new RescheduleSessionCommandHandler(
            sessionRepository, slotRepository, StubCurrentUserProvider.As(session.StudentId.Value, "Student"), unitOfWork, new FixedDateTimeProvider(DateTime.UtcNow), new FakeSchedulingConstraintsProvider());

        var result = await handler.Handle(new RescheduleSessionCommand(session.Id.Value, newSlot.Id.Value));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
        Assert.Equal(newSlot.Id, session.AvailabilitySlotId);
        Assert.Equal(newSlot.StartTimeUtc, session.ScheduledTimeUtc);
    }

    // Phase 4.7: rescheduling is cancel-and-rebook — the old slot must free
    // up and the new one must become consumed, both in the same operation.
    [Fact]
    public async Task Handle_reopens_the_old_slot_and_consumes_the_new_slot()
    {
        var sessionRepository = new InMemorySessionRepository();
        var slotRepository = new InMemoryAvailabilitySlotRepository();
        var unitOfWork = new FakeUnitOfWork();
        var (oldSlot, session, tutorId) = BookSession();
        var newSlot = DeclareOpenSlot(tutorId);
        await sessionRepository.AddAsync(session);
        await slotRepository.AddAsync(oldSlot);
        await slotRepository.AddAsync(newSlot);
        var handler = new RescheduleSessionCommandHandler(
            sessionRepository, slotRepository, StubCurrentUserProvider.As(session.StudentId.Value, "Student"), unitOfWork, new FixedDateTimeProvider(DateTime.UtcNow), new FakeSchedulingConstraintsProvider());

        var result = await handler.Handle(new RescheduleSessionCommand(session.Id.Value, newSlot.Id.Value));

        Assert.True(result.IsSuccess);
        Assert.False(oldSlot.IsConsumed);
        Assert.True(newSlot.IsConsumed);
    }

    [Fact]
    public async Task Handle_returns_failure_when_session_not_found()
    {
        var sessionRepository = new InMemorySessionRepository();
        var slotRepository = new InMemoryAvailabilitySlotRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new RescheduleSessionCommandHandler(
            sessionRepository, slotRepository, StubCurrentUserProvider.AsAdminStaff(Guid.NewGuid()), unitOfWork, new FixedDateTimeProvider(DateTime.UtcNow), new FakeSchedulingConstraintsProvider());

        var result = await handler.Handle(new RescheduleSessionCommand(Guid.NewGuid(), Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_returns_failure_when_new_slot_not_found()
    {
        var sessionRepository = new InMemorySessionRepository();
        var slotRepository = new InMemoryAvailabilitySlotRepository();
        var unitOfWork = new FakeUnitOfWork();
        var (oldSlot, session, _) = BookSession();
        await sessionRepository.AddAsync(session);
        await slotRepository.AddAsync(oldSlot);
        var handler = new RescheduleSessionCommandHandler(
            sessionRepository, slotRepository, StubCurrentUserProvider.As(session.StudentId.Value, "Student"), unitOfWork, new FixedDateTimeProvider(DateTime.UtcNow), new FakeSchedulingConstraintsProvider());

        var result = await handler.Handle(new RescheduleSessionCommand(session.Id.Value, Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.Equal("RescheduleSessionCommand.AvailabilitySlotNotFound", result.Error.Code);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_translates_domain_error_when_session_not_scheduled()
    {
        var sessionRepository = new InMemorySessionRepository();
        var slotRepository = new InMemoryAvailabilitySlotRepository();
        var unitOfWork = new FakeUnitOfWork();
        var (oldSlot, session, tutorId) = BookSession();
        session.Cancel();
        var newSlot = DeclareOpenSlot(tutorId);
        await sessionRepository.AddAsync(session);
        await slotRepository.AddAsync(oldSlot);
        await slotRepository.AddAsync(newSlot);
        var handler = new RescheduleSessionCommandHandler(
            sessionRepository, slotRepository, StubCurrentUserProvider.As(session.StudentId.Value, "Student"), unitOfWork, new FixedDateTimeProvider(DateTime.UtcNow), new FakeSchedulingConstraintsProvider());

        var result = await handler.Handle(new RescheduleSessionCommand(session.Id.Value, newSlot.Id.Value));

        Assert.True(result.IsFailure);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    // Phase 4.7: Session.Reschedule's own "same Tutor" guard, exercised
    // through the handler.
    [Fact]
    public async Task Handle_translates_domain_error_when_new_slot_belongs_to_a_different_tutor()
    {
        var sessionRepository = new InMemorySessionRepository();
        var slotRepository = new InMemoryAvailabilitySlotRepository();
        var unitOfWork = new FakeUnitOfWork();
        var (oldSlot, session, _) = BookSession();
        var otherTutorsSlot = DeclareOpenSlot(TutorId.From(Guid.NewGuid()));
        await sessionRepository.AddAsync(session);
        await slotRepository.AddAsync(oldSlot);
        await slotRepository.AddAsync(otherTutorsSlot);
        var handler = new RescheduleSessionCommandHandler(
            sessionRepository, slotRepository, StubCurrentUserProvider.As(session.StudentId.Value, "Student"), unitOfWork, new FixedDateTimeProvider(DateTime.UtcNow), new FakeSchedulingConstraintsProvider());

        var result = await handler.Handle(new RescheduleSessionCommand(session.Id.Value, otherTutorsSlot.Id.Value));

        Assert.True(result.IsFailure);
        Assert.Equal("RescheduleSessionCommand.InvalidState", result.Error.Code);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    // AvailabilitySlot.Consume's own "not already consumed" guard,
    // exercised through the handler — the reschedule equivalent of
    // BookSessionCommand's own already-consumed-slot rejection.
    [Fact]
    public async Task Handle_translates_domain_error_when_new_slot_is_already_consumed()
    {
        var sessionRepository = new InMemorySessionRepository();
        var slotRepository = new InMemoryAvailabilitySlotRepository();
        var unitOfWork = new FakeUnitOfWork();
        var (oldSlot, session, tutorId) = BookSession();
        var alreadyConsumedSlot = DeclareOpenSlot(tutorId);
        alreadyConsumedSlot.Book(StudentId.From(Guid.NewGuid()), parentGuardianId: null);
        await sessionRepository.AddAsync(session);
        await slotRepository.AddAsync(oldSlot);
        await slotRepository.AddAsync(alreadyConsumedSlot);
        var handler = new RescheduleSessionCommandHandler(
            sessionRepository, slotRepository, StubCurrentUserProvider.As(session.StudentId.Value, "Student"), unitOfWork, new FixedDateTimeProvider(DateTime.UtcNow), new FakeSchedulingConstraintsProvider());

        var result = await handler.Handle(new RescheduleSessionCommand(session.Id.Value, alreadyConsumedSlot.Id.Value));

        Assert.True(result.IsFailure);
        Assert.Equal("RescheduleSessionCommand.InvalidState", result.Error.Code);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_reschedules_for_admin_who_is_not_a_party()
    {
        var sessionRepository = new InMemorySessionRepository();
        var slotRepository = new InMemoryAvailabilitySlotRepository();
        var unitOfWork = new FakeUnitOfWork();
        var (oldSlot, session, tutorId) = BookSession();
        var newSlot = DeclareOpenSlot(tutorId);
        await sessionRepository.AddAsync(session);
        await slotRepository.AddAsync(oldSlot);
        await slotRepository.AddAsync(newSlot);
        var handler = new RescheduleSessionCommandHandler(
            sessionRepository, slotRepository, StubCurrentUserProvider.AsAdminStaff(Guid.NewGuid()), unitOfWork, new FixedDateTimeProvider(DateTime.UtcNow), new FakeSchedulingConstraintsProvider());

        var result = await handler.Handle(new RescheduleSessionCommand(session.Id.Value, newSlot.Id.Value));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_returns_forbidden_when_caller_is_not_a_party_and_not_admin()
    {
        var sessionRepository = new InMemorySessionRepository();
        var slotRepository = new InMemoryAvailabilitySlotRepository();
        var unitOfWork = new FakeUnitOfWork();
        var (oldSlot, session, tutorId) = BookSession();
        var newSlot = DeclareOpenSlot(tutorId);
        await sessionRepository.AddAsync(session);
        await slotRepository.AddAsync(oldSlot);
        await slotRepository.AddAsync(newSlot);
        var handler = new RescheduleSessionCommandHandler(
            sessionRepository, slotRepository, StubCurrentUserProvider.AsStudent(Guid.NewGuid()), unitOfWork, new FixedDateTimeProvider(DateTime.UtcNow), new FakeSchedulingConstraintsProvider());

        var result = await handler.Handle(new RescheduleSessionCommand(session.Id.Value, newSlot.Id.Value));

        Assert.True(result.IsFailure);
        Assert.Equal(TutorFlow.Application.Common.ErrorType.Authorization, result.Error.Type);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    // docs/adr/ADR-025-... Addendum — Booking Notice & Horizon (Accepted
    // 2026-07-29), applied identically to reschedule's new start time.
    [Fact]
    public async Task Handle_rejects_a_reschedule_onto_a_slot_less_than_the_minimum_notice_away()
    {
        var sessionRepository = new InMemorySessionRepository();
        var slotRepository = new InMemoryAvailabilitySlotRepository();
        var unitOfWork = new FakeUnitOfWork();
        var (oldSlot, session, tutorId) = BookSession();
        var now = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        var newSlot = AvailabilitySlot.Declare(
            tutorId, now.AddHours(23), SessionDuration.Of(TimeSpan.FromHours(1)), DeliveryMode.Online);
        await sessionRepository.AddAsync(session);
        await slotRepository.AddAsync(oldSlot);
        await slotRepository.AddAsync(newSlot);
        var handler = new RescheduleSessionCommandHandler(
            sessionRepository, slotRepository, StubCurrentUserProvider.As(session.StudentId.Value, "Student"),
            unitOfWork, new FixedDateTimeProvider(now), new FakeSchedulingConstraintsProvider());

        var result = await handler.Handle(new RescheduleSessionCommand(session.Id.Value, newSlot.Id.Value));

        Assert.True(result.IsFailure);
        Assert.Equal("RescheduleSessionCommand.BookingTooSoon", result.Error.Code);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_allows_a_reschedule_exactly_at_the_minimum_notice_boundary()
    {
        var sessionRepository = new InMemorySessionRepository();
        var slotRepository = new InMemoryAvailabilitySlotRepository();
        var unitOfWork = new FakeUnitOfWork();
        var (oldSlot, session, tutorId) = BookSession();
        var now = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        var newSlot = AvailabilitySlot.Declare(
            tutorId, now.AddHours(24), SessionDuration.Of(TimeSpan.FromHours(1)), DeliveryMode.Online);
        await sessionRepository.AddAsync(session);
        await slotRepository.AddAsync(oldSlot);
        await slotRepository.AddAsync(newSlot);
        var handler = new RescheduleSessionCommandHandler(
            sessionRepository, slotRepository, StubCurrentUserProvider.As(session.StudentId.Value, "Student"),
            unitOfWork, new FixedDateTimeProvider(now), new FakeSchedulingConstraintsProvider());

        var result = await handler.Handle(new RescheduleSessionCommand(session.Id.Value, newSlot.Id.Value));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_rejects_a_reschedule_onto_a_slot_beyond_the_maximum_horizon()
    {
        var sessionRepository = new InMemorySessionRepository();
        var slotRepository = new InMemoryAvailabilitySlotRepository();
        var unitOfWork = new FakeUnitOfWork();
        var (oldSlot, session, tutorId) = BookSession();
        var now = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        var newSlot = AvailabilitySlot.Declare(
            tutorId, now.AddDays(90).AddHours(1), SessionDuration.Of(TimeSpan.FromHours(1)), DeliveryMode.Online);
        await sessionRepository.AddAsync(session);
        await slotRepository.AddAsync(oldSlot);
        await slotRepository.AddAsync(newSlot);
        var handler = new RescheduleSessionCommandHandler(
            sessionRepository, slotRepository, StubCurrentUserProvider.As(session.StudentId.Value, "Student"),
            unitOfWork, new FixedDateTimeProvider(now), new FakeSchedulingConstraintsProvider());

        var result = await handler.Handle(new RescheduleSessionCommand(session.Id.Value, newSlot.Id.Value));

        Assert.True(result.IsFailure);
        Assert.Equal("RescheduleSessionCommand.BookingTooFarAhead", result.Error.Code);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_allows_a_reschedule_exactly_at_the_maximum_horizon_boundary()
    {
        var sessionRepository = new InMemorySessionRepository();
        var slotRepository = new InMemoryAvailabilitySlotRepository();
        var unitOfWork = new FakeUnitOfWork();
        var (oldSlot, session, tutorId) = BookSession();
        var now = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        var newSlot = AvailabilitySlot.Declare(
            tutorId, now.AddDays(90), SessionDuration.Of(TimeSpan.FromHours(1)), DeliveryMode.Online);
        await sessionRepository.AddAsync(session);
        await slotRepository.AddAsync(oldSlot);
        await slotRepository.AddAsync(newSlot);
        var handler = new RescheduleSessionCommandHandler(
            sessionRepository, slotRepository, StubCurrentUserProvider.As(session.StudentId.Value, "Student"),
            unitOfWork, new FixedDateTimeProvider(now), new FakeSchedulingConstraintsProvider());

        var result = await handler.Handle(new RescheduleSessionCommand(session.Id.Value, newSlot.Id.Value));

        Assert.True(result.IsSuccess);
    }
}
