using Microsoft.EntityFrameworkCore;
using Npgsql;
using TutorFlow.Modules.Tutors.Domain;
using TutorFlow.Modules.Tutors.Persistence;

namespace TutorFlow.Modules.Tutors.Features.SubmitApplication;

public static class SubmitApplication
{
    public sealed record Response(Guid Id, TutorApplicationStatus Status, DateTimeOffset? SubmittedAtUtc, string? PublicSlug);

    internal sealed class Handler(TutorsDbContext db, TimeProvider clock)
    {
        public async Task<Result<Response>> HandleAsync(Guid userId, CancellationToken ct)
        {
            var application = await db.TutorApplications.FirstOrDefaultAsync(a => a.UserId == userId, ct);
            if (application is null)
            {
                return Result.Failure<Response>("No tutor application found for this user.");
            }

            // Submit() also assigns PublicSlug as a pure domain operation (ADR-0022) — no
            // SELECT-then-INSERT uniqueness *check* is needed before this call, since the
            // Id-derived suffix makes a collision vanishingly rare by construction. "Vanishingly
            // rare" is not "impossible", though (the suffix is only the first 8 hex chars of the
            // Id, i.e. 32 bits — a true collision still needs a matching slugified DisplayName
            // too, but isn't mathematically ruled out at scale): the unique index on PublicSlug
            // is the real, final guard, and this catch turns that guard into a clean failure
            // result instead of an unhandled 500, matching CreateApplication.Handler's identical
            // pattern for UserId.
            var result = application.Submit(clock);
            if (result.IsFailure)
            {
                return Result.Failure<Response>([.. result.Errors]);
            }

            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                return Result.Failure<Response>("Could not assign a unique profile URL; please try submitting again.");
            }

            return Result.Success(new Response(application.Id, application.Status, application.SubmittedAtUtc, application.PublicSlug));
        }
    }
}
