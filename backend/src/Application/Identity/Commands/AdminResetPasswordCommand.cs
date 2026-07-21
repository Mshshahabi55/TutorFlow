namespace TutorFlow.Application.Identity.Commands;

// Admin-assisted password reset — no self-service recovery flow exists yet
// (docs/adr/ADR-017-authentication-mechanism-decision.md). Role-agnostic:
// works for any of the four Account types, since AccountId alone identifies
// the target uniquely.
public sealed record AdminResetPasswordCommand(Guid AccountId, string NewPassword);
