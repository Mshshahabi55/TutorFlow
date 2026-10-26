namespace TutorFlow.Modules.Messaging.Domain;

// ADR-0030: one row per Student<->Tutor relationship, matching Reviews' identical
// (StudentUserId, TutorUserId) uniqueness shape — never a per-topic or per-booking thread. No
// Booking is required to create one (DOCUMENTED Preply behavior: pre-trial contact is possible).
// Read state is two plain timestamps rather than a per-message receipts table — the simplest
// model consistent with real unread-badge mechanics being NOT VERIFIED anywhere in this
// project's evidence; an explicit engineering default, not a claimed parity match.
public sealed class Conversation
{
    private Conversation()
    {
    }

    public Guid Id { get; private init; }

    public Guid StudentUserId { get; private init; }

    public Guid TutorUserId { get; private init; }

    public DateTimeOffset CreatedAtUtc { get; private init; }

    public DateTimeOffset? LastMessageAtUtc { get; private set; }

    public DateTimeOffset? StudentLastReadAtUtc { get; private set; }

    public DateTimeOffset? TutorLastReadAtUtc { get; private set; }

    public static Conversation Create(Guid studentUserId, Guid tutorUserId, TimeProvider clock) => new()
    {
        Id = Guid.NewGuid(),
        StudentUserId = studentUserId,
        TutorUserId = tutorUserId,
        CreatedAtUtc = clock.GetUtcNow()
    };
}
