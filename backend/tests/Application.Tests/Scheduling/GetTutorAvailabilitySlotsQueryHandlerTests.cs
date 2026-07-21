using TutorFlow.Application.Scheduling.Handlers;
using TutorFlow.Application.Scheduling.Queries;
using TutorFlow.Application.Tests.TestDoubles;
using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Scheduling;
using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Application.Tests.Scheduling;

public class GetTutorAvailabilitySlotsQueryHandlerTests
{
    [Fact]
    public async Task Handle_returns_every_slot_for_the_tutor_regardless_of_consumption_state()
    {
        var tutorRepository = new InMemoryTutorRepository();
        var availabilitySlotRepository = new InMemoryAvailabilitySlotRepository();

        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        await tutorRepository.AddAsync(tutor);
        var tutorId = TutorId.From(tutor.Id.Value);

        var openSlot = AvailabilitySlot.Declare(tutorId, DateTime.UtcNow.AddDays(1), SessionDuration.Of(TimeSpan.FromHours(1)), DeliveryMode.Online);
        var consumedSlot = AvailabilitySlot.Declare(tutorId, DateTime.UtcNow.AddDays(2), SessionDuration.Of(TimeSpan.FromHours(1)), DeliveryMode.Online);
        consumedSlot.Book(StudentId.From(Guid.NewGuid()), null);

        var otherTutorSlot = AvailabilitySlot.Declare(TutorId.From(Guid.NewGuid()), DateTime.UtcNow.AddDays(1), SessionDuration.Of(TimeSpan.FromHours(1)), DeliveryMode.Online);

        await availabilitySlotRepository.AddAsync(openSlot);
        await availabilitySlotRepository.AddAsync(consumedSlot);
        await availabilitySlotRepository.AddAsync(otherTutorSlot);

        var handler = new GetTutorAvailabilitySlotsQueryHandler(
            tutorRepository, availabilitySlotRepository, StubCurrentUserProvider.AsTutor(tutor.Id.Value));

        var result = await handler.Handle(new GetTutorAvailabilitySlotsQuery(tutor.Id.Value));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Contains(result.Value, s => s.AvailabilitySlotId == openSlot.Id.Value && !s.IsConsumed);
        Assert.Contains(result.Value, s => s.AvailabilitySlotId == consumedSlot.Id.Value && s.IsConsumed);
        Assert.DoesNotContain(result.Value, s => s.AvailabilitySlotId == otherTutorSlot.Id.Value);
    }

    [Fact]
    public async Task Handle_returns_success_for_any_authenticated_caller_when_discoverable()
    {
        var tutorRepository = new InMemoryTutorRepository();
        var availabilitySlotRepository = new InMemoryAvailabilitySlotRepository();
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        tutor.Approve();
        await tutorRepository.AddAsync(tutor);
        var handler = new GetTutorAvailabilitySlotsQueryHandler(
            tutorRepository, availabilitySlotRepository, StubCurrentUserProvider.AsStudent(Guid.NewGuid()));

        var result = await handler.Handle(new GetTutorAvailabilitySlotsQuery(tutor.Id.Value));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_returns_unauthenticated_error_when_discoverable()
    {
        var tutorRepository = new InMemoryTutorRepository();
        var availabilitySlotRepository = new InMemoryAvailabilitySlotRepository();
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        tutor.Approve();
        await tutorRepository.AddAsync(tutor);
        var handler = new GetTutorAvailabilitySlotsQueryHandler(
            tutorRepository, availabilitySlotRepository, StubCurrentUserProvider.Unauthenticated());

        var result = await handler.Handle(new GetTutorAvailabilitySlotsQuery(tutor.Id.Value));

        Assert.True(result.IsFailure);
        Assert.Equal("GetTutorAvailabilitySlotsQuery.Unauthenticated", result.Error.Code);
    }

    [Fact]
    public async Task Handle_returns_forbidden_for_an_unrelated_caller_when_not_discoverable()
    {
        var tutorRepository = new InMemoryTutorRepository();
        var availabilitySlotRepository = new InMemoryAvailabilitySlotRepository();
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        await tutorRepository.AddAsync(tutor);
        var handler = new GetTutorAvailabilitySlotsQueryHandler(
            tutorRepository, availabilitySlotRepository, StubCurrentUserProvider.AsStudent(Guid.NewGuid()));

        var result = await handler.Handle(new GetTutorAvailabilitySlotsQuery(tutor.Id.Value));

        Assert.True(result.IsFailure);
        Assert.Equal(TutorFlow.Application.Common.ErrorType.Authorization, result.Error.Type);
    }

    [Fact]
    public async Task Handle_returns_success_for_admin_when_not_discoverable()
    {
        var tutorRepository = new InMemoryTutorRepository();
        var availabilitySlotRepository = new InMemoryAvailabilitySlotRepository();
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        await tutorRepository.AddAsync(tutor);
        var handler = new GetTutorAvailabilitySlotsQueryHandler(
            tutorRepository, availabilitySlotRepository, StubCurrentUserProvider.AsAdminStaff(Guid.NewGuid()));

        var result = await handler.Handle(new GetTutorAvailabilitySlotsQuery(tutor.Id.Value));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_returns_not_found_for_unknown_tutor()
    {
        var tutorRepository = new InMemoryTutorRepository();
        var availabilitySlotRepository = new InMemoryAvailabilitySlotRepository();
        var handler = new GetTutorAvailabilitySlotsQueryHandler(
            tutorRepository, availabilitySlotRepository, StubCurrentUserProvider.AsAdminStaff(Guid.NewGuid()));

        var result = await handler.Handle(new GetTutorAvailabilitySlotsQuery(Guid.NewGuid()));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_returns_empty_collection_for_tutor_with_no_declared_slots()
    {
        var tutorRepository = new InMemoryTutorRepository();
        var availabilitySlotRepository = new InMemoryAvailabilitySlotRepository();
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        await tutorRepository.AddAsync(tutor);
        var handler = new GetTutorAvailabilitySlotsQueryHandler(
            tutorRepository, availabilitySlotRepository, StubCurrentUserProvider.AsTutor(tutor.Id.Value));

        var result = await handler.Handle(new GetTutorAvailabilitySlotsQuery(tutor.Id.Value));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task Handle_returns_failure_for_empty_tutor_id()
    {
        var tutorRepository = new InMemoryTutorRepository();
        var availabilitySlotRepository = new InMemoryAvailabilitySlotRepository();
        var handler = new GetTutorAvailabilitySlotsQueryHandler(
            tutorRepository, availabilitySlotRepository, StubCurrentUserProvider.AsAdminStaff(Guid.NewGuid()));

        var result = await handler.Handle(new GetTutorAvailabilitySlotsQuery(Guid.Empty));

        Assert.True(result.IsFailure);
    }
}
