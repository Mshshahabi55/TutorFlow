using TutorFlow.Domain.Common;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Domain.Identity.Events;

// An Administrator resets an Account's password (docs/adr/ADR-017-authentication-mechanism-decision.md:
// Admin-assisted reset for this release).
public sealed record PasswordReset(AccountId AccountId) : DomainEvent;
