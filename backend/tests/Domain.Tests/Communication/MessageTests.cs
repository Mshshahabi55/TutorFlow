using TutorFlow.Domain.Communication;
using TutorFlow.Domain.Communication.Events;
using TutorFlow.Domain.Communication.ValueObjects;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Domain.Tests.Communication;

public class MessageTests
{
    private static readonly ConversationId SomeConversationId = ConversationId.New();

    [Fact]
    public void Sending_records_sender_recipient_and_body()
    {
        var sender = AccountId.New();
        var recipient = AccountId.New();

        var message = Message.Send(SomeConversationId, sender, recipient, "Hello!", DateTime.UtcNow);

        Assert.Equal(sender, message.SenderId);
        Assert.Equal(recipient, message.RecipientId);
        Assert.Equal("Hello!", message.Body);
        Assert.Null(message.ReadAtUtc);
    }

    [Fact]
    public void Sending_trims_surrounding_whitespace()
    {
        var message = Message.Send(SomeConversationId, AccountId.New(), AccountId.New(), "  padded  ", DateTime.UtcNow);

        Assert.Equal("padded", message.Body);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Sending_an_empty_or_whitespace_body_fails(string body)
    {
        Assert.Throws<ArgumentException>(
            () => Message.Send(SomeConversationId, AccountId.New(), AccountId.New(), body, DateTime.UtcNow));
    }

    [Fact]
    public void Sending_a_body_over_the_max_length_fails()
    {
        var tooLong = new string('a', Message.MaxBodyLength + 1);

        Assert.Throws<ArgumentException>(
            () => Message.Send(SomeConversationId, AccountId.New(), AccountId.New(), tooLong, DateTime.UtcNow));
    }

    [Fact]
    public void Sending_raises_MessageSent_carrying_the_recipient()
    {
        var recipient = AccountId.New();
        var message = Message.Send(SomeConversationId, AccountId.New(), recipient, "Hi", DateTime.UtcNow);

        Assert.Contains(
            message.DomainEvents,
            e => e is MessageSent sent && sent.MessageId == message.Id && sent.RecipientId == recipient);
    }

    [Fact]
    public void MarkRead_sets_ReadAtUtc()
    {
        var message = Message.Send(SomeConversationId, AccountId.New(), AccountId.New(), "Hi", DateTime.UtcNow);
        var readTime = DateTime.UtcNow;

        message.MarkRead(readTime);

        Assert.Equal(readTime, message.ReadAtUtc);
    }

    [Fact]
    public void MarkRead_is_idempotent_and_keeps_the_first_read_time()
    {
        var message = Message.Send(SomeConversationId, AccountId.New(), AccountId.New(), "Hi", DateTime.UtcNow);
        var firstRead = DateTime.UtcNow;
        message.MarkRead(firstRead);

        message.MarkRead(firstRead.AddMinutes(5));

        Assert.Equal(firstRead, message.ReadAtUtc);
    }
}
