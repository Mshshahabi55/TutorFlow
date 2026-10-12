using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Reviews.Persistence;
using TutorFlow.Shared;

namespace TutorFlow.Modules.Reviews.Features.GetReviewEligibility;

// Documented Preply behavior (product benchmark, not an intentional deviation): a student
// becomes eligible to review a tutor after their 3rd completed lesson with that specific tutor —
// this is a relationship-level gate, never a per-Lesson one.
public static class GetReviewEligibility
{
    public const int RequiredCompletedLessonCount = 3;

    public sealed record ExistingReview(int Rating, string? Comment, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc);

    public sealed record Response(bool IsEligible, int CompletedLessonCount, int RequiredCompletedLessonCount, ExistingReview? ExistingReview);

    internal sealed class Handler(ReviewsDbContext db, ICompletedLessonCountQuery completedLessonCountQuery)
    {
        public async Task<Response> HandleAsync(Guid studentUserId, Guid tutorUserId, CancellationToken ct)
        {
            var completedCount = await completedLessonCountQuery.CountCompletedLessonsAsync(studentUserId, tutorUserId, ct);

            var existing = await db.Reviews
                .AsNoTracking()
                .Where(r => r.StudentUserId == studentUserId && r.TutorUserId == tutorUserId)
                .Select(r => new ExistingReview(r.Rating, r.Comment, r.CreatedAtUtc, r.UpdatedAtUtc))
                .FirstOrDefaultAsync(ct);

            return new Response(completedCount >= RequiredCompletedLessonCount, completedCount, RequiredCompletedLessonCount, existing);
        }
    }
}
