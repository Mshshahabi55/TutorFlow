namespace TutorFlow.Application.Common;

// Produces the opaque reference token issued at login, and the deterministic
// hash used to store and look it up. The raw value is returned to the caller
// exactly once, at issuance, and never stored
// (docs/adr/ADR-017-authentication-mechanism-decision.md).
public interface ITokenGenerator
{
    string GenerateToken();

    string Hash(string token);
}
