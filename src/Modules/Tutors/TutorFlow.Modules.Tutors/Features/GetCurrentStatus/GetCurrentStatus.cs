using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Tutors.Domain;
using TutorFlow.Modules.Tutors.Persistence;

namespace TutorFlow.Modules.Tutors.Features.GetCurrentStatus;

public static class GetCurrentStatus
{
    public sealed record Response(Guid UserId, TutorApplicationStatus Status, string? DisplayName);

    internal sealed class Handler(TutorsDbContext db)
    {
        public async Task<Result<Response>> HandleAsync(Guid userId, CancellationToken ct)
        {
            var application = await db.TutorApplications.AsNoTracking().FirstOrDefaultAsync(a => a.UserId == userId, ct);
            if (application is null)
            {
                return Result.Failure<Response>("No tutor application found for this user.");
            }

            return Result.Success(new Response(application.UserId, application.Status, application.DisplayName));
        }
    }
}
