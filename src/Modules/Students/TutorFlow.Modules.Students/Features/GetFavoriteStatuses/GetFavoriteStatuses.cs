using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Students.Persistence;

namespace TutorFlow.Modules.Students.Features.GetFavoriteStatuses;

/// <summary>
/// Bulk counterpart to <c>GetFavoriteStatus</c> (ADR-0024) — lets a page of N search results
/// show real favorite state with one request instead of N. Add/remove and the single-tutor
/// status check are reused unchanged; this is the only new Favorites capability.
/// </summary>
public static class GetFavoriteStatuses
{
    public sealed record Response(IReadOnlyList<Guid> FavoritedTutorUserIds);

    internal sealed class Handler(StudentsDbContext db)
    {
        public async Task<Response> HandleAsync(Guid studentUserId, IReadOnlyList<Guid> tutorUserIds, CancellationToken ct)
        {
            if (tutorUserIds.Count == 0)
            {
                return new Response([]);
            }

            var favorited = await db.FavoriteTutors
                .AsNoTracking()
                .Where(f => f.StudentUserId == studentUserId && tutorUserIds.Contains(f.TutorUserId))
                .Select(f => f.TutorUserId)
                .ToListAsync(ct);

            return new Response(favorited);
        }
    }
}
