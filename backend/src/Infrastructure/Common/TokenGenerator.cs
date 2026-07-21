using System.Security.Cryptography;
using System.Text;
using TutorFlow.Application.Common;

namespace TutorFlow.Infrastructure.Common;

// The reference/opaque token mechanism (docs/adr/ADR-017-authentication-mechanism-decision.md):
// GenerateToken produces the raw value returned to the caller exactly once;
// Hash produces the deterministic value actually stored and looked up, so a
// database read alone can never be replayed as a valid session token.
internal sealed class TokenGenerator : ITokenGenerator
{
    private const int TokenSizeBytes = 32;

    public string GenerateToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(TokenSizeBytes));

    public string Hash(string token) => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
