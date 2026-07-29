namespace TutorFlow.Domain.Identity.ValueObjects;

// ADR-024 (Accepted, 2026-07-28): governs only whether a Tutor has asked to
// enter the existing Admin approval queue — a separate concern from
// IsApproved/IsSuspended, which are unchanged. A Draft Tutor is never
// discoverable (IsDiscoverable is still computed purely from
// IsApproved/IsSuspended, and a never-submitted Tutor is never IsApproved),
// so no additional discoverability gate is needed.
public enum TutorProfileStatus
{
    Draft,
    Submitted,
}
