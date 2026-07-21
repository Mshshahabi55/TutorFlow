using TutorFlow.Application.Common;

namespace TutorFlow.Application.Tests.TestDoubles;

// Minimal hand-rolled test double — no mocking framework, no real hashing
// algorithm. Deterministic and reversible only in the sense that Verify can
// check its own Hash output; RegisterXCommandHandler only needs a non-empty
// string to hand to PasswordHash.Of.
internal sealed class FakePasswordHasher : IPasswordHasher
{
    public string Hash(string password) => $"hashed:{password}";

    public bool Verify(string password, string hash) => hash == Hash(password);
}
