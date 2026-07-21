namespace TutorFlow.Application.Identity.Commands;

// docs/adr/ADR-017-authentication-mechanism-decision.md: Password credential,
// email identifier. Role is not supplied by the caller — the same email is
// looked up across all four role-specific Account tables, since email is
// unique per role, not globally.
public sealed record LoginCommand(string Email, string Password);
