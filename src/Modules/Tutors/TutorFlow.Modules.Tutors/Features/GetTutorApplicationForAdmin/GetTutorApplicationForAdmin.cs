using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Tutors.Domain;
using TutorFlow.Modules.Tutors.Persistence;
using GetCurrentApplicationFeature = TutorFlow.Modules.Tutors.Features.GetCurrentApplication.GetCurrentApplication;

namespace TutorFlow.Modules.Tutors.Features.GetTutorApplicationForAdmin;

/// <summary>
/// Admin detail view — the by-<c>Id</c> counterpart to <c>GetCurrentApplication</c>'s
/// by-caller's-own-<c>UserId</c> lookup. Reuses the exact same response shape (full draft/profile
/// data, including internal-only fields like <c>RejectionReason</c> that the public
/// <c>GetPublicProfile</c> deliberately omits) rather than inventing a second DTO for the same
/// data.
/// </summary>
public static class GetTutorApplicationForAdmin
{
    internal sealed class Handler(TutorsDbContext db)
    {
        public async Task<Result<GetCurrentApplicationFeature.Response>> HandleAsync(Guid applicationId, CancellationToken ct)
        {
            var application = await db.TutorApplications.AsNoTracking().FirstOrDefaultAsync(a => a.Id == applicationId, ct);
            if (application is null)
            {
                return Result.Failure<GetCurrentApplicationFeature.Response>("Tutor application not found.");
            }

            return Result.Success(GetCurrentApplicationFeature.Map(application));
        }
    }
}
