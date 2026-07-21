using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Identity.Interfaces;

public interface IRelationshipRepository
{
    Task<Relationship?> GetByIdAsync(RelationshipId id, CancellationToken cancellationToken = default);

    // Every Relationship in which the given account is either the inviting
    // Parent/Guardian or the invited Student (PRODUCT_REQUIREMENTS.md IDR-3:
    // many-to-many in both directions) — serves User Journey 5.2 step 6.
    Task<IReadOnlyCollection<Relationship>> GetByAccountIdAsync(AccountId accountId, CancellationToken cancellationToken = default);

    Task AddAsync(Relationship relationship, CancellationToken cancellationToken = default);
}
