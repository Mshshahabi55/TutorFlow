using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TutorFlow.Modules.Tutors.Domain;
using TutorFlow.Modules.Tutors.Infrastructure;
using TutorFlow.Modules.Tutors.Persistence;
using SubmitCertificateDocumentFeature = TutorFlow.Modules.Tutors.Features.SubmitCertificateDocument.SubmitCertificateDocument;

namespace TutorFlow.Modules.Tutors.Features.WithdrawCertificateDocument;

/// <summary>Tutor withdraws a not-yet-decided or rejected document without replacing it.</summary>
public static class WithdrawCertificateDocument
{
    internal sealed class Handler(TutorsDbContext db, ICertificateDocumentStorage storage, TimeProvider clock, ILogger<Handler> logger)
    {
        public async Task<Result> HandleAsync(Guid userId, Guid certificateId, CancellationToken ct)
        {
            var application = await db.TutorApplications.FirstOrDefaultAsync(a => a.UserId == userId, ct);
            if (application is null)
            {
                return Result.Failure("No tutor application found for this user.");
            }

            var result = application.WithdrawCertificateDocument(certificateId, clock);
            if (result.IsFailure)
            {
                return result;
            }

            await db.SaveChangesAsync(ct);
            await SubmitCertificateDocumentFeature.CleanupOrphanedFilesAsync(application, storage, logger, ct);
            return Result.Success();
        }
    }
}
