using TutorFlow.Domain.Common;

namespace TutorFlow.Domain.Identity.ValueObjects;

public sealed class RelationshipId : ValueObject
{
    private RelationshipId(Guid value) => Value = value;

    public Guid Value { get; }

    public static RelationshipId New() => new(Guid.NewGuid());

    public static RelationshipId From(Guid value) => new(Guard.Against.Default(value, nameof(value)));

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
