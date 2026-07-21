namespace TutorFlow.Application.Identity.Commands;

// The raw token value as presented by the caller (e.g. via the Authorization
// header) — hashed by the handler before lookup, exactly as at login
// (docs/adr/ADR-017-authentication-mechanism-decision.md).
public sealed record LogoutCommand(string Token);
