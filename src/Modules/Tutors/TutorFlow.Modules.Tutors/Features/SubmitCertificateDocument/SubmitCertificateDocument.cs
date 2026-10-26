using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TutorFlow.Modules.Tutors.Domain;
using TutorFlow.Modules.Tutors.Infrastructure;
using TutorFlow.Modules.Tutors.Persistence;

namespace TutorFlow.Modules.Tutors.Features.SubmitCertificateDocument;

/// <summary>Tutor uploads/replaces a document for one of their own certificates — always moves it
/// to PendingReview (never auto-verifies; see <c>CertificateEntry</c>'s own doc comment).</summary>
public static class SubmitCertificateDocument
{
    public sealed record Response(Guid CertificateId, string VerificationStatus);

    internal sealed class Handler(TutorsDbContext db, ICertificateDocumentStorage storage, TimeProvider clock, ILogger<Handler> logger)
    {
        public async Task<Result<Response>> HandleAsync(Guid userId, Guid certificateId, Microsoft.AspNetCore.Http.IFormFile? file, CancellationToken ct)
        {
            if (file is null)
            {
                return Result.Failure<Response>("No file was uploaded.");
            }

            var application = await db.TutorApplications.FirstOrDefaultAsync(a => a.UserId == userId, ct);
            if (application is null)
            {
                return Result.Failure<Response>("No tutor application found for this user.");
            }

            var saveResult = await storage.SaveAsync(file, ct);
            if (saveResult.IsFailure)
            {
                return Result.Failure<Response>([.. saveResult.Errors]);
            }

            var domainResult = application.SubmitCertificateDocument(certificateId, saveResult.Value, clock);
            if (domainResult.IsFailure)
            {
                // The file is already written at this point — nothing was persisted referencing
                // it, so it's an orphan from the moment it's written. Best-effort immediate
                // cleanup; the sweep service is the backstop if this also fails.
                storage.TryDelete(saveResult.Value);
                return Result.Failure<Response>([.. domainResult.Errors]);
            }

            await db.SaveChangesAsync(ct);
            await CleanupOrphanedFilesAsync(application, storage, logger, ct);

            var entry = application.CertificateEntries.First(e => e.Id == certificateId);
            return Result.Success(new Response(entry.Id, entry.VerificationStatus.ToString()));
        }
    }

    /// <summary>Shared by every feature in this vertical that can populate
    /// <c>TutorApplication.PendingFileCleanup</c> — deletes each queued file only after
    /// <c>SaveChangesAsync</c> has already succeeded (never before), logs any failure (the sweep
    /// service retries later), then clears the queue.</summary>
    internal static Task CleanupOrphanedFilesAsync(
        Domain.TutorApplication application, ICertificateDocumentStorage storage, ILogger logger, CancellationToken ct)
    {
        foreach (var fileReference in application.PendingFileCleanup)
        {
            if (!storage.TryDelete(fileReference))
            {
                logger.LogWarning("Failed to delete orphaned certificate document for application {ApplicationId} — the orphan sweep will retry", application.Id);
            }
        }

        application.ClearPendingFileCleanup();
        return Task.CompletedTask;
    }
}
