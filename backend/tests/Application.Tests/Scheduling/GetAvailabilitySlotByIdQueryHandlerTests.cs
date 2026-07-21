using TutorFlow.Application.Scheduling.Handlers;
using TutorFlow.Application.Scheduling.Queries;
using TutorFlow.Application.Tests.TestDoubles;
using TutorFlow.Domain.Scheduling;
using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Application.Tests.Scheduling;

public class GetAvailabilitySlotByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_returns_slot_for_the_declaring_tutor()
    {
        var slotRepository = new InMemoryAvailabilitySlotRepository();
        var sessionRepository = new InMemorySessionRepository();
        var tutorId = Guid.NewGuid();
        var slot = AvailabilitySlot.Declare(
            TutorId.From(tutorId),
            DateTime.UtcNow.AddDays(1),
            SessionDuration.Of(TimeSpan.FromHours(1)),
            DeliveryMode.Online);
        await slotRepository.AddAsync(slot);
        var handler = new GetAvailabilitySlotByIdQueryHandler(
            slotRepository, sessionRepository, StubCurrentUserProvider.AsTutor(tutorId));

        var result = await handler.Handle(new GetAvailabilitySlotByIdQuery(slot.Id.Value));

        Assert.True(result.IsSuccess);
        Assert.Equal(slot.Id.Value, result.Value.AvailabilitySlotId);
    }

    [Fact]
    public async Task Handle_returns_slot_for_admin()
    {
        var slotRepository = new InMemoryAvailabilitySlotRepository();
        var sessionRepository = new InMemorySessionRepository();
        var slot = AvailabilitySlot.Declare(
            TutorId.From(Guid.NewGuid()),
            DateTime.UtcNow.AddDays(1),
            SessionDuration.Of(TimeSpan.FromHours(1)),
            DeliveryMode.Online);
        await slotRepository.AddAsync(slot);
        var handler = new GetAvailabilitySlotByIdQueryHandler(
            slotRepository, sessionRepository, StubCurrentUserProvider.AsAdminStaff(Guid.NewGuid()));

        var result = await handler.Handle(new GetAvailabilitySlotByIdQuery(slot.Id.Value));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_returns_slot_for_the_booking_student_once_consumed()
    {
        var slotRepository = new InMemoryAvailabilitySlotRepository();
        var sessionRepository = new InMemorySessionRepository();
        var slot = AvailabilitySlot.Declare(
            TutorId.From(Guid.NewGuid()),
            DateTime.UtcNow.AddDays(1),
            SessionDuration.Of(TimeSpan.FromHours(1)),
            DeliveryMode.Online);
        var studentId = Guid.NewGuid();
        var session = slot.Book(StudentId.From(studentId), parentGuardianId: null);
        await slotRepository.AddAsync(slot);
        await sessionRepository.AddAsync(session);
        var handler = new GetAvailabilitySlotByIdQueryHandler(
            slotRepository, sessionRepository, StubCurrentUserProvider.AsStudent(studentId));

        var result = await handler.Handle(new GetAvailabilitySlotByIdQuery(slot.Id.Value));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_returns_unauthenticated_error_for_an_anonymous_caller()
    {
        var slotRepository = new InMemoryAvailabilitySlotRepository();
        var sessionRepository = new InMemorySessionRepository();
        var slot = AvailabilitySlot.Declare(
            TutorId.From(Guid.NewGuid()),
            DateTime.UtcNow.AddDays(1),
            SessionDuration.Of(TimeSpan.FromHours(1)),
            DeliveryMode.Online);
        await slotRepository.AddAsync(slot);
        var handler = new GetAvailabilitySlotByIdQueryHandler(
            slotRepository, sessionRepository, StubCurrentUserProvider.Unauthenticated());

        var result = await handler.Handle(new GetAvailabilitySlotByIdQuery(slot.Id.Value));

        Assert.True(result.IsFailure);
        Assert.Equal(TutorFlow.Application.Common.ErrorType.Authorization, result.Error.Type);
        Assert.Equal("GetAvailabilitySlotByIdQuery.Unauthenticated", result.Error.Code);
    }

    [Fact]
    public async Task Handle_returns_forbidden_for_an_unrelated_caller()
    {
        var slotRepository = new InMemoryAvailabilitySlotRepository();
        var sessionRepository = new InMemorySessionRepository();
        var slot = AvailabilitySlot.Declare(
            TutorId.From(Guid.NewGuid()),
            DateTime.UtcNow.AddDays(1),
            SessionDuration.Of(TimeSpan.FromHours(1)),
            DeliveryMode.Online);
        await slotRepository.AddAsync(slot);
        var handler = new GetAvailabilitySlotByIdQueryHandler(
            slotRepository, sessionRepository, StubCurrentUserProvider.AsStudent(Guid.NewGuid()));

        var result = await handler.Handle(new GetAvailabilitySlotByIdQuery(slot.Id.Value));

        Assert.True(result.IsFailure);
        Assert.Equal(TutorFlow.Application.Common.ErrorType.Authorization, result.Error.Type);
    }

    [Fact]
    public async Task Handle_returns_not_found_for_unknown_slot()
    {
        var slotRepository = new InMemoryAvailabilitySlotRepository();
        var sessionRepository = new InMemorySessionRepository();
        var handler = new GetAvailabilitySlotByIdQueryHandler(
            slotRepository, sessionRepository, StubCurrentUserProvider.AsAdminStaff(Guid.NewGuid()));

        var result = await handler.Handle(new GetAvailabilitySlotByIdQuery(Guid.NewGuid()));

        Assert.True(result.IsFailure);
    }
}
