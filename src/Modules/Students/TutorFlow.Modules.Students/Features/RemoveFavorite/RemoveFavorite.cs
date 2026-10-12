using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Students.Persistence;

namespace TutorFlow.Modules.Students.Features.RemoveFavorite;

public static class RemoveFavorite
{
    internal sealed class Handler(StudentsDbContext db)
    {
        public async Task HandleAsync(Guid studentUserId, Guid tutorUserId, CancellationToken ct)
        {
            // Idempotent — matches a DELETE-style "ensure not favorited" semantic; removing an
            // already-absent favorite is a no-op, not an error.
            await db.FavoriteTutors
                .Where(f => f.StudentUserId == studentUserId && f.TutorUserId == tutorUserId)
                .ExecuteDeleteAsync(ct);
        }
    }
}
