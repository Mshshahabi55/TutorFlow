using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TutorFlow.Modules.Tutors.Domain;
using TutorFlow.Modules.Tutors.Persistence;
using TutorFlow.Shared;
using Wolverine;
using Wolverine.Attributes;
using Wolverine.Persistence;

namespace TutorFlow.Modules.Tutors.Features.SuspendApplication;

/// <summary>
/// Publishes <c>TutorUnpublished</c> (ADR-0024) whenever <c>TutorApplication.Suspend()</c>
/// succeeds — the de-listing counterpart to <c>PublishApplication</c>. Mapped to a real admin
/// HTTP endpoint (ADR-0028); the endpoint binds and validates the wire-level reason via
/// <see cref="Request"/>/<see cref="Validator"/>.
///
/// A44 #103: same fix as <c>PublishApplication</c> — the HTTP endpoint dispatches
/// <see cref="Command"/> via <c>IMessageBus.InvokeAsync</c> instead of calling
/// <see cref="Handler"/> directly, so <see cref="SuspendApplicationConsumer"/>'s
/// <c>[Transactional(Lightweight)]</c> middleware's single <c>SaveChangesAsync</c> call commits
/// the status change and the outgoing <c>TutorUnpublished</c> envelope together. <see
/// cref="Handler"/> no longer calls <c>SaveChangesAsync</c> itself and must only be reached
/// through that chain.
/// </summary>
public static class SuspendApplication
{
    public sealed record Command(Guid ApplicationId, string Reason, Guid AdminUserId);

    public sealed record Request(string Reason);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Reason).NotEmpty().WithMessage("A suspension reason is required.").MaximumLength(2000);
        }
    }

    public sealed class Handler(TutorsDbContext db, IMessageBus bus, TimeProvider clock, ILogger<Handler> logger)
    {
        public async Task<Result> HandleAsync(Guid applicationId, string reason, Guid adminUserId, CancellationToken ct)
        {
            var application = await db.TutorApplications.FirstOrDefaultAsync(a => a.Id == applicationId, ct);
            if (application is null)
            {
                return Result.Failure("Tutor application not found.");
            }

            // A44 #103 idempotent-recovery branch (mirrors PublishApplication.Handler's — see its
            // own remarks for the full reasoning): if already Suspended, republish rather than
            // fail, in case an earlier attempt's write committed but the caller never saw the
            // response.
            if (application.Status != TutorApplicationStatus.Suspended)
            {
                var result = application.Suspend(reason, clock);
                if (result.IsFailure)
                {
                    return result;
                }
            }

            logger.LogInformation(
                "Tutor application {ApplicationId} suspended by admin {AdminUserId}",
                applicationId, adminUserId);

            await bus.PublishAsync(new TutorUnpublished(application.UserId, clock.GetUtcNow()));

            return Result.Success();
        }
    }
}

/// <summary>
/// A44 #103: local-command dispatch target — see <c>PublishApplicationConsumer</c>'s remarks for
/// why it's named with the "Consumer" suffix and why Lightweight (not Eager) mode is correct and
/// required here (<c>TutorsDbContext</c>'s <c>EnableRetryOnFailure()</c> registration rejects
/// Eager's explicit transaction).
/// </summary>
[WolverineHandler]
[Transactional(typeof(TutorsDbContext), Mode = TransactionMiddlewareMode.Lightweight)]
public sealed class SuspendApplicationConsumer(SuspendApplication.Handler handler)
{
    public Task<Result> HandleAsync(SuspendApplication.Command command, CancellationToken ct) =>
        handler.HandleAsync(command.ApplicationId, command.Reason, command.AdminUserId, ct);
}
