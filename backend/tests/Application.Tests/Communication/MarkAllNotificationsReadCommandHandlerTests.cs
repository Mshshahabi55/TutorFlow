using TutorFlow.Application.Communication.Commands;
using TutorFlow.Application.Communication.Handlers;
using TutorFlow.Application.Tests.TestDoubles;
using TutorFlow.Domain.Communication;
using TutorFlow.Domain.Communication.ValueObjects;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Tests.Communication;

public class MarkAllNotificationsReadCommandHandlerTests
{
    [Fact]
    public async Task Marks_every_unread_notification_for_the_caller_read()
    {
        var recipient = AccountId.New();
        var first = Notification.Create(recipient, NotificationType.BookingConfirmed, "Booked", null, DateTime.UtcNow);
        var second = Notification.Create(recipient, NotificationType.LessonCancelled, "Cancelled", null, DateTime.UtcNow);
        var repository = new InMemoryNotificationRepository();
        await repository.AddAsync(first);
        await repository.AddAsync(second);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new MarkAllNotificationsReadCommandHandler(
            repository, StubCurrentUserProvider.AsStudent(recipient.Value), new FixedDateTimeProvider(DateTime.UtcNow), unitOfWork);

        var result = await handler.Handle(new MarkAllNotificationsReadCommand());

        Assert.True(result.IsSuccess);
        Assert.NotNull(first.ReadAtUtc);
        Assert.NotNull(second.ReadAtUtc);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Does_not_call_SaveChanges_when_nothing_is_unread()
    {
        var recipient = AccountId.New();
        var repository = new InMemoryNotificationRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new MarkAllNotificationsReadCommandHandler(
            repository, StubCurrentUserProvider.AsStudent(recipient.Value), new FixedDateTimeProvider(DateTime.UtcNow), unitOfWork);

        var result = await handler.Handle(new MarkAllNotificationsReadCommand());

        Assert.True(result.IsSuccess);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }
}
