namespace TutorFlow.Application.Identity.Commands;

// Corresponds to Relationship.Invite(AccountId parentGuardianId, AccountId
// studentId) (PRODUCT_REQUIREMENTS.md IDR-4).
public sealed record CreateRelationshipInvitationCommand(Guid ParentGuardianId, Guid StudentId);
