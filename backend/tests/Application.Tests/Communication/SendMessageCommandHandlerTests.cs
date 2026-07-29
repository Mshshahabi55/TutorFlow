using TutorFlow.Application.Common;
using TutorFlow.Application.Communication.Commands;
using TutorFlow.Application.Communication.Handlers;
using TutorFlow.Application.Tests.TestDoubles;
using TutorFlow.Domain.Communication;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Tests.Communication;

public class SendMessageCommandHandlerTests
{
    [Fact]
    public async Task A_participant_can_send_a_message_and_it_persists_and_updates_conversation_activity()
    {
        var participantA = AccountId.New();
        var participantB = AccountId.New();
        var conversation = Conversation.Start(participantA, participantB, DateTime.UtcNow.AddMinutes(-5));
        var conversationRepository = new InMemoryConversationRepository();
        await conversationRepository.AddAsync(conversation);
        var messageRepository = new InMemoryMessageRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new SendMessageCommandHandler(
            conversationRepository, messageRepository, StubCurrentUserProvider.AsStudent(participantA.Value),
            new FixedDateTimeProvider(DateTime.UtcNow), unitOfWork);

        var result = await handler.Handle(new SendMessageCommand(conversation.Id.Value, "Hello there"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Hello there", result.Value.Body);
        Assert.Equal(participantB.Value, result.Value.RecipientId);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
        Assert.NotNull(conversation.LastMessageAtUtc);
    }

    [Fact]
    public async Task A_non_participant_cannot_send_a_message()
    {
        var participantA = AccountId.New();
        var participantB = AccountId.New();
        var conversation = Conversation.Start(participantA, participantB, DateTime.UtcNow);
        var conversationRepository = new InMemoryConversationRepository();
        await conversationRepository.AddAsync(conversation);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new SendMessageCommandHandler(
            conversationRepository, new InMemoryMessageRepository(), StubCurrentUserProvider.AsStudent(AccountId.New().Value),
            new FixedDateTimeProvider(DateTime.UtcNow), unitOfWork);

        var result = await handler.Handle(new SendMessageCommand(conversation.Id.Value, "Hi"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Authorization, result.Error.Type);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Sending_to_a_nonexistent_conversation_fails()
    {
        var handler = new SendMessageCommandHandler(
            new InMemoryConversationRepository(), new InMemoryMessageRepository(),
            StubCurrentUserProvider.AsStudent(AccountId.New().Value), new FixedDateTimeProvider(DateTime.UtcNow),
            new FakeUnitOfWork());

        var result = await handler.Handle(new SendMessageCommand(Guid.NewGuid(), "Hi"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Domain, result.Error.Type);
    }

    [Fact]
    public async Task Sending_an_empty_body_fails_before_touching_the_repository()
    {
        var participantA = AccountId.New();
        var participantB = AccountId.New();
        var conversation = Conversation.Start(participantA, participantB, DateTime.UtcNow);
        var conversationRepository = new InMemoryConversationRepository();
        await conversationRepository.AddAsync(conversation);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new SendMessageCommandHandler(
            conversationRepository, new InMemoryMessageRepository(), StubCurrentUserProvider.AsStudent(participantA.Value),
            new FixedDateTimeProvider(DateTime.UtcNow), unitOfWork);

        var result = await handler.Handle(new SendMessageCommand(conversation.Id.Value, "   "));

        Assert.True(result.IsFailure);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }
}
