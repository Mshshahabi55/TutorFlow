using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Tutors.Domain;
using TutorFlow.Modules.Tutors.Persistence;
using TutorFlow.Shared;
using Wolverine;
using Wolverine.Attributes;
using Wolverine.Persistence;
using GetCurrentApplicationFeature = TutorFlow.Modules.Tutors.Features.GetCurrentApplication.GetCurrentApplication;

namespace TutorFlow.Modules.Tutors.Features.PublishApplication;

/// <summary>
/// Publishes <c>TutorPublished</c> (ADR-0021) whenever <c>TutorApplication.Publish()</c>
/// succeeds — the handler-level half of the first real Wolverine pipeline in this codebase.
///
/// Now mapped to a real admin HTTP endpoint (ADR-0028, the Admin role/policy `OPEN_QUESTIONS.md`
/// A35 was blocking on now exists).
///
/// A44 #103: the HTTP endpoint no longer calls <see cref="Handler"/> directly — it dispatches
/// <see cref="Command"/> via <c>IMessageBus.InvokeAsync</c> so this handler's body runs inside a
/// real Wolverine chain, which <see cref="PublishApplicationConsumer"/> below is this feature's
/// half of. Correspondingly, <see cref="Handler"/> no longer calls <c>SaveChangesAsync</c>
/// itself — the application entity change is left tracked, and <c>PublishApplicationConsumer</c>'s
/// <c>[Transactional(Lightweight)]</c> middleware's own single <c>SaveChangesAsync</c> call
/// (added automatically after the handler returns) flushes it together with the outgoing
/// <c>TutorPublished</c> envelope the outbox-enlisted <c>bus.PublishAsync</c> below buffers onto
/// the same <see cref="TutorsDbContext"/> instance — one call, one transaction, both commit or
/// neither does. <see cref="Handler"/> must only ever be reached through that chain now (every
/// caller, including tests, dispatches <see cref="Command"/> via Wolverine) — calling it directly
/// without a surrounding SaveChangesAsync would silently drop the status change.
/// </summary>
public static class PublishApplication
{
    public sealed record Command(Guid ApplicationId);

    public sealed class Handler(TutorsDbContext db, IMessageBus bus, TimeProvider clock)
    {
        public async Task<Result<GetCurrentApplicationFeature.Response>> HandleAsync(Guid applicationId, CancellationToken ct)
        {
            var application = await db.TutorApplications.FirstOrDefaultAsync(a => a.Id == applicationId, ct);
            if (application is null)
            {
                return Result.Failure<GetCurrentApplicationFeature.Response>("Tutor application not found.");
            }

            // A44 #103 idempotent-recovery branch, mirroring ConfirmBooking.Handler's
            // AlreadyConfirmed precedent exactly: if this command is retried after an earlier
            // attempt's business write already committed — possible if the caller never saw a
            // response (e.g. the connection dropped right as Wolverine's single SaveChangesAsync
            // committed) — Publish() would otherwise fail with "already Published" and the event
            // would never be (re)sent. Republishing from the already-persisted state recovers
            // that case instead of returning a confusing failure for an action that, in fact,
            // already succeeded.
            if (application.Status != TutorApplicationStatus.Published)
            {
                var result = application.Publish(clock);
                if (result.IsFailure)
                {
                    return Result.Failure<GetCurrentApplicationFeature.Response>([.. result.Errors]);
                }
            }

            var @event = new TutorPublished(
                application.UserId,
                // Guaranteed non-null by the Draft -> Submitted -> ... -> Published state
                // machine: Submit() always assigns PublicSlug on first success (ADR-0022), and
                // Published can only be reached by having passed through Submitted.
                application.PublicSlug!,
                application.DisplayName ?? "",
                application.Headline ?? "",
                application.ProfilePhotoReference,
                application.SubjectIds,
                application.SpecialtyIds,
                [.. application.SpokenLanguages.Select(l => new TutorPublishedSpokenLanguage(l.LanguageCode, l.Proficiency.ToString()))],
                application.CountryOfBirthCode,
                application.IsProfessionalTutor,
                application.IsSuperTutor,
                application.HourlyRateAmountMinorUnits ?? 0,
                application.HourlyRateCurrency ?? "EUR",
                application.PublishedAtUtc ?? clock.GetUtcNow(),
                clock.GetUtcNow(),
                application.IsAcceptingNewStudents);

            await bus.PublishAsync(@event);

            return Result.Success(GetCurrentApplicationFeature.Map(application));
        }
    }
}

/// <summary>
/// A44 #103: local-command dispatch target, not a cross-module event consumer — named with the
/// "Consumer" suffix only because Program.cs's Wolverine discovery config excludes every
/// "...Handler"-suffixed type (F6; every feature's own business class is named <c>Handler</c>)
/// and "Consumer" is the one other convention already proven to work in this codebase. Exists
/// solely so <see cref="PublishApplication.Handler"/>'s body runs inside a real Wolverine chain:
/// <c>AutoApplyTransactions()</c> (Program.cs) wraps it in one ambient <see cref="TutorsDbContext"/>
/// transaction, so the <c>SaveChangesAsync</c> that marks the application Published and the
/// outgoing <c>TutorPublished</c> envelope (persisted into TutorsDb's own Ancillary outbox store
/// per Program.cs's <c>PersistMessagesWithPostgresql</c> call) commit or roll back together —
/// closing the crash window that existed when the HTTP endpoint called <see cref="PublishApplication.Handler"/>
/// directly.
///
/// Lightweight, not Eager, and verified empirically (via `codegen write`, not assumed): Eager
/// mode emits an explicit <c>BeginTransactionAsync()</c>, which <c>TutorsDbContext</c>'s
/// <c>EnableRetryOnFailure()</c> registration (TutorsModule.cs) categorically refuses — EF
/// Core's retrying execution strategy cannot coexist with a caller-managed transaction. This is
/// not the weaker "different physical connection, needs idempotent recovery" Lightweight case
/// documented on <c>PaymentSucceededConsumer</c>: for a local (non-HTTP-mapped) Wolverine chain
/// against a DbContext that already is its event's own Ancillary store,
/// <c>EFCorePersistenceFrameProvider</c> enlists the DbContext directly in the outbox and flushes
/// via one ordinary <c>SaveChangesAsync</c> call (decompiled and confirmed) — the business write
/// and the envelope insert still land in the same transaction, just via EF's own implicit
/// per-SaveChanges transaction (which the retry strategy supports) instead of an explicit one.
/// </summary>
[WolverineHandler]
[Transactional(typeof(TutorsDbContext), Mode = TransactionMiddlewareMode.Lightweight)]
public sealed class PublishApplicationConsumer(PublishApplication.Handler handler)
{
    public Task<Result<GetCurrentApplicationFeature.Response>> HandleAsync(PublishApplication.Command command, CancellationToken ct) =>
        handler.HandleAsync(command.ApplicationId, ct);
}
