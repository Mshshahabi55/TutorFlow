using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Lessons.Domain;
using TutorFlow.Modules.Lessons.Persistence;
using CompleteDueLessonsFeature = TutorFlow.Modules.Lessons.Features.CompleteDueLessons.CompleteDueLessons;

namespace TutorFlow.Modules.Lessons.Features.GetMyLessons;

public static class GetMyLessons
{
    public sealed record Summary(Guid Id, Guid BookingId, Guid TutorUserId, LessonStatus Status, DateTimeOffset StartUtc, DateTimeOffset EndUtc, int DurationMinutes);

    public sealed record Response(IReadOnlyList<Summary> Lessons);

    internal sealed class Handler(LessonsDbContext db, CompleteDueLessonsFeature.Handler completeDueLessons)
    {
        // Same ordering convention as Bookings' own GetMyBookingsAsStudent (most-recent/future
        // first) — reused for consistency rather than inventing a separate upcoming/past split.
        public async Task<Response> HandleAsync(Guid studentUserId, CancellationToken ct)
        {
            // Lazy sweep before reading, same "triggered by the read that needs an accurate
            // status" precedent as Bookings' own hold-expiry check — a student's lessons are
            // never stale by more than the time since their last GetMyLessons call.
            await completeDueLessons.HandleAsync(studentUserId, ct);

            var lessons = await db.Lessons
                .AsNoTracking()
                .Where(l => l.StudentUserId == studentUserId)
                .OrderByDescending(l => l.StartUtc)
                .Select(l => new Summary(l.Id, l.BookingId, l.TutorUserId, l.Status, l.StartUtc, l.EndUtc, (int)(l.EndUtc - l.StartUtc).TotalMinutes))
                .ToListAsync(ct);

            return new Response(lessons);
        }
    }
}
