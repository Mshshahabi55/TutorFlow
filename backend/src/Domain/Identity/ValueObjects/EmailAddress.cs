using System.Text.RegularExpressions;
using TutorFlow.Domain.Common;

namespace TutorFlow.Domain.Identity.ValueObjects;

// The login identifier for every Account (docs/adr/ADR-017-authentication-mechanism-decision.md).
// Normalized to lowercase so "A@B.com" and "a@b.com" collide against the
// per-role unique index rather than silently coexisting as distinct rows.
public sealed partial class EmailAddress : ValueObject
{
    private EmailAddress(string value) => Value = value;

    public string Value { get; }

    public static EmailAddress Of(string value)
    {
        var trimmed = Guard.Against.NullOrWhiteSpace(value, nameof(value)).Trim();

        if (!ValidFormat().IsMatch(trimmed))
        {
            throw new ArgumentException("Value is not a valid email address.", nameof(value));
        }

        return new EmailAddress(trimmed.ToLowerInvariant());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    // A deliberately simple, permissive shape check — not full RFC 5322
    // validation. Rejecting a technically-valid-but-unusual address is worse
    // for a login identifier than accepting one that later bounces on a
    // future reset-email send (out of scope for this release; ADR-017).
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex ValidFormat();
}
