namespace TutorFlow.Modules.Reviews.Domain;

// Product Owner decision (documented Preply behavior, not an intentional deviation): a review is
// a Student <-> Tutor relationship, not a per-Lesson artifact — eligibility is derived from the
// student's completed-lesson count with that tutor (>= 3, see SubmitReview), never stored as a
// foreign key here. One row per (StudentUserId, TutorUserId); submitting again edits this same
// row rather than creating another (DB-enforced, see ReviewConfiguration's unique index).
public sealed class Review
{
    private Review()
    {
    }

    public Guid Id { get; private init; }

    public Guid StudentUserId { get; private init; }

    public Guid TutorUserId { get; private init; }

    public int Rating { get; private init; }

    public string? Comment { get; private init; }

    public DateTimeOffset CreatedAtUtc { get; private init; }

    public DateTimeOffset UpdatedAtUtc { get; private init; }

    public static Review Create(Guid studentUserId, Guid tutorUserId, int rating, string? comment, TimeProvider clock)
    {
        var now = clock.GetUtcNow();
        return new Review
        {
            Id = Guid.NewGuid(),
            StudentUserId = studentUserId,
            TutorUserId = tutorUserId,
            Rating = rating,
            Comment = comment,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }
}
