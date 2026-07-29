using TutorFlow.Domain.Common;
using TutorFlow.Domain.Identity.Events;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Domain.Identity;

// The link between one Parent/Guardian and one Student, established only by
// invitation and confirmation — never unilaterally by a single party
// (PRODUCT_REQUIREMENTS.md IDR-3, IDR-4; DOMAIN_MODEL.md: Invariant 7).
// InvitedByAccountId records which of the two named parties issued the
// invitation — the domain fact needed to enforce "only the counterparty may
// confirm" (AUTHORIZATION_MATRIX.md §4.1; Launch Preparation, Priority 2,
// WP4 Priority 4). The authorization decision itself is still not made
// here — this only records the fact; ConfirmRelationshipCommandHandler
// evaluates it.
public sealed class Relationship : AggregateRoot<RelationshipId>
{
    private Relationship(
        RelationshipId id,
        AccountId parentGuardianId,
        AccountId studentId,
        AccountId invitedByAccountId,
        RelationshipStatus status) : base(id)
    {
        ParentGuardianId = parentGuardianId;
        StudentId = studentId;
        InvitedByAccountId = invitedByAccountId;
        Status = status;
    }

    public AccountId ParentGuardianId { get; }

    public AccountId StudentId { get; }

    // Always equal to either ParentGuardianId or StudentId — whichever
    // party's own account issued this invitation.
    public AccountId InvitedByAccountId { get; }

    public RelationshipStatus Status { get; private set; }

    public static Relationship Invite(AccountId parentGuardianId, AccountId studentId, AccountId invitedByAccountId)
    {
        Guard.Against.Null(parentGuardianId, nameof(parentGuardianId));
        Guard.Against.Null(studentId, nameof(studentId));
        Guard.Against.Null(invitedByAccountId, nameof(invitedByAccountId));

        var relationship = new Relationship(
            RelationshipId.New(),
            parentGuardianId,
            studentId,
            invitedByAccountId,
            RelationshipStatus.Invited);

        relationship.RaiseDomainEvent(
            new RelationshipInvited(relationship.Id, parentGuardianId, studentId));

        return relationship;
    }

    public void Confirm()
    {
        if (Status != RelationshipStatus.Invited)
        {
            throw new InvalidOperationException("Only an invited Relationship can be confirmed.");
        }

        Status = RelationshipStatus.Confirmed;
        RaiseDomainEvent(new RelationshipConfirmed(Id, ParentGuardianId, StudentId, InvitedByAccountId));
    }
}
