using TutorFlow.Application.Discovery.Handlers;
using TutorFlow.Application.Discovery.Queries;
using TutorFlow.Application.Tests.TestDoubles;
using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Identity.ValueObjects;
using TutorFlow.Domain.Scheduling;
using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Application.Tests.Discovery;

public class SearchTutorsQueryHandlerTests
{
    [Fact]
    public async Task Handle_with_no_filters_returns_every_discoverable_tutor()
    {
        var tutorRepository = new InMemoryTutorRepository();
        var slotRepository = new InMemoryAvailabilitySlotRepository();

        var discoverable = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        discoverable.Approve();
        await tutorRepository.AddAsync(discoverable);

        var pending = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        await tutorRepository.AddAsync(pending);

        var handler = new SearchTutorsQueryHandler(tutorRepository, slotRepository);

        var result = await handler.Handle(new SearchTutorsQuery(null, null, null, null, 1, 20));

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items, t => t.TutorId == discoverable.Id.Value);
        Assert.DoesNotContain(result.Value.Items, t => t.TutorId == pending.Id.Value);
    }

    [Fact]
    public async Task Handle_filters_by_subject()
    {
        var tutorRepository = new InMemoryTutorRepository();
        var slotRepository = new InMemoryAvailabilitySlotRepository();

        var math = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        math.Approve();
        math.SetSubject(Subject.Of("Mathematics"));
        await tutorRepository.AddAsync(math);

        var history = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        history.Approve();
        history.SetSubject(Subject.Of("History"));
        await tutorRepository.AddAsync(history);

        var handler = new SearchTutorsQueryHandler(tutorRepository, slotRepository);

        var result = await handler.Handle(new SearchTutorsQuery("Mathematics", null, null, null, 1, 20));

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items, t => t.TutorId == math.Id.Value);
        Assert.DoesNotContain(result.Value.Items, t => t.TutorId == history.Id.Value);
    }

    [Fact]
    public async Task Handle_filters_by_available_from()
    {
        var tutorRepository = new InMemoryTutorRepository();
        var slotRepository = new InMemoryAvailabilitySlotRepository();

        var availableTutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        availableTutor.Approve();
        await tutorRepository.AddAsync(availableTutor);
        var openSlot = AvailabilitySlot.Declare(
            TutorId.From(availableTutor.Id.Value),
            DateTime.UtcNow.AddDays(5),
            SessionDuration.Of(TimeSpan.FromHours(1)),
            DeliveryMode.Online);
        await slotRepository.AddAsync(openSlot);

        var noSlotTutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        noSlotTutor.Approve();
        await tutorRepository.AddAsync(noSlotTutor);

        var handler = new SearchTutorsQueryHandler(tutorRepository, slotRepository);

        var result = await handler.Handle(
            new SearchTutorsQuery(null, null, null, DateTime.UtcNow, 1, 20));

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items, t => t.TutorId == availableTutor.Id.Value);
        Assert.DoesNotContain(result.Value.Items, t => t.TutorId == noSlotTutor.Id.Value);
    }

    [Fact]
    public async Task Handle_paginates_the_composed_result()
    {
        var tutorRepository = new InMemoryTutorRepository();
        var slotRepository = new InMemoryAvailabilitySlotRepository();

        for (var i = 0; i < 3; i++)
        {
            var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
            tutor.Approve();
            await tutorRepository.AddAsync(tutor);
        }

        var handler = new SearchTutorsQueryHandler(tutorRepository, slotRepository);

        var result = await handler.Handle(new SearchTutorsQuery(null, null, null, null, 1, 2));

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.TotalCount);
        Assert.Equal(2, result.Value.Items.Count);
    }

    [Fact]
    public async Task Handle_returns_failure_for_page_size_out_of_range()
    {
        var tutorRepository = new InMemoryTutorRepository();
        var slotRepository = new InMemoryAvailabilitySlotRepository();
        var handler = new SearchTutorsQueryHandler(tutorRepository, slotRepository);

        var result = await handler.Handle(new SearchTutorsQuery(null, null, null, null, 1, 1000));

        Assert.True(result.IsFailure);
    }
}
