using TutorFlow.Application.Common;
using TutorFlow.Application.Communication.Commands;
using TutorFlow.Application.Communication.Handlers;
using TutorFlow.Application.Tests.TestDoubles;
using TutorFlow.Domain.Communication;
using TutorFlow.Domain.Communication.ValueObjects;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Tests.Communication;

public class MarkNotificationReadCommandHandlerTests
{
    [Fact]
    public async Task The_recipient_can_mark_their_own_notification_read()
    {
        var recipient = AccountId.New();
        var notification = Notification.Create(
            recipient, NotificationType.BookingConfirmed, "Your lesson has been booked.", null, DateTime.UtcNow);
        var repository = new InMemoryNotificationRepository();
        await repository.AddAsync(notification);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new MarkNotificationReadCommandHandler(
            repository, StubCurrentUserProvider.AsStudent(recipient.Value), new FixedDateTimeProvider(DateTime.UtcNow), unitOfWork);

        var result = await handler.Handle(new MarkNotificationReadCommand(notification.Id.Value));

        Assert.True(result.IsSuccess);
        Assert.NotNull(notification.ReadAtUtc);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Someone_else_cannot_mark_a_notification_read()
    {
        var recipient = AccountId.New();
        var notification = Notification.Create(recipient, NotificationType.NewMessage, "Hi", null, DateTime.UtcNow);
        var repository = new InMemoryNotificationRepository();
        await repository.AddAsync(notification);
        var handler = new MarkNotificationReadCommandHandler(
            repository, StubCurrentUserProvider.AsStudent(AccountId.New().Value),
            new FixedDateTimeProvider(DateTime.UtcNow), new FakeUnitOfWork());

        var result = await handler.Handle(new MarkNotificationReadCommand(notification.Id.Value));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Authorization, result.Error.Type);
    }
}
