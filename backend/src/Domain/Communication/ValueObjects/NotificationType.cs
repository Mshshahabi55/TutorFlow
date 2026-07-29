namespace TutorFlow.Domain.Communication.ValueObjects;

// docs/adr/ADR-022-communication-and-notifications-architecture.md's
// Notification triggers table. AvailabilityChanged is named for forward
// compatibility but nothing raises it today — no recipient-resolution
// mechanism exists yet (see that ADR's Non-Goals) — it is not a fabricated
// capability, it is a named, not-yet-wired seam, the same pattern ADR-019
// already established for Currency.
public enum NotificationType
{
    BookingConfirmed = 0,
    LessonCancelled = 1,
    TutorReplied = 2,
    NewMessage = 3,
    ParentConfirmed = 4,
    AvailabilityChanged = 5,

    // docs/adr/ADR-023-online-lesson-meeting-provider-architecture.md.
    // MeetingCancelled is named for forward compatibility but has no
    // trigger wired today — Meeting cancellation only ever happens as
    // SessionCancelled's own side effect, which already raises
    // LessonCancelled; wiring both would double-notify the same real-world
    // fact. The same "named but not yet meaningfully populated" pattern
    // this enum already uses for AvailabilityChanged.
    MeetingCreated = 6,
    MeetingUpdated = 7,
    MeetingCancelled = 8,
}
