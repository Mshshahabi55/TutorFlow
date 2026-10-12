using Microsoft.EntityFrameworkCore;
using Npgsql;
using TutorFlow.Modules.Students.Domain;
using TutorFlow.Modules.Students.Persistence;

namespace TutorFlow.Modules.Students.Features.AddFavorite;

public static class AddFavorite
{
    public sealed record Response(bool IsFavorited);

    internal sealed class Handler(StudentsDbContext db, TimeProvider clock)
    {
        public async Task<Result<Response>> HandleAsync(Guid studentUserId, Guid tutorUserId, CancellationToken ct)
        {
            var existing = await db.FavoriteTutors
                .AsNoTracking()
                .AnyAsync(f => f.StudentUserId == studentUserId && f.TutorUserId == tutorUserId, ct);

            if (existing)
            {
                // Idempotent — matches a PUT-style "ensure favorited" semantic rather than
                // erroring on a second favorite of the same tutor.
                return Result.Success(new Response(true));
            }

            db.FavoriteTutors.Add(FavoriteTutor.Create(studentUserId, tutorUserId, clock));

            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                // Two concurrent favorite requests for the same (student, tutor) pair can both
                // pass the check above; the database's unique index is the real guard.
            }

            return Result.Success(new Response(true));
        }
    }
}
