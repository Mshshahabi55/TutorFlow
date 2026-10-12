using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TutorFlow.Modules.Reviews.Domain;
using TutorFlow.Modules.Reviews.Persistence;
using TutorFlow.Shared;
using GetSubcategoryRatingEligibilityFeature = TutorFlow.Modules.Reviews.Features.GetSubcategoryRatingEligibility.GetSubcategoryRatingEligibility;

namespace TutorFlow.Modules.Reviews.Features.SubmitSubcategoryRating;

// DOCUMENTED (help.preply.com/articles/11196386): a separate, immutable, at-most-once-per-
// calendar-month submission — never an upsert, never editable by the student afterward.
public static class SubmitSubcategoryRating
{
    public sealed record Command(int? Clarity, int? Progress, int? Preparation, int? Reassurance);

    public sealed record Response(Guid Id, int? Clarity, int? Progress, int? Preparation, int? Reassurance, DateTimeOffset CreatedAtUtc);

    public enum Outcome
    {
        Created,
        NotEligible,
        AlreadySubmittedThisMonth
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            // Each dimension is independently optional — DOCUMENTED ("rate all, some, or none").
            RuleFor(x => x.Clarity).InclusiveBetween(1, 5).When(x => x.Clarity.HasValue);
            RuleFor(x => x.Progress).InclusiveBetween(1, 5).When(x => x.Progress.HasValue);
            RuleFor(x => x.Preparation).InclusiveBetween(1, 5).When(x => x.Preparation.HasValue);
            RuleFor(x => x.Reassurance).InclusiveBetween(1, 5).When(x => x.Reassurance.HasValue);
        }
    }

    internal sealed class Handler(ReviewsDbContext db, TimeProvider clock, ICompletedLessonCountQuery completedLessonCountQuery)
    {
        public async Task<(Outcome Outcome, Response? Response)> HandleAsync(Guid studentUserId, Guid tutorUserId, Command command, CancellationToken ct)
        {
            // Server-side re-check — never trusts the separate eligibility GET.
            var completedCount = await completedLessonCountQuery.CountCompletedLessonsAsync(studentUserId, tutorUserId, ct);
            if (completedCount < GetSubcategoryRatingEligibilityFeature.RequiredCompletedLessonCount)
            {
                return (Outcome.NotEligible, null);
            }

            var rating = SubcategoryRating.Create(studentUserId, tutorUserId, command.Clarity, command.Progress, command.Preparation, command.Reassurance, clock);
            db.SubcategoryRatings.Add(rating);

            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                // The DB's unique index on (StudentUserId, TutorUserId, SubmissionMonthKey) is the
                // real concurrency guard — two near-simultaneous submissions in the same month
                // never both persist, regardless of what the eligibility GET said a moment ago.
                return (Outcome.AlreadySubmittedThisMonth, null);
            }

            return (Outcome.Created, Map(rating));
        }

        private static Response Map(SubcategoryRating r) => new(r.Id, r.Clarity, r.Progress, r.Preparation, r.Reassurance, r.CreatedAtUtc);
    }
}
