using Microsoft.EntityFrameworkCore;
using TutorFlow.Application.Identity.Interfaces;
using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Identity.ValueObjects;
using TutorFlow.Infrastructure.Persistence;

namespace TutorFlow.Infrastructure.Identity.Repositories;

internal sealed class AuthTokenRepository : IAuthTokenRepository
{
    private readonly TutorFlowDbContext _dbContext;

    public AuthTokenRepository(TutorFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddAsync(AuthToken token, CancellationToken cancellationToken = default)
    {
        _dbContext.AuthTokens.Add(token);
        return Task.CompletedTask;
    }

    public Task<AuthToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        _dbContext.AuthTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

    public async Task<IReadOnlyCollection<AuthToken>> GetActiveByAccountIdAsync(
        AccountId accountId, CancellationToken cancellationToken = default) =>
        await _dbContext.AuthTokens
            .Where(t => t.AccountId == accountId && t.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);
}
