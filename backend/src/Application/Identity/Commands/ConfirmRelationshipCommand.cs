namespace TutorFlow.Application.Identity.Commands;

// Corresponds to Relationship.Confirm() (PRODUCT_REQUIREMENTS.md IDR-4).
public sealed record ConfirmRelationshipCommand(Guid RelationshipId);
