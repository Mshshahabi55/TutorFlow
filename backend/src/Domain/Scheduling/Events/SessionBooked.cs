using TutorFlow.Domain.Common;
using TutorFlow.Domain.Identity.ValueObjects;
using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Domain.Scheduling.Events;

// A Student/Parent books an Availability Slot, producing a Session in
// Scheduled status (PRODUCT_REQUIREMENTS.md SCH-5, SCH-6; DOMAIN_MODEL.md:
// Domain Events).
//
// ParentGuardianId: additive, trailing, defaulted
// (docs/adr/ADR-022-communication-and-notifications-architecture.md) —
// Session.Book(...) already receives this value, it just wasn't on the
// event. Existing positional constructions (AuditDomainEventHandlerTests)
// keep compiling unchanged; the default only matters for callers that
// don't pass it.
public sealed record SessionBooked(
    SessionId SessionId,
    TutorId TutorId,
    StudentId StudentId,
    AvailabilitySlotId AvailabilitySlotId,
    ParentGuardianId? ParentGuardianId = null) : DomainEvent;
