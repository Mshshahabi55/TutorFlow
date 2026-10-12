using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Tutors.Domain;
using TutorFlow.Modules.Tutors.Persistence;
using GetCurrentApplicationFeature = TutorFlow.Modules.Tutors.Features.GetCurrentApplication.GetCurrentApplication;

namespace TutorFlow.Modules.Tutors.Features.ApproveApplication;

/// <summary>
/// Admin action: Submitted/UnderReview -&gt; Approved. Deliberately does <b>not</b> auto-publish
/// (see ADR-0028) — Approved and Published are kept as the two distinct, separately-admin-gated
/// steps the domain already models (<c>TutorApplication.Approve</c> vs. <c>.Publish</c>), matching
/// both the pre-existing test precedent (<c>TutorPublishedPipelineTests</c>,
/// <c>TutorsEndpointsTests.CreatePublishedTutorAsync</c>, both of which already call Approve then
/// Publish as two separate steps) and the already-shipped tutor-facing copy
/// (<c>ApplicationStatusPanel</c>'s Approved case: "Your profile will go live shortly" — honest
/// about a still-pending separate step, not a mistake to fix). A second admin action
/// (<c>PublishApplication</c>, now mapped to its own endpoint) makes the tutor live.
/// </summary>
public static class ApproveApplication
{
    internal sealed class Handler(TutorsDbContext db, TimeProvider clock)
    {
        public async Task<Result<GetCurrentApplicationFeature.Response>> HandleAsync(Guid applicationId, CancellationToken ct)
        {
            var application = await db.TutorApplications.FirstOrDefaultAsync(a => a.Id == applicationId, ct);
            if (application is null)
            {
                return Result.Failure<GetCurrentApplicationFeature.Response>("Tutor application not found.");
            }

            var result = application.Approve(clock);
            if (result.IsFailure)
            {
                return Result.Failure<GetCurrentApplicationFeature.Response>([.. result.Errors]);
            }

            await db.SaveChangesAsync(ct);

            return Result.Success(GetCurrentApplicationFeature.Map(application));
        }
    }
}
