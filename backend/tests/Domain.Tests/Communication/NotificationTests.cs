using TutorFlow.Domain.Communication;
using TutorFlow.Domain.Communication.Events;
using TutorFlow.Domain.Communication.ValueObjects;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Domain.Tests.Communication;

public class NotificationTests
{
    [Fact]
    public void Creating_records_recipient_type_summary_and_related_entity()
    {
        var recipient = AccountId.New();
        var relatedId = Guid.NewGuid();

        var notification = Notification.Create(
            recipient, NotificationType.BookingConfirmed, "Your lesson has been booked.", relatedId, DateTime.UtcNow);

        Assert.Equal(recipient, notification.RecipientId);
        Assert.Equal(NotificationType.BookingConfirmed, notification.Type);
        Assert.Equal("Your lesson has been booked.", notification.Summary);
        Assert.Equal(relatedId, notification.RelatedEntityId);
        Assert.Null(notification.ReadAtUtc);
    }

    [Fact]
    public void Creating_with_no_related_entity_is_allowed()
    {
        var notification = Notification.Create(
            AccountId.New(), NotificationType.ParentConfirmed, "A relationship was confirmed.", null, DateTime.UtcNow);

        Assert.Null(notification.RelatedEntityId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Creating_with_an_empty_summary_fails(string summary)
    {
        Assert.Throws<ArgumentException>(
            () => Notification.Create(AccountId.New(), NotificationType.NewMessage, summary, null, DateTime.UtcNow));
    }

    [Fact]
    public void Creating_truncates_a_summary_over_the_max_length_rather_than_failing()
    {
        var tooLong = new string('a', Notification.MaxSummaryLength + 50);

        var notification = Notification.Create(AccountId.New(), NotificationType.NewMessage, tooLong, null, DateTime.UtcNow);

        Assert.Equal(Notification.MaxSummaryLength, notification.Summary.Length);
    }

    [Fact]
    public void Creating_raises_NotificationCreated()
    {
        var recipient = AccountId.New();
        var notification = Notification.Create(recipient, NotificationType.TutorReplied, "Your tutor replied.", null, DateTime.UtcNow);

        Assert.Contains(
            notification.DomainEvents,
            e => e is NotificationCreated created && created.NotificationId == notification.Id && created.RecipientId == recipient);
    }

    [Fact]
    public void MarkRead_is_idempotent_and_keeps_the_first_read_time()
    {
        var notification = Notification.Create(AccountId.New(), NotificationType.NewMessage, "Hi", null, DateTime.UtcNow);
        var firstRead = DateTime.UtcNow;
        notification.MarkRead(firstRead);

        notification.MarkRead(firstRead.AddMinutes(5));

        Assert.Equal(firstRead, notification.ReadAtUtc);
    }
}
