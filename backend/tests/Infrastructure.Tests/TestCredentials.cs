using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Infrastructure.Tests;

// Every Account factory now requires a credential pair (Email/PasswordHash
// mandatory per the Identity bounded context's authentication work). Email
// must be unique per role table (real SQLite UNIQUE index) — a fresh call
// generates a new one every time so tests that create several accounts of
// the same role in one method never collide.
internal static class TestCredentials
{
    public static EmailAddress Email() => EmailAddress.Of($"test-{Guid.NewGuid():N}@example.com");

    public static PasswordHash Hash() => PasswordHash.Of("Test-Password-123!");
}
