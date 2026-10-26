using FluentValidation;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TutorFlow.Modules.Tutors.Domain;
using TutorFlow.Modules.Tutors.Infrastructure;
using TutorFlow.Modules.Tutors.Persistence;
using TutorFlow.Shared;
using Wolverine;
using Wolverine.Attributes;
using Wolverine.Persistence;
using SubmitCertificateDocumentFeature = TutorFlow.Modules.Tutors.Features.SubmitCertificateDocument.SubmitCertificateDocument;

namespace TutorFlow.Modules.Tutors.Features.RejectCertificate;

/// <summary>Admin rejects a PendingReview certificate. A reason is required (mirrors
/// <c>RejectApplication</c>'s existing precedent exactly) and published via
/// <see cref="TutorCertificateRejected"/> so Notifications can email the tutor.
///
/// A44 #103: unlike <c>PublishApplication</c>/<c>SuspendApplication</c>, <see cref="Handler"/>
/// still calls <c>SaveChangesAsync</c> itself, immediately — <c>CleanupOrphanedFilesAsync</c>'s
/// own doc comment requires the file delete to happen only after that commit, never before, so
/// the entity change cannot be deferred to a single trailing <c>SaveChangesAsync</c> the way the
/// other two features' can. This makes the business write and the outgoing
/// <c>TutorCertificateRejected</c> envelope two separate commits within one chain invocation —
/// structurally the same gap <c>ConfirmBooking.Handler</c>'s <c>AlreadyConfirmed</c> path already
/// accepts for the identical reason (its write is an immediate <c>ExecuteUpdateAsync</c>) —
/// compensated the same way: idempotent republish from persisted state on retry, below.
/// </summary>
public static class RejectCertificate
{
    /// <summary>Wire-level request body, bound and validated from the HTTP request — distinct
    /// from <see cref="Dispatch"/>, the local Wolverine command below.</summary>
    public sealed record Command(string Reason);

    /// <summary>A44 #103 local-command dispatch payload — named separately from <see cref="Command"/>
    /// (the wire-level request DTO above) since it carries the route/claims values the endpoint
    /// already resolved (<c>ApplicationId</c>/<c>CertificateId</c>/<c>AdminUserId</c>), not what
    /// the HTTP client sent.</summary>
    public sealed record Dispatch(Guid ApplicationId, Guid CertificateId, string Reason, Guid AdminUserId);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Reason).NotEmpty().WithMessage("A rejection reason is required.").MaximumLength(2000);
        }
    }

    public sealed class Handler(
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

            var entry = application.CertificateEntries.FirstOrDefault(e => e.Id == certificateId);
            if (entry is null)
            {
                return Result.Failure("Certificate not found.");
            }

            // A44 #103 idempotent-recovery branch — see this type's own remarks. Already-Rejected
            // republishes instead of failing, in case an earlier attempt's write committed but the
            // caller never saw the response.
            if (entry.VerificationStatus != Domain.CertificateVerificationStatus.Rejected)
            {
                var result = application.RejectCertificate(certificateId, reason, clock);
                if (result.IsFailure)
                {
                    return result;
                }

                await db.SaveChangesAsync(ct);
                await SubmitCertificateDocumentFeature.CleanupOrphanedFilesAsync(application, storage, logger, ct);
                await outputCache.EvictByTagAsync("tutors", ct);
            }

            var certificateName = entry.Name;

            logger.LogInformation(
                "Certificate {CertificateId} on application {ApplicationId} rejected by admin {AdminUserId}",
                certificateId, applicationId, adminUserId);

            await bus.PublishAsync(new TutorCertificateRejected(
                application.UserId, application.Id, certificateId, certificateName, reason, clock.GetUtcNow()));

            return Result.Success();
        }
    }
}

/// <summary>
/// A44 #103: local-command dispatch target — see <c>PublishApplicationConsumer</c>'s remarks for
/// why it's named with the "Consumer" suffix and why Lightweight (not Eager) mode is required
/// here (<c>TutorsDbContext</c>'s <c>EnableRetryOnFailure()</c> registration rejects Eager's
/// explicit transaction).
/// </summary>
[WolverineHandler]
[Transactional(typeof(TutorsDbContext), Mode = TransactionMiddlewareMode.Lightweight)]
public sealed class RejectCertificateConsumer(RejectCertificate.Handler handler)
{
    public Task<Result> HandleAsync(RejectCertificate.Dispatch command, CancellationToken ct) =>
        handler.HandleAsync(command.ApplicationId, command.CertificateId, command.Reason, command.AdminUserId, ct);
}
