using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Tests.TestDoubles;

// Every Account factory now requires a credential pair (Email/PasswordHash
// mandatory per the Identity bounded context's authentication work). Tests
// that only care about the resulting aggregate's behavior — not about a
// specific credential value — get a fresh, valid, unique one from here on
// every call, so nothing collides.
internal static class TestCredentials
{
    public static EmailAddress Email() => EmailAddress.Of($"test-{Guid.NewGuid():N}@example.com");

    public static PasswordHash Hash() => PasswordHash.Of("Test-Password-123!");
}
