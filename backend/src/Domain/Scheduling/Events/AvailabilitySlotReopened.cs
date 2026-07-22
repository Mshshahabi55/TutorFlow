using TutorFlow.Domain.Common;
using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Domain.Scheduling.Events;

// An Availability Slot becomes bookable again after its originating Session
// was cancelled (Phase 4.6: formally resolves DOMAIN_MODEL.md Open Question
// 7 — cancelling a Session does reopen its slot). Distinct from
// SessionCancelled (whose audit subject is the Session) so the Availability
// Slot's own lifecycle — declared, consumed, reopened — has a direct,
// first-class audit trail of its own (CONST-2 names availability state
// explicitly, not only booking state).
public sealed record AvailabilitySlotReopened(
    AvailabilitySlotId AvailabilitySlotId,
    SessionId CancelledSessionId) : DomainEvent;
