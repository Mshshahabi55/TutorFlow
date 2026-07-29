using TutorFlow.Application.Common;
using TutorFlow.Application.Communication.Commands;
using TutorFlow.Application.Communication.Handlers;
using TutorFlow.Application.Tests.TestDoubles;
using TutorFlow.Domain.Communication;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Tests.Communication;

public class MarkConversationReadCommandHandlerTests
{
    [Fact]
    public async Task Marks_only_the_readers_own_unread_messages_as_read()
    {
        var sender = AccountId.New();
        var reader = AccountId.New();
        var conversation = Conversation.Start(sender, reader, DateTime.UtcNow.AddMinutes(-10));
        var conversationRepository = new InMemoryConversationRepository();
        await conversationRepository.AddAsync(conversation);

        var messageFromSender = Message.Send(conversation.Id, sender, reader, "Hi", DateTime.UtcNow.AddMinutes(-5));
        var messageFromReader = Message.Send(conversation.Id, reader, sender, "Hey", DateTime.UtcNow.AddMinutes(-4));
        var messageRepository = new InMemoryMessageRepository();
        await messageRepository.AddAsync(messageFromSender);
        await messageRepository.AddAsync(messageFromReader);

        var unitOfWork = new FakeUnitOfWork();
        var handler = new MarkConversationReadCommandHandler(
            conversationRepository, messageRepository, StubCurrentUserProvider.AsStudent(reader.Value),
            new FixedDateTimeProvider(DateTime.UtcNow), unitOfWork);

        var result = await handler.Handle(new MarkConversationReadCommand(conversation.Id.Value));

        Assert.True(result.IsSuccess);
        Assert.NotNull(messageFromSender.ReadAtUtc);
        Assert.Null(messageFromReader.ReadAtUtc);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Marking_read_with_nothing_unread_does_not_call_SaveChanges()
    {
        var sender = AccountId.New();
        var reader = AccountId.New();
        var conversation = Conversation.Start(sender, reader, DateTime.UtcNow);
        var conversationRepository = new InMemoryConversationRepository();
        await conversationRepository.AddAsync(conversation);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new MarkConversationReadCommandHandler(
            conversationRepository, new InMemoryMessageRepository(), StubCurrentUserProvider.AsStudent(reader.Value),
            new FixedDateTimeProvider(DateTime.UtcNow), unitOfWork);

        var result = await handler.Handle(new MarkConversationReadCommand(conversation.Id.Value));

        Assert.True(result.IsSuccess);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task A_non_participant_cannot_mark_a_conversation_read()
    {
        var sender = AccountId.New();
        var reader = AccountId.New();
        var conversation = Conversation.Start(sender, reader, DateTime.UtcNow);
        var conversationRepository = new InMemoryConversationRepository();
        await conversationRepository.AddAsync(conversation);
        var handler = new MarkConversationReadCommandHandler(
            conversationRepository, new InMemoryMessageRepository(), StubCurrentUserProvider.AsStudent(AccountId.New().Value),
            new FixedDateTimeProvider(DateTime.UtcNow), new FakeUnitOfWork());

        var result = await handler.Handle(new MarkConversationReadCommand(conversation.Id.Value));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Authorization, result.Error.Type);
    }
}
