using TutorFlow.Domain.Common;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Domain.Identity.Events;

// Raised for every login attempt, successful or not
// (docs/adr/ADR-017-authentication-mechanism-decision.md: failed logins are
// audited alongside successful mutating actions). AccountId is null when no
// Account matched the attempted email at all — a genuinely subject-less
// audit record, not a fabricated one.
public sealed record LoginAttempted(
    LoginAttemptId LoginAttemptId,
    string Email,
    bool Succeeded,
    AccountId? AccountId) : DomainEvent;
