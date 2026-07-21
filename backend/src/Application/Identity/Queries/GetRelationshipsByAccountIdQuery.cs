namespace TutorFlow.Application.Identity.Queries;

// Returns every Relationship in which the given account participates, as
// either the inviting Parent/Guardian or the invited Student
// (PRODUCT_REQUIREMENTS.md IDR-3; User Journey 5.2 step 6). An account with
// no Relationships yet is a valid, empty result, not a "not found" error.
public sealed record GetRelationshipsByAccountIdQuery(Guid AccountId);
