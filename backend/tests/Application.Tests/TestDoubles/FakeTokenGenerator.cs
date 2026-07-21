using TutorFlow.Application.Common;

namespace TutorFlow.Application.Tests.TestDoubles;

internal sealed class FakeTokenGenerator : ITokenGenerator
{
    public string GenerateToken() => $"raw-token-{Guid.NewGuid():N}";

    public string Hash(string token) => $"hashed:{token}";
}
