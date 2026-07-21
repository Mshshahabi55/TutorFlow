using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Identity.Events;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Domain.Tests.Identity;

public class RelationshipTests
{
    [Fact]
    public void Relationship_starts_Invited()
    {
        var relationship = Relationship.Invite(AccountId.New(), AccountId.New(), AccountId.New());

        Assert.Equal(RelationshipStatus.Invited, relationship.Status);
    }

    [Fact]
    public void Relationship_can_be_confirmed()
    {
        var relationship = Relationship.Invite(AccountId.New(), AccountId.New(), AccountId.New());

        relationship.Confirm();

        Assert.Equal(RelationshipStatus.Confirmed, relationship.Status);
    }

    [Fact]
    public void Confirming_twice_fails()
    {
        var relationship = Relationship.Invite(AccountId.New(), AccountId.New(), AccountId.New());
        relationship.Confirm();

        Assert.Throws<InvalidOperationException>(() => relationship.Confirm());
    }

    [Fact]
    public void Inviting_raises_RelationshipInvited()
    {
        var relationship = Relationship.Invite(AccountId.New(), AccountId.New(), AccountId.New());

        Assert.Contains(
            relationship.DomainEvents,
            e => e is RelationshipInvited invited && invited.RelationshipId == relationship.Id);
    }

    [Fact]
    public void Confirming_raises_RelationshipConfirmed()
    {
        var relationship = Relationship.Invite(AccountId.New(), AccountId.New(), AccountId.New());

        relationship.Confirm();

        Assert.Contains(
            relationship.DomainEvents,
            e => e is RelationshipConfirmed confirmed && confirmed.RelationshipId == relationship.Id);
    }
}
