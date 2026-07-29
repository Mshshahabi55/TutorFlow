using TutorFlow.Domain.Communication;
using TutorFlow.Domain.Communication.Events;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Domain.Tests.Communication;

public class ConversationTests
{
    [Fact]
    public void Starting_a_conversation_records_both_participants()
    {
        var participantA = AccountId.New();
        var participantB = AccountId.New();

        var conversation = Conversation.Start(participantA, participantB, DateTime.UtcNow);

        Assert.Equal(participantA, conversation.ParticipantAId);
        Assert.Equal(participantB, conversation.ParticipantBId);
        Assert.Null(conversation.LastMessageAtUtc);
    }

    [Fact]
    public void Starting_raises_ConversationStarted()
    {
        var participantA = AccountId.New();
        var participantB = AccountId.New();

        var conversation = Conversation.Start(participantA, participantB, DateTime.UtcNow);

        Assert.Contains(
            conversation.DomainEvents,
            e => e is ConversationStarted started && started.ConversationId == conversation.Id);
    }

    [Fact]
    public void Starting_with_the_same_account_twice_fails()
    {
        var account = AccountId.New();

        Assert.Throws<InvalidOperationException>(() => Conversation.Start(account, account, DateTime.UtcNow));
    }

    [Fact]
    public void HasParticipant_is_true_for_either_side_and_false_for_a_stranger()
    {
        var participantA = AccountId.New();
        var participantB = AccountId.New();
        var conversation = Conversation.Start(participantA, participantB, DateTime.UtcNow);

        Assert.True(conversation.HasParticipant(participantA));
        Assert.True(conversation.HasParticipant(participantB));
        Assert.False(conversation.HasParticipant(AccountId.New()));
    }

    [Fact]
    public void OtherParticipant_returns_the_other_side()
    {
        var participantA = AccountId.New();
        var participantB = AccountId.New();
        var conversation = Conversation.Start(participantA, participantB, DateTime.UtcNow);

        Assert.Equal(participantB, conversation.OtherParticipant(participantA));
        Assert.Equal(participantA, conversation.OtherParticipant(participantB));
    }

    [Fact]
    public void RecordActivity_sets_LastMessageAtUtc()
    {
        var conversation = Conversation.Start(AccountId.New(), AccountId.New(), DateTime.UtcNow);
        var activityTime = DateTime.UtcNow;

        conversation.RecordActivity(activityTime);

        Assert.Equal(activityTime, conversation.LastMessageAtUtc);
    }
}
