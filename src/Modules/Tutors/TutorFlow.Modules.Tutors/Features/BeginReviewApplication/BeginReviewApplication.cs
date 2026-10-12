using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Tutors.Domain;
using TutorFlow.Modules.Tutors.Persistence;
using GetCurrentApplicationFeature = TutorFlow.Modules.Tutors.Features.GetCurrentApplication.GetCurrentApplication;

namespace TutorFlow.Modules.Tutors.Features.BeginReviewApplication;

/// <summary>
/// Admin action: Submitted -&gt; UnderReview. Lets an admin signal they've started working a
/// queue entry without yet deciding Approve/Reject — purely a coordination marker (the domain
/// guard on <c>Approve</c>/<c>Reject</c> already accepts either Submitted or UnderReview, so this
/// step is optional, not a required gate).
/// </summary>
public static class BeginReviewApplication
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

            var result = application.BeginReview(clock);
            if (result.IsFailure)
            {
                return Result.Failure<GetCurrentApplicationFeature.Response>([.. result.Errors]);
            }

            await db.SaveChangesAsync(ct);

            return Result.Success(GetCurrentApplicationFeature.Map(application));
        }
    }
}
