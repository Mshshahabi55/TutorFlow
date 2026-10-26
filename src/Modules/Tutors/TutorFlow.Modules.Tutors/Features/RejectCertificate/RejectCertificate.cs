using FluentValidation;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TutorFlow.Modules.Tutors.Domain;
using TutorFlow.Modules.Tutors.Infrastructure;
using TutorFlow.Modules.Tutors.Persistence;
using TutorFlow.Shared;
using Wolverine;
using SubmitCertificateDocumentFeature = TutorFlow.Modules.Tutors.Features.SubmitCertificateDocument.SubmitCertificateDocument;

namespace TutorFlow.Modules.Tutors.Features.RejectCertificate;

/// <summary>Admin rejects a PendingReview certificate. A reason is required (mirrors
/// <c>RejectApplication</c>'s existing precedent exactly) and published via
/// <see cref="TutorCertificateRejected"/> so Notifications can email the tutor.</summary>
public static class RejectCertificate
{
    public sealed record Command(string Reason);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Reason).NotEmpty().WithMessage("A rejection reason is required.").MaximumLength(2000);
        }
    }

    internal sealed class Handler(
        TutorsDbContext db,
        ICertificateDocumentStorage storage,
        IOutputCacheStore outputCache,
        IMessageBus bus,
        TimeProvider clock,
        ILogger<Handler> logger)
    {
        public async Task<Result> HandleAsync(Guid applicationId, Guid certificateId, string reason, Guid adminUserId, CancellationToken ct)
        {
            var application = await db.TutorApplications.FirstOrDefaultAsync(a => a.Id == applicationId, ct);
            if (application is null)
            {
                return Result.Failure("Tutor application not found.");
            }

            var result = application.RejectCertificate(certificateId, reason, clock);
            if (result.IsFailure)
            {
                return result;
            }

            var certificateName = application.CertificateEntries.First(e => e.Id == certificateId).Name;

            await db.SaveChangesAsync(ct);
            await SubmitCertificateDocumentFeature.CleanupOrphanedFilesAsync(application, storage, logger, ct);
            await outputCache.EvictByTagAsync("tutors", ct);

            logger.LogInformation(
                "Certificate {CertificateId} on application {ApplicationId} rejected by admin {AdminUserId}",
                certificateId, applicationId, adminUserId);

            await bus.PublishAsync(new TutorCertificateRejected(
                application.UserId, application.Id, certificateId, certificateName, reason, clock.GetUtcNow()));

            return Result.Success();
        }
    }
}
