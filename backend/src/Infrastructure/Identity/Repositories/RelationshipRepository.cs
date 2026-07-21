using Microsoft.EntityFrameworkCore;
using TutorFlow.Application.Identity.Interfaces;
using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Identity.ValueObjects;
using TutorFlow.Infrastructure.Persistence;

namespace TutorFlow.Infrastructure.Identity.Repositories;

internal sealed class RelationshipRepository : IRelationshipRepository
{
    private readonly TutorFlowDbContext _dbContext;

    public RelationshipRepository(TutorFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Relationship?> GetByIdAsync(RelationshipId id, CancellationToken cancellationToken = default) =>
        _dbContext.Relationships.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<Relationship>> GetByAccountIdAsync(AccountId accountId, CancellationToken cancellationToken = default) =>
        await _dbContext.Relationships
            .AsNoTracking()
            .Where(r => r.ParentGuardianId == accountId || r.StudentId == accountId)
            .ToListAsync(cancellationToken);

    public Task AddAsync(Relationship relationship, CancellationToken cancellationToken = default)
    {
        _dbContext.Relationships.Add(relationship);
        return Task.CompletedTask;
    }
}
