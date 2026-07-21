using TutorFlow.Domain.Common;

namespace TutorFlow.Domain.Identity.ValueObjects;

// A search/filter attribute of a Tutor (PRODUCT_REQUIREMENTS.md DISC-1).
// Whether this is a fixed taxonomy or free text is not established
// (DOMAIN_MODEL.md Open Question 10); this Value Object does not presume
// either and only requires a non-empty value.
public sealed class Subject : ValueObject
{
    private Subject(string value) => Value = value;

    public string Value { get; }

    public static Subject Of(string value) =>
        new(Guard.Against.NullOrWhiteSpace(value, nameof(value)).Trim());

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
