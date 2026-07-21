using TutorFlow.Domain.Common;

namespace TutorFlow.Domain.Identity.ValueObjects;

// A search/filter attribute of a Tutor (PRODUCT_REQUIREMENTS.md DISC-1).
// Whether this represents an address, a city/region, or a travel radius is
// not established (DOMAIN_MODEL.md Open Question 12); this Value Object
// does not presume a structure and only requires a non-empty description.
public sealed class Location : ValueObject
{
    private Location(string value) => Value = value;

    public string Value { get; }

    public static Location Of(string value) =>
        new(Guard.Against.NullOrWhiteSpace(value, nameof(value)).Trim());

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
