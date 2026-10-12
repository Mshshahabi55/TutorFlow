using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TutorFlow.Modules.Reviews.Domain;
using TutorFlow.Modules.Reviews.Persistence;
using TutorFlow.Shared;
using GetReviewEligibilityFeature = TutorFlow.Modules.Reviews.Features.GetReviewEligibility.GetReviewEligibility;

namespace TutorFlow.Modules.Reviews.Features.SubmitReview;

// Creates the student's review of a tutor on first submission; a later call for the same
// (StudentUserId, TutorUserId) pair edits that same row rather than creating a second one —
// documented Preply behavior, not a per-Lesson review.
public static class SubmitReview
{
    public sealed record Command(int Rating, string? Comment);

    public sealed record Response(Guid Id, int Rating, string? Comment, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc);

    public enum Outcome
    {
        Created,
        Updated,
        NotEligible
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Rating).InclusiveBetween(1, 5);
            RuleFor(x => x.Comment).MaximumLength(2000);
        }
    }

    internal sealed class Handler(ReviewsDbContext db, TimeProvider clock, ICompletedLessonCountQuery completedLessonCountQuery)
    {
        public async Task<(Outcome Outcome, Response? Response)> HandleAsync(Guid studentUserId, Guid tutorUserId, Command command, CancellationToken ct)
        {
            // Server-side re-check — the GetReviewEligibility GET endpoint only ever advises the
            // frontend's own create/edit/locked UI; this is the actual enforcement gate, same
            // discipline as CreateTrialHold's own trial-eligibility re-check.
            var completedCount = await completedLessonCountQuery.CountCompletedLessonsAsync(studentUserId, tutorUserId, ct);
            if (completedCount < GetReviewEligibilityFeature.RequiredCompletedLessonCount)
            {
                return (Outcome.NotEligible, null);
            }

            var now = clock.GetUtcNow();

            // Try the update path first (same conditional-UPDATE convention as CompleteDueLessons/
            // CancelBooking) — if a row already exists for this relationship, this is an edit.
            var updated = await db.Reviews
                .Where(r => r.StudentUserId == studentUserId && r.TutorUserId == tutorUserId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.Rating, command.Rating)
                    .SetProperty(r => r.Comment, command.Comment)
                    .SetProperty(r => r.UpdatedAtUtc, now), ct);

            if (updated == 1)
            {
                return (Outcome.Updated, await ReadAsync(studentUserId, tutorUserId, ct));
            }

            // No existing row — attempt to create one. The DB's unique index on
            // (StudentUserId, TutorUserId) is the real concurrency guard: if a concurrent request
            // won the insert race between the UPDATE above (which matched 0 rows) and this Add,
            // the violation is caught and this request's own data is applied via the same
            // conditional UPDATE instead — every concurrent submitter's intent is still applied,
            // to exactly one surviving row, never two.
            db.Reviews.Add(Review.Create(studentUserId, tutorUserId, command.Rating, command.Comment, clock));

            try
            {
                await db.SaveChangesAsync(ct);
                return (Outcome.Created, await ReadAsync(studentUserId, tutorUserId, ct));
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                await db.Reviews
                    .Where(r => r.StudentUserId == studentUserId && r.TutorUserId == tutorUserId)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(r => r.Rating, command.Rating)
                        .SetProperty(r => r.Comment, command.Comment)
                        .SetProperty(r => r.UpdatedAtUtc, now), ct);

                return (Outcome.Updated, await ReadAsync(studentUserId, tutorUserId, ct));
            }
        }

        private async Task<Response> ReadAsync(Guid studentUserId, Guid tutorUserId, CancellationToken ct)
        {
            return await db.Reviews
                .AsNoTracking()
                .Where(r => r.StudentUserId == studentUserId && r.TutorUserId == tutorUserId)
                .Select(r => new Response(r.Id, r.Rating, r.Comment, r.CreatedAtUtc, r.UpdatedAtUtc))
                .SingleAsync(ct);
        }
    }
}
