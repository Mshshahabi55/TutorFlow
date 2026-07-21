using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;
using TutorFlow.Application.Common;

namespace TutorFlow.Infrastructure.Common;

// Argon2id (docs/adr/ADR-017-authentication-mechanism-decision.md, Launch
// Preparation Priority 1 — explicitly required, superseding this class's
// earlier PBKDF2 implementation, which was chosen at the time specifically
// to avoid a new dependency; that trade-off has since been revisited).
// Parameters follow the OWASP Password Storage Cheat Sheet's minimum
// recommended Argon2id profile and are stored alongside each hash so a
// future increase does not invalidate already-issued hashes.
internal sealed class PasswordHasher : IPasswordHasher
{
    private const int SaltSizeBytes = 16;
    private const int HashSizeBytes = 32;
    private const int MemorySizeKb = 19 * 1024;
    private const int Iterations = 2;
    private const int DegreeOfParallelism = 1;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        var hash = ComputeHash(password, salt, MemorySizeKb, Iterations, DegreeOfParallelism, HashSizeBytes);
        return $"{MemorySizeKb}.{Iterations}.{DegreeOfParallelism}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string hash)
    {
        var parts = hash.Split('.', 5);
        if (parts.Length != 5
            || !int.TryParse(parts[0], out var memorySizeKb)
            || !int.TryParse(parts[1], out var iterations)
            || !int.TryParse(parts[2], out var degreeOfParallelism))
        {
            return false;
        }

        byte[] salt;
        byte[] expectedHash;
        try
        {
            salt = Convert.FromBase64String(parts[3]);
            expectedHash = Convert.FromBase64String(parts[4]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actualHash = ComputeHash(password, salt, memorySizeKb, iterations, degreeOfParallelism, expectedHash.Length);

        // Constant-time comparison — a plain == or SequenceEqual would leak
        // timing information about how many leading bytes matched.
        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }

    private static byte[] ComputeHash(
        string password, byte[] salt, int memorySizeKb, int iterations, int degreeOfParallelism, int hashSizeBytes)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            MemorySize = memorySizeKb,
            Iterations = iterations,
            DegreeOfParallelism = degreeOfParallelism,
        };

        return argon2.GetBytes(hashSizeBytes);
    }
}
