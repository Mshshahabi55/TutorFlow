namespace TutorFlow.Application.Scheduling.Interfaces;

// docs/adr/ADR-025-scheduling-recurring-availability-and-booking-constraints.md
// Addendum — Booking Notice & Horizon (2026-07-29, Accepted): a booking-time
// policy check against the current instant, not a fact knowable from
// AvailabilitySlot/Session's own data alone — so it is read here by the
// Application-layer handlers that enforce it (BookSessionCommandHandler,
// RescheduleSessionCommandHandler), never inside the Domain aggregates
// themselves. Config-driven (Infrastructure implements this by reading the
// "SchedulingConstraints" section), matching the existing
// IMeetingProviderSettingsCatalog pattern: Application depends only on this
// narrow port, never on Microsoft.Extensions.Options or Infrastructure directly.
public interface ISchedulingConstraintsProvider
{
    TimeSpan MinimumBookingNotice { get; }

    TimeSpan MaximumBookingHorizon { get; }
}
