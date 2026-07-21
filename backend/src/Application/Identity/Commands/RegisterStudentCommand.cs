namespace TutorFlow.Application.Identity.Commands;

// Corresponds to Student.Register(EmailAddress, PasswordHash, bool isMinor)
// (PRODUCT_REQUIREMENTS.md IDR-1, IDR-5, IDR-6; docs/adr/ADR-017-authentication-mechanism-decision.md).
// How IsMinor is determined (e.g. an age threshold) is not established
// (DOMAIN_MODEL.md Open Question 1); it is supplied here as an already-known
// fact, not computed.
public sealed record RegisterStudentCommand(string Email, string Password, bool IsMinor);
