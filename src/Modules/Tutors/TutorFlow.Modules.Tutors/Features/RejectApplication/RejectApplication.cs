using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Tutors.Domain;
using TutorFlow.Modules.Tutors.Persistence;
using GetCurrentApplicationFeature = TutorFlow.Modules.Tutors.Features.GetCurrentApplication.GetCurrentApplication;

namespace TutorFlow.Modules.Tutors.Features.RejectApplication;

/// <summary>
/// Admin action: Submitted/UnderReview -&gt; Rejected. A reason is required (DOCUMENTED rejection
/// categories exist — profile quality, under-18, low subject demand, sanctioned-country
/// residency, `04-platform-trust-and-safety.md`) and persisted so the already-built tutor-facing
/// <c>ApplicationStatusPanel</c> can display it; the applicant can then self-service
/// <c>ReopenApplicationForRevision</c> (already implemented) to correct and resubmit.
/// </summary>
public static class RejectApplication
{
    public sealed record Command(string Reason);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Reason).NotEmpty().WithMessage("A rejection reason is required.").MaximumLength(2000);
        }
    }

    internal sealed class Handler(TutorsDbContext db, TimeProvider clock)
    {
        public async Task<Result<GetCurrentApplicationFeature.Response>> HandleAsync(Guid applicationId, string reason, CancellationToken ct)
        {
            var application = await db.TutorApplications.FirstOrDefaultAsync(a => a.Id == applicationId, ct);
            if (application is null)
            {
                return Result.Failure<GetCurrentApplicationFeature.Response>("Tutor application not found.");
            }

            var result = application.Reject(reason, clock);
            if (result.IsFailure)
            {
                return Result.Failure<GetCurrentApplicationFeature.Response>([.. result.Errors]);
            }

            await db.SaveChangesAsync(ct);

            return Result.Success(GetCurrentApplicationFeature.Map(application));
        }
    }
}
