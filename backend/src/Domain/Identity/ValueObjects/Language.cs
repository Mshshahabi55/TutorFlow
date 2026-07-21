using TutorFlow.Domain.Common;

namespace TutorFlow.Domain.Identity.ValueObjects;

// A Tutor's teaching language, used as a search/filter attribute
// (PRODUCT_REQUIREMENTS.md DISC-1).
public sealed class Language : ValueObject
{
    private Language(string value) => Value = value;

    public string Value { get; }

    public static Language Of(string value) =>
        new(Guard.Against.NullOrWhiteSpace(value, nameof(value)).Trim());

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
