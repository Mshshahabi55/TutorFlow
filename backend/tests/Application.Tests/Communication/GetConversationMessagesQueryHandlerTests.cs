using TutorFlow.Application.Communication.Handlers;
using TutorFlow.Application.Communication.Queries;
using TutorFlow.Application.Tests.TestDoubles;
using TutorFlow.Domain.Communication;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Tests.Communication;

public class GetConversationMessagesQueryHandlerTests
{
    [Fact]
    public async Task Returns_messages_in_chronological_order()
    {
        var participantA = AccountId.New();
        var participantB = AccountId.New();
        var conversation = Conversation.Start(participantA, participantB, DateTime.UtcNow.AddMinutes(-10));
        var conversationRepository = new InMemoryConversationRepository();
        await conversationRepository.AddAsync(conversation);

        var first = Message.Send(conversation.Id, participantA, participantB, "First", DateTime.UtcNow.AddMinutes(-5));
        var second = Message.Send(conversation.Id, participantB, participantA, "Second", DateTime.UtcNow.AddMinutes(-1));
        var messageRepository = new InMemoryMessageRepository();
        await messageRepository.AddAsync(second);
        await messageRepository.AddAsync(first);

        var handler = new GetConversationMessagesQueryHandler(
            conversationRepository, messageRepository, StubCurrentUserProvider.AsStudent(participantA.Value));

        var result = await handler.Handle(new GetConversationMessagesQuery(conversation.Id.Value));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Equal("First", result.Value.ElementAt(0).Body);
        Assert.Equal("Second", result.Value.ElementAt(1).Body);
    }

    [Fact]
    public async Task A_non_participant_cannot_read_the_conversation()
    {
        var participantA = AccountId.New();
        var participantB = AccountId.New();
        var conversation = Conversation.Start(participantA, participantB, DateTime.UtcNow);
        var conversationRepository = new InMemoryConversationRepository();
        await conversationRepository.AddAsync(conversation);

        var handler = new GetConversationMessagesQueryHandler(
            conversationRepository, new InMemoryMessageRepository(),
            StubCurrentUserProvider.AsStudent(AccountId.New().Value));

        var result = await handler.Handle(new GetConversationMessagesQuery(conversation.Id.Value));

        Assert.True(result.IsFailure);
    }
}
