using TutorFlow.Domain.Common;
using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Domain.Scheduling.Events;

// A Session moves onto a different Availability Slot by a permitted actor
// (PRODUCT_REQUIREMENTS.md SCH-7; DOMAIN_MODEL.md: Domain Events). Phase
// 4.7: carries both slot ids (not just the new time) so the audit record —
// keyed by SessionId, per AuditDomainEventHandler's allowlist — captures
// which slot was vacated and which was newly occupied, not only when.
// docs/adr/ADR-023-online-lesson-meeting-provider-architecture.md: gains an
// optional trailing NewEndTimeUtc (default null) so MeetingSyncDomainEventHandler
// can reschedule an existing Meeting's own time range without querying
// Scheduling & Booking's tables directly — additive only, every existing
// positional construction still compiles unchanged.
public sealed record SessionRescheduled(
    SessionId SessionId,
    AvailabilitySlotId OldAvailabilitySlotId,
    AvailabilitySlotId NewAvailabilitySlotId,
    DateTime NewScheduledTimeUtc,
    DateTime? NewEndTimeUtc = null) : DomainEvent;
