using TutorFlow.Domain.Common;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Domain.Identity.Events;

// A Tutor submits their onboarding profile for Admin review — governance-
// relevant (it moves ProfileStatus Draft -> Submitted and enters the
// existing Admin approval queue) so it is covered by the audit trail
// (ADR-016), same as TutorApproved/TutorSuspended (ADR-024, 2026-07-28).
public sealed record TutorProfileSubmitted(AccountId TutorId) : DomainEvent;
