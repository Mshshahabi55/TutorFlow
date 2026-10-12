using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Lessons.Domain;
using TutorFlow.Modules.Lessons.Persistence;

namespace TutorFlow.Modules.Lessons.Features.CompleteDueLessons;

// V1 lesson-completion rule (Product Owner decision, approved): a Lesson becomes Completed at
// EndUtc + 15 minutes, regardless of Classroom — TutorFlow has no Classroom yet.
//
// INTENTIONAL DEVIATION from Preply's own documented behavior (~15min inside Classroom / a
// longer fallback window otherwise, see docs/product/preply-reverse-engineering/08-state-
// machines.md) — this flat, Classroom-independent rule applies to every Lesson until Classroom
// exists, at which point this can be revisited against the more accurate Preply behavior. Not
// invented by this implementation; explicitly approved before this slice was written.
public static class CompleteDueLessons
{
    public static readonly TimeSpan CompletionGrace = TimeSpan.FromMinutes(15);

    public sealed class Handler(LessonsDbContext db, TimeProvider clock)
    {
        // Lazy sweep, same conditional-UPDATE shape as Bookings' CheckoutBooking/ConfirmBooking
        // (ADR-0013 §3 precedent) — scoped to the lessons the triggering read is about to
        // return, same as Bookings' own sweep is scoped to the one booking being acted on, not a
        // table-wide background sweep. Concurrency-safe by construction: the WHERE clause only
        // ever matches rows still Scheduled-and-due at execution time, so two concurrent callers
        // racing the same row affect it at most once between them — neither can "double
        // complete" a row, and a Cancelled/NoShow/already-Completed row can never match at all.
        public async Task<int> HandleAsync(Guid studentUserId, CancellationToken ct)
        {
            var now = clock.GetUtcNow();

            return await db.Lessons
                .Where(l => l.StudentUserId == studentUserId
                    && l.Status == LessonStatus.Scheduled
                    && l.EndUtc + CompletionGrace <= now)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(l => l.Status, LessonStatus.Completed)
                    .SetProperty(l => l.UpdatedAtUtc, now), ct);
        }
    }
}
