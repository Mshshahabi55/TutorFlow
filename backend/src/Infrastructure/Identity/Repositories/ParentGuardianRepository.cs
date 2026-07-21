using Microsoft.EntityFrameworkCore;
using TutorFlow.Application.Identity.Interfaces;
using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Identity.ValueObjects;
using TutorFlow.Infrastructure.Persistence;

namespace TutorFlow.Infrastructure.Identity.Repositories;

internal sealed class ParentGuardianRepository : IParentGuardianRepository
{
    private readonly TutorFlowDbContext _dbContext;

    public ParentGuardianRepository(TutorFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ParentGuardian?> GetByIdAsync(AccountId id, CancellationToken cancellationToken = default) =>
        _dbContext.ParentGuardians.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<ParentGuardian?> GetByEmailAsync(EmailAddress email, CancellationToken cancellationToken = default) =>
        _dbContext.ParentGuardians.FirstOrDefaultAsync(p => p.Email == email, cancellationToken);

    public Task AddAsync(ParentGuardian parentGuardian, CancellationToken cancellationToken = default)
    {
        _dbContext.ParentGuardians.Add(parentGuardian);
        return Task.CompletedTask;
    }
}
