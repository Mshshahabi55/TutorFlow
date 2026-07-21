using TutorFlow.Domain.Common;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Domain.Identity.Events;

// Raised once, on the transition into lockout (Account.MaxFailedLoginAttempts
// reached) — not on every failed attempt (docs/adr/ADR-017-authentication-mechanism-decision.md,
// Launch Preparation Priority 1: account lock handling).
public sealed record AccountLocked(AccountId AccountId, DateTime LockedUntilUtc) : DomainEvent;
