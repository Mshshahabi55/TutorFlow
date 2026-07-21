using TutorFlow.Application.Common;

namespace TutorFlow.Application.Identity.Validators;

// Shared structural checks for the Email/Password pair every credential-
// bearing command (Register*, Login) validates identically — extracted only
// because the four call sites would otherwise duplicate the exact same
// checks (docs/adr/ADR-007-validation-strategy.md: structural validation
// only, never a business rule; email format itself is validated by Domain's
// EmailAddress.Of, not repeated here).
internal static class CredentialValidation
{
    // NIST 800-63B-aligned: a minimum length only, no forced complexity or
    // rotation rules, which tend to push users toward predictable patterns
    // instead of stronger ones.
    private const int MinimumPasswordLength = 8;

    public static Error? ValidateEmail(string? email, string commandName)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return new Error($"{commandName}.Email.Empty", "Email is required.", ErrorType.Domain);
        }

        return null;
    }

    public static Error? ValidatePassword(string? password, string commandName)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return new Error($"{commandName}.Password.Empty", "Password is required.", ErrorType.Domain);
        }

        if (password.Length < MinimumPasswordLength)
        {
            return new Error(
                $"{commandName}.Password.TooShort",
                $"Password must be at least {MinimumPasswordLength} characters.",
                ErrorType.Domain);
        }

        return null;
    }
}
