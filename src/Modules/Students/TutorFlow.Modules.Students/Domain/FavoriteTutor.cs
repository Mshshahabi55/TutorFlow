namespace TutorFlow.Modules.Students.Domain;

// Students owns the "favorite" relationship (ownership already settled by precedent in
// PREPLY_SEARCH_PARITY_MATRIX.md row 28 — "Students (FavoriteTutor, stub)" — this is that
// capability's first real implementation, ADR-0022). A student's saved-tutor list, not a
// Tutors-owned concept — matches Preply's own framing ("Save to my list" is the student's
// action, not the tutor's state).
public sealed class FavoriteTutor
{
    private FavoriteTutor()
    {
    }

    public Guid Id { get; private init; }

    public Guid StudentUserId { get; private init; }

    public Guid TutorUserId { get; private init; }

    public DateTimeOffset CreatedAtUtc { get; private init; }

    public static FavoriteTutor Create(Guid studentUserId, Guid tutorUserId, TimeProvider clock) => new()
    {
        Id = Guid.NewGuid(),
        StudentUserId = studentUserId,
        TutorUserId = tutorUserId,
        CreatedAtUtc = clock.GetUtcNow()
    };
}
