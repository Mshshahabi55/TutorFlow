using TutorFlow.Domain.Common;

namespace TutorFlow.Domain.Identity.ValueObjects;

// Wraps an already-hashed password value. This Value Object knows nothing
// about which hashing algorithm produced it and enforces no plaintext
// complexity policy of its own — that lives in Infrastructure (the hasher)
// and Application (command validators) respectively
// (docs/adr/ADR-017-authentication-mechanism-decision.md; docs/adr/ADR-007-validation-strategy.md).
public sealed class PasswordHash : ValueObject
{
    private PasswordHash(string value) => Value = value;

    public string Value { get; }

    public static PasswordHash Of(string hashedValue) =>
        new(Guard.Against.NullOrWhiteSpace(hashedValue, nameof(hashedValue)));

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
