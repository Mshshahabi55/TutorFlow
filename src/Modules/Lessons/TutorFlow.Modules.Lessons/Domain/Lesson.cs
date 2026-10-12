namespace TutorFlow.Modules.Lessons.Domain;

// Mirrors Payments.Domain.Payment's shape exactly (private constructor, private-init
// properties, a static Create factory) — the minimum ceremony this module's one real invariant
// (one Lesson per BookingId, ADR-0016 §3) actually needs, not DDD aggregate ceremony applied
// ahead of a real need (CLAUDE.md).
public sealed class Lesson
{
    private Lesson()
    {
    }

    public Guid Id { get; private init; }

    public Guid BookingId { get; private init; }

    public Guid StudentUserId { get; private init; }

    public Guid TutorUserId { get; private init; }

    public DateTimeOffset StartUtc { get; private init; }

    public DateTimeOffset EndUtc { get; private init; }

    public LessonStatus Status { get; private init; }

    public DateTimeOffset CreatedAtUtc { get; private init; }

    public DateTimeOffset UpdatedAtUtc { get; private init; }

    public static Lesson CreateScheduled(
        Guid bookingId,
        Guid studentUserId,
        Guid tutorUserId,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        TimeProvider clock)
    {
        var now = clock.GetUtcNow();
        return new Lesson
        {
            Id = Guid.NewGuid(),
            BookingId = bookingId,
            StudentUserId = studentUserId,
            TutorUserId = tutorUserId,
            StartUtc = startUtc,
            EndUtc = endUtc,
            Status = LessonStatus.Scheduled,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }
}
