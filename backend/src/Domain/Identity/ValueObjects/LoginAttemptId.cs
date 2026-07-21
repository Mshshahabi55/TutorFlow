using TutorFlow.Domain.Common;

namespace TutorFlow.Domain.Identity.ValueObjects;

public sealed class LoginAttemptId : ValueObject
{
    private LoginAttemptId(Guid value) => Value = value;

    public Guid Value { get; }

    public static LoginAttemptId New() => new(Guid.NewGuid());

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
