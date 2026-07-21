using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Identity.Interfaces;

public interface IAuthTokenRepository
{
    Task AddAsync(AuthToken token, CancellationToken cancellationToken = default);

    Task<AuthToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    // Administrator password reset revokes every existing session for the
    // Account immediately (docs/adr/ADR-017-authentication-mechanism-decision.md,
    // Launch Preparation Priority 1) — only tokens not already revoked need
    // to be revoked again.
    Task<IReadOnlyCollection<AuthToken>> GetActiveByAccountIdAsync(AccountId accountId, CancellationToken cancellationToken = default);
}
