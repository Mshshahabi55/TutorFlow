using TutorFlow.Application.Identity.Interfaces;
using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Tests.TestDoubles;

internal sealed class InMemoryRelationshipRepository : IRelationshipRepository
{
    private readonly Dictionary<Guid, Relationship> _relationships = new();

    public Task<Relationship?> GetByIdAsync(RelationshipId id, CancellationToken cancellationToken = default)
    {
        _relationships.TryGetValue(id.Value, out var relationship);
        return Task.FromResult(relationship);
    }

    public Task<IReadOnlyCollection<Relationship>> GetByAccountIdAsync(AccountId accountId, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<Relationship> matches = _relationships.Values
            .Where(r => r.ParentGuardianId == accountId || r.StudentId == accountId)
            .ToList();
        return Task.FromResult(matches);
    }

    public Task AddAsync(Relationship relationship, CancellationToken cancellationToken = default)
    {
        _relationships[relationship.Id.Value] = relationship;
        return Task.CompletedTask;
    }
}
