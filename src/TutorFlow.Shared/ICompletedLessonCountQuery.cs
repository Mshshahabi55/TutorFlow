namespace TutorFlow.Shared;

// ADR-0014 pattern: a narrow, read-only cross-module query contract — implemented by the owning
// module (Lessons), consumed via DI by any module that needs the answer (Reviews), never via a
// direct project reference. Lessons' own lazy-completion sweep (EndUtc + 15min) runs as part of
// this query's implementation, so the count is always accurate at the moment it's asked, not
// only after some other unrelated read happened to trigger the sweep first.
public interface ICompletedLessonCountQuery
{
    Task<int> CountCompletedLessonsAsync(Guid studentUserId, Guid tutorUserId, CancellationToken ct);
}
