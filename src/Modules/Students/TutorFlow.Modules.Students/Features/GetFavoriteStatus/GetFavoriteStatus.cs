using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Students.Persistence;

namespace TutorFlow.Modules.Students.Features.GetFavoriteStatus;

public static class GetFavoriteStatus
{
    public sealed record Response(bool IsFavorited);

    internal sealed class Handler(StudentsDbContext db)
    {
        public async Task<Response> HandleAsync(Guid studentUserId, Guid tutorUserId, CancellationToken ct)
        {
            var isFavorited = await db.FavoriteTutors
                .AsNoTracking()
                .AnyAsync(f => f.StudentUserId == studentUserId && f.TutorUserId == tutorUserId, ct);

            return new Response(isFavorited);
        }
    }
}
