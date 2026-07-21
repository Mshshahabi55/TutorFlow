using TutorFlow.Application.Identity.Interfaces;
using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Tests.TestDoubles;

internal sealed class InMemoryAuthTokenRepository : IAuthTokenRepository
{
    private readonly Dictionary<Guid, AuthToken> _tokens = new();

    public Task AddAsync(AuthToken token, CancellationToken cancellationToken = default)
    {
        _tokens[token.Id.Value] = token;
        return Task.CompletedTask;
    }

    public Task<AuthToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        var token = _tokens.Values.SingleOrDefault(t => t.TokenHash == tokenHash);
        return Task.FromResult(token);
    }

    public Task<IReadOnlyCollection<AuthToken>> GetActiveByAccountIdAsync(
        AccountId accountId, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<AuthToken> active = _tokens.Values
            .Where(t => t.AccountId == accountId && t.RevokedAtUtc is null)
            .ToList();
        return Task.FromResult(active);
    }
}
