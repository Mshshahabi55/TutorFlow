using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TutorFlow.Infrastructure.Persistence;

namespace TutorFlow.Infrastructure.Common;

// Session cleanup (Launch Preparation, Priority 1). Purely storage hygiene —
// an already-expired or already-revoked token was already audited at
// issuance/revocation time (LoginSucceeded/logout); its later physical
// deletion is not itself a business fact, so this bypasses IUnitOfWork/
// Domain Event dispatch deliberately, consistent with every other pure
// Infrastructure-only concern in this codebase. Runs in-process
// (Microsoft.Extensions.Hosting.BackgroundService) rather than as a
// separately deployed job — not "new infrastructure" in the sense this
// project's other ADRs use that phrase (ADR-016's own Outbox discussion
// draws exactly this line).
internal sealed class AuthTokenCleanupService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(30);

    // Kept for a window after expiry/revocation rather than deleted
    // immediately, in case a recent session needs to be inspected (e.g.
    // while investigating a security incident) before it is purged.
    private static readonly TimeSpan RetentionAfterExpiry = TimeSpan.FromHours(24);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuthTokenCleanupService> _logger;

    public AuthTokenCleanupService(IServiceScopeFactory scopeFactory, ILogger<AuthTokenCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "AuthToken cleanup pass failed; will retry on the next interval.");
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown.
            }
        }
    }

    private async Task CleanupAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TutorFlowDbContext>();

        var cutoff = DateTime.UtcNow.Subtract(RetentionAfterExpiry);
        var stale = await dbContext.AuthTokens
            .Where(t => t.ExpiresAtUtc < cutoff || (t.RevokedAtUtc != null && t.RevokedAtUtc < cutoff))
            .ToListAsync(cancellationToken);

        if (stale.Count == 0)
        {
            return;
        }

        dbContext.AuthTokens.RemoveRange(stale);
        await dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("AuthToken cleanup removed {Count} stale token(s).", stale.Count);
    }
}
