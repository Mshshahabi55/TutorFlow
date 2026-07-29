using TutorFlow.Application.Communication.Handlers;
using TutorFlow.Application.Communication.Queries;
using TutorFlow.Application.Tests.TestDoubles;
using TutorFlow.Domain.Communication;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Tests.Communication;

public class GetMyConversationsQueryHandlerTests
{
    [Fact]
    public async Task Lists_only_conversations_the_caller_participates_in_with_unread_counts()
    {
        var me = AccountId.New();
        var other = AccountId.New();
        var stranger1 = AccountId.New();
        var stranger2 = AccountId.New();

        var myConversation = Conversation.Start(me, other, DateTime.UtcNow.AddMinutes(-10));
        var strangersConversation = Conversation.Start(stranger1, stranger2, DateTime.UtcNow);

        var conversationRepository = new InMemoryConversationRepository();
        await conversationRepository.AddAsync(myConversation);
        await conversationRepository.AddAsync(strangersConversation);

        var unreadMessage = Message.Send(myConversation.Id, other, me, "Hi", DateTime.UtcNow.AddMinutes(-1));
        var messageRepository = new InMemoryMessageRepository();
        await messageRepository.AddAsync(unreadMessage);

        var handler = new GetMyConversationsQueryHandler(
            conversationRepository, messageRepository, StubCurrentUserProvider.AsStudent(me.Value));

        var result = await handler.Handle(new GetMyConversationsQuery());

        Assert.True(result.IsSuccess);
        var conversation = Assert.Single(result.Value);
        Assert.Equal(myConversation.Id.Value, conversation.ConversationId);
        Assert.Equal(other.Value, conversation.OtherParticipantId);
        Assert.Equal(1, conversation.UnreadCount);
        Assert.Equal("Hi", conversation.LastMessagePreview);
    }

    [Fact]
    public async Task An_unauthenticated_caller_is_rejected()
    {
        var handler = new GetMyConversationsQueryHandler(
            new InMemoryConversationRepository(), new InMemoryMessageRepository(), StubCurrentUserProvider.Unauthenticated());

        var result = await handler.Handle(new GetMyConversationsQuery());

        Assert.True(result.IsFailure);
    }
}
