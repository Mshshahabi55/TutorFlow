using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Tutors.Domain;
using TutorFlow.Modules.Tutors.Persistence;
using TutorFlow.Shared;
using Wolverine;
using GetCurrentApplicationFeature = TutorFlow.Modules.Tutors.Features.GetCurrentApplication.GetCurrentApplication;

namespace TutorFlow.Modules.Tutors.Features.PublishApplication;

/// <summary>
/// Publishes <c>TutorPublished</c> (ADR-0021) whenever <c>TutorApplication.Publish()</c>
/// succeeds — the handler-level half of the first real Wolverine pipeline in this codebase.
///
/// Now mapped to a real admin HTTP endpoint (ADR-0028, the Admin role/policy `OPEN_QUESTIONS.md`
/// A35 was blocking on now exists). <see cref="Handler"/>'s signature is unchanged so the
/// existing <c>TutorPublishedPipelineTests</c> call sites (which invoke it directly) keep
/// working unmodified.
/// </summary>
public static class PublishApplication
{
    internal sealed class Handler(TutorsDbContext db, IMessageBus bus, TimeProvider clock)
    {
        public async Task<Result<GetCurrentApplicationFeature.Response>> HandleAsync(Guid applicationId, CancellationToken ct)
        {
            var application = await db.TutorApplications.FirstOrDefaultAsync(a => a.Id == applicationId, ct);
            if (application is null)
            {
                return Result.Failure<GetCurrentApplicationFeature.Response>("Tutor application not found.");
            }

            var result = application.Publish(clock);
            if (result.IsFailure)
            {
                return Result.Failure<GetCurrentApplicationFeature.Response>([.. result.Errors]);
            }

            await db.SaveChangesAsync(ct);

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
