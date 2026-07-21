namespace TutorFlow.Application.Identity.DTOs;

// Token is the raw, unhashed value — returned to the caller exactly once, at
// issuance, and never recoverable again since only its hash is stored
// (docs/adr/ADR-017-authentication-mechanism-decision.md). Not a FromDomain
// projection of a single aggregate: composed by LoginCommandHandler from the
// issued AuthToken and the matched Account's role.
public sealed record LoginResultDto(string Token, Guid AccountId, string Role, DateTime ExpiresAtUtc);
