using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Reviews.Domain;
using TutorFlow.Modules.Reviews.Persistence;
using TutorFlow.Shared;

namespace TutorFlow.Modules.Reviews.Features.GetSubcategoryRatingEligibility;

// DOCUMENTED (help.preply.com/articles/11196386): "once per month, after a completed lesson" —
// a deliberately looser, separate eligibility gate from the written Review's 3-lesson threshold.
public static class GetSubcategoryRatingEligibility
{
    public const int RequiredCompletedLessonCount = 1;

    public sealed record Response(bool IsEligible, bool HasCompletedAtLeastOneLesson, bool AlreadySubmittedThisMonth);

    internal sealed class Handler(ReviewsDbContext db, TimeProvider clock, ICompletedLessonCountQuery completedLessonCountQuery)
    {
        public async Task<Response> HandleAsync(Guid studentUserId, Guid tutorUserId, CancellationToken ct)
        {
            var completedCount = await completedLessonCountQuery.CountCompletedLessonsAsync(studentUserId, tutorUserId, ct);
            var hasCompletedAtLeastOne = completedCount >= RequiredCompletedLessonCount;

            var monthKey = SubcategoryRatingMonthKey.For(clock.GetUtcNow());
            var alreadySubmitted = await db.SubcategoryRatings
                .AsNoTracking()
                .AnyAsync(r => r.StudentUserId == studentUserId && r.TutorUserId == tutorUserId && r.SubmissionMonthKey == monthKey, ct);

            return new Response(hasCompletedAtLeastOne && !alreadySubmitted, hasCompletedAtLeastOne, alreadySubmitted);
        }
    }
}
