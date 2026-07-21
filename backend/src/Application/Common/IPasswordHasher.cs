namespace TutorFlow.Application.Common;

// Verifies a plaintext password against a stored hash and produces new
// hashes at registration or an Admin-initiated reset. The specific hashing
// algorithm is an Infrastructure choice
// (docs/adr/ADR-017-authentication-mechanism-decision.md); this abstraction
// encodes no algorithm of its own.
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string hash);
}
