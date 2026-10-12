using Microsoft.EntityFrameworkCore;
using Npgsql;
using TutorFlow.Modules.Tutors.Domain;
using TutorFlow.Modules.Tutors.Persistence;

namespace TutorFlow.Modules.Tutors.Features.CreateApplication;

public static class CreateApplication
{
    public sealed record Response(Guid Id, Guid UserId, TutorApplicationStatus Status, DateTimeOffset CreatedAtUtc);

    internal sealed class Handler(TutorsDbContext db, TimeProvider clock)
    {
        public async Task<Result<Response>> HandleAsync(Guid userId, CancellationToken ct)
        {
            var existing = await db.TutorApplications.AsNoTracking().FirstOrDefaultAsync(a => a.UserId == userId, ct);
            if (existing is not null)
            {
                return Result.Failure<Response>("A tutor application already exists for this user.");
            }

            var application = TutorApplication.CreateDraft(userId, clock);
            db.TutorApplications.Add(application);

            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                // Two concurrent create requests for the same user can both pass the check above;
                // the database's unique index on UserId is the real guard against that race.
                return Result.Failure<Response>("A tutor application already exists for this user.");
            }

            return Result.Success(new Response(application.Id, application.UserId, application.Status, application.CreatedAtUtc));
        }
    }
}
