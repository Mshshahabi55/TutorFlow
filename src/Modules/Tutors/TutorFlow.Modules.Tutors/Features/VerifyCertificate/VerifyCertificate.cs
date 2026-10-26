using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TutorFlow.Modules.Tutors.Domain;
using TutorFlow.Modules.Tutors.Infrastructure;
using TutorFlow.Modules.Tutors.Persistence;
using SubmitCertificateDocumentFeature = TutorFlow.Modules.Tutors.Features.SubmitCertificateDocument.SubmitCertificateDocument;

namespace TutorFlow.Modules.Tutors.Features.VerifyCertificate;

/// <summary>Admin approves a PendingReview certificate. Deliberately separate from
/// <c>ApproveApplication</c> — verifying every certificate is never implied by approving the whole
/// application (ADR-0028 precedent).</summary>
public static class VerifyCertificate
{
    internal sealed class Handler(
        TutorsDbContext db,
        ICertificateDocumentStorage storage,
        IOutputCacheStore outputCache,
        TimeProvider clock,
        ILogger<Handler> logger)
    {
        public async Task<Result> HandleAsync(Guid applicationId, Guid certificateId, Guid adminUserId, CancellationToken ct)
        {
            var application = await db.TutorApplications.FirstOrDefaultAsync(a => a.Id == applicationId, ct);
            if (application is null)
            {
                return Result.Failure("Tutor application not found.");
            }

            var result = application.VerifyCertificate(certificateId, clock);
            if (result.IsFailure)
            {
                return result;
            }

            await db.SaveChangesAsync(ct);
            await SubmitCertificateDocumentFeature.CleanupOrphanedFilesAsync(application, storage, logger, ct);

            // Immediate invalidation (not just the 5-minute TTL) — a newly-verified badge should
            // not have to wait out the cache window to appear on the public profile.
            await outputCache.EvictByTagAsync("tutors", ct);

            logger.LogInformation(
                "Certificate {CertificateId} on application {ApplicationId} verified by admin {AdminUserId}",
                certificateId, applicationId, adminUserId);

            return Result.Success();
        }
    }
}
