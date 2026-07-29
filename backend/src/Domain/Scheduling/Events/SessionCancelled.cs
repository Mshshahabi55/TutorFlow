using TutorFlow.Domain.Common;
using TutorFlow.Domain.Identity.ValueObjects;
using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Domain.Scheduling.Events;

// A Session is cancelled by a permitted actor (PRODUCT_REQUIREMENTS.md SCH-7;
// DOMAIN_MODEL.md: Domain Events).
//
// TutorId/StudentId/ParentGuardianId: additive, trailing, defaulted
// (docs/adr/ADR-022-communication-and-notifications-architecture.md) —
// Session.Cancel() already holds all three on `this`, they just weren't on
// the event. Lets a Communication-context listener create a "Lesson
// cancelled" Notification without querying Scheduling & Booking's own
// tables directly (ADR-002's Integration Rules).
public sealed record SessionCancelled(
    SessionId SessionId,
    TutorId? TutorId = null,
    StudentId? StudentId = null,
    ParentGuardianId? ParentGuardianId = null) : DomainEvent;
