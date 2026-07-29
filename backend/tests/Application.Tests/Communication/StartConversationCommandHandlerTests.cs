using TutorFlow.Application.Common;
using TutorFlow.Application.Communication.Commands;
using TutorFlow.Application.Communication.Handlers;
using TutorFlow.Application.Tests.TestDoubles;
using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Tests.Communication;

public class StartConversationCommandHandlerTests
{
    private static StartConversationCommandHandler CreateHandler(
        InMemoryConversationRepository conversationRepository,
        InMemoryMessageRepository messageRepository,
        InMemoryTutorRepository tutorRepository,
        ICurrentUserProvider currentUserProvider,
        FakeUnitOfWork unitOfWork) =>
        new(conversationRepository, messageRepository, tutorRepository, currentUserProvider,
            new FixedDateTimeProvider(DateTime.UtcNow), unitOfWork);

    [Fact]
    public async Task A_Student_can_start_a_conversation_with_an_existing_Tutor()
    {
        var tutorRepository = new InMemoryTutorRepository();
        var tutor = Tutor.Register(EmailAddress.Of("t@example.com"), PasswordHash.Of("hash"));
        await tutorRepository.AddAsync(tutor);
        var studentId = AccountId.New();
        var handler = CreateHandler(
            new InMemoryConversationRepository(), new InMemoryMessageRepository(), tutorRepository,
            StubCurrentUserProvider.AsStudent(studentId.Value), new FakeUnitOfWork());

        var result = await handler.Handle(new StartConversationCommand(tutor.Id.Value));

        Assert.True(result.IsSuccess);
        Assert.Equal(tutor.Id.Value, result.Value.OtherParticipantId);
        Assert.Equal(0, result.Value.UnreadCount);
    }

    [Fact]
    public async Task Starting_with_the_same_two_participants_twice_returns_the_existing_conversation()
    {
        var tutorRepository = new InMemoryTutorRepository();
        var tutor = Tutor.Register(EmailAddress.Of("t2@example.com"), PasswordHash.Of("hash"));
        await tutorRepository.AddAsync(tutor);
        var studentId = AccountId.New();
        var conversationRepository = new InMemoryConversationRepository();
        var handler = CreateHandler(
            conversationRepository, new InMemoryMessageRepository(), tutorRepository,
            StubCurrentUserProvider.AsStudent(studentId.Value), new FakeUnitOfWork());

        var first = await handler.Handle(new StartConversationCommand(tutor.Id.Value));
        var second = await handler.Handle(new StartConversationCommand(tutor.Id.Value));

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(first.Value.ConversationId, second.Value.ConversationId);
    }

    [Fact]
    public async Task A_Tutor_cannot_unilaterally_start_a_new_conversation()
    {
        var studentId = AccountId.New();
        var tutorId = AccountId.New();
        var handler = CreateHandler(
            new InMemoryConversationRepository(), new InMemoryMessageRepository(), new InMemoryTutorRepository(),
            StubCurrentUserProvider.AsTutor(tutorId.Value), new FakeUnitOfWork());

        var result = await handler.Handle(new StartConversationCommand(studentId.Value));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Authorization, result.Error.Type);
    }

    [Fact]
    public async Task An_Admin_can_start_a_conversation_with_anyone()
    {
        var adminId = AccountId.New();
        var anyAccountId = AccountId.New();
        var handler = CreateHandler(
            new InMemoryConversationRepository(), new InMemoryMessageRepository(), new InMemoryTutorRepository(),
            StubCurrentUserProvider.AsAdminStaff(adminId.Value), new FakeUnitOfWork());

        var result = await handler.Handle(new StartConversationCommand(anyAccountId.Value));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Cannot_start_a_conversation_with_yourself()
    {
        var accountId = AccountId.New();
        var handler = CreateHandler(
            new InMemoryConversationRepository(), new InMemoryMessageRepository(), new InMemoryTutorRepository(),
            StubCurrentUserProvider.AsStudent(accountId.Value), new FakeUnitOfWork());

        var result = await handler.Handle(new StartConversationCommand(accountId.Value));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Domain, result.Error.Type);
    }

    [Fact]
    public async Task A_Student_targeting_a_nonexistent_Tutor_fails()
    {
        var studentId = AccountId.New();
        var handler = CreateHandler(
            new InMemoryConversationRepository(), new InMemoryMessageRepository(), new InMemoryTutorRepository(),
            StubCurrentUserProvider.AsStudent(studentId.Value), new FakeUnitOfWork());

        var result = await handler.Handle(new StartConversationCommand(AccountId.New().Value));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Domain, result.Error.Type);
    }
}
