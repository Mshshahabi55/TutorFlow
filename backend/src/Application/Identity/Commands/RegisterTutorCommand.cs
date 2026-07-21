namespace TutorFlow.Application.Identity.Commands;

// Corresponds to Tutor.Register(EmailAddress, PasswordHash)
// (PRODUCT_REQUIREMENTS.md IDR-1; docs/adr/ADR-017-authentication-mechanism-decision.md).
// Password is plaintext only transiently, in memory, for the duration of this
// request — never logged, never persisted as anything but its hash.
public sealed record RegisterTutorCommand(string Email, string Password);
