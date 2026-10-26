using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TutorFlow.Modules.Tutors.Persistence;

namespace TutorFlow.Modules.Tutors.Infrastructure;

/// <summary>Orphan-file sweep for private certificate-document storage — the retry/reconciliation
/// mechanism for two cases the synchronous request path can't fully close: (1) a post-decision
/// delete (<c>VerifyCertificate</c>/<c>RejectCertificate</c>/withdraw/replace) that failed and was
/// only logged, not retried in-request; (2) a document written to disk by
/// <see cref="LocalCertificateDocumentStorage.SaveAsync"/> whose corresponding DB write never
/// committed (crash/cancellation between the two steps). Same <see cref="BackgroundService"/> +
/// <see cref="PeriodicTimer"/> shape as <c>AbandonedCheckoutClaimReconciliationService</c> — no
/// job-scheduling library exists in this codebase.
///
/// Deliberately conservative: only deletes a file that is (a) not referenced by any
/// <c>CertificateEntry.FileReference</c> in the database, AND (b) older than
/// <see cref="MinimumOrphanAgeMinutes"/> — the age guard exists specifically so this sweep can
/// never race and delete a file that was just written but whose owning request hasn't reached
/// <c>SaveChangesAsync</c> yet.</summary>
public sealed class CertificateDocumentCleanupService(
    IServiceScopeFactory scopeFactory,
    TimeProvider clock,
    IOptions<CertificateDocumentCleanupOptions> options,
    ILogger<CertificateDocumentCleanupService> logger) : BackgroundService
{
    private const int MinimumOrphanAgeMinutes = 10;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(options.Value.IntervalMinutes), clock);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await SweepOnceAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Certificate-document orphan sweep tick failed");
            }
        }
    }

    /// <summary>Public so tests can drive one sweep pass deterministically rather than waiting on
    /// the timer.</summary>
    public async Task SweepOnceAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TutorsDbContext>();
        var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

        var root = LocalCertificateDocumentStorage.ResolveRootPath(env, options.Value.RootPath);
        if (!Directory.Exists(root))
        {
            return;
        }

        var referenced = (await db.TutorApplications.AsNoTracking()
                .Select(a => a.CertificateEntries)
                .ToListAsync(ct))
            .SelectMany(entries => entries)
            .Select(e => e.FileReference)
            .Where(r => r is not null)
            .ToHashSet()!;

        var cutoff = clock.GetUtcNow() - TimeSpan.FromMinutes(MinimumOrphanAgeMinutes);
        var deleted = 0;
        foreach (var path in Directory.EnumerateFiles(root))
        {
            var fileName = Path.GetFileName(path);
            if (fileName.EndsWith(".tmp", StringComparison.Ordinal))
            {
                // An interrupted upload's temp file — same orphan logic, same age guard.
                continue;
            }

            if (referenced.Contains(fileName))
            {
                continue;
            }

            if (File.GetLastWriteTimeUtc(path) > cutoff)
            {
                continue;
            }

            try
            {
                File.Delete(path);
                deleted++;
            }
            catch (IOException ex)
            {
                logger.LogWarning(ex, "Certificate-document orphan sweep could not delete an unreferenced file — will retry next tick");
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.LogWarning(ex, "Certificate-document orphan sweep could not delete an unreferenced file — will retry next tick");
            }
        }

        if (deleted > 0)
        {
            logger.LogInformation("Certificate-document orphan sweep deleted {Count} unreferenced file(s)", deleted);
        }
    }
}

public sealed class CertificateDocumentCleanupOptions
{
    public int IntervalMinutes { get; set; } = 30;

    public string? RootPath { get; set; }
}
