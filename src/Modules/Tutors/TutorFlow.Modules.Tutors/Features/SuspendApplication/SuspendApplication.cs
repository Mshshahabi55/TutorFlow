using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TutorFlow.Modules.Tutors.Domain;
using TutorFlow.Modules.Tutors.Persistence;
using TutorFlow.Shared;
using Wolverine;

namespace TutorFlow.Modules.Tutors.Features.SuspendApplication;

/// <summary>
/// Publishes <c>TutorUnpublished</c> (ADR-0024) whenever <c>TutorApplication.Suspend()</c>
/// succeeds — the de-listing counterpart to <c>PublishApplication</c>. Now mapped to a real admin
/// HTTP endpoint (ADR-0028); <see cref="Handler"/>'s signature is unchanged from before that ADR
/// so the existing <c>TutorPublishedPipelineTests</c> call site (which invokes this handler
/// directly) keeps working unmodified — only the additive <see cref="Request"/>/<see
/// cref="Validator"/> below exist to let the endpoint bind and validate the wire-level reason.
/// </summary>
public static class SuspendApplication
{
    public sealed record Request(string Reason);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Reason).NotEmpty().WithMessage("A suspension reason is required.").MaximumLength(2000);
        }
    }

    internal sealed class Handler(TutorsDbContext db, IMessageBus bus, TimeProvider clock, ILogger<Handler> logger)
    {
        public async Task<Result> HandleAsync(Guid applicationId, string reason, Guid adminUserId, CancellationToken ct)
        {
            var application = await db.TutorApplications.FirstOrDefaultAsync(a => a.Id == applicationId, ct);
            if (application is null)
            {
                return Result.Failure("Tutor application not found.");
            }

            var result = application.Suspend(reason, clock);
            if (result.IsFailure)
            {
                return result;
            }

            await db.SaveChangesAsync(ct);

            logger.LogInformation(
                "Tutor application {ApplicationId} suspended by admin {AdminUserId}",
                applicationId, adminUserId);

            await bus.PublishAsync(new TutorUnpublished(application.UserId, clock.GetUtcNow()));

            return Result.Success();
        }
    }
}
