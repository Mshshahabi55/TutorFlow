using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Lessons.Domain;
using TutorFlow.Modules.Lessons.Persistence;
using TutorFlow.Shared;
using CompleteDueLessonsFeature = TutorFlow.Modules.Lessons.Features.CompleteDueLessons.CompleteDueLessons;

namespace TutorFlow.Modules.Lessons.Infrastructure;

// ADR-0014 pattern — the Reviews module's eligibility check depends on this via DI, no project
// reference between Lessons and Reviews.
internal sealed class CompletedLessonCountQuery(LessonsDbContext db, CompleteDueLessonsFeature.Handler completeDueLessons) : ICompletedLessonCountQuery
{
    public async Task<int> CountCompletedLessonsAsync(Guid studentUserId, Guid tutorUserId, CancellationToken ct)
    {
        // Same "sweep before an accurate read is needed" convention as GetMyLessons — a lesson
        // whose completion threshold has passed but was never read by the student yet must still
        // count here, not only after some unrelated GetMyLessons call happened to sweep it first.
        await completeDueLessons.HandleAsync(studentUserId, ct);

        return await db.Lessons
            .AsNoTracking()
            .CountAsync(l => l.StudentUserId == studentUserId && l.TutorUserId == tutorUserId && l.Status == LessonStatus.Completed, ct);
    }
}
