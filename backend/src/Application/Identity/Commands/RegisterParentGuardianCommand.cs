namespace TutorFlow.Application.Identity.Commands;

// Corresponds to ParentGuardian.Register(EmailAddress, PasswordHash)
// (PRODUCT_REQUIREMENTS.md IDR-1; docs/adr/ADR-017-authentication-mechanism-decision.md).
public sealed record RegisterParentGuardianCommand(string Email, string Password);
