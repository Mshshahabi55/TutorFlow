using TutorFlow.Domain.Common;

namespace TutorFlow.Domain.Identity.ValueObjects;

// The conceptual identity shared by Student, Tutor, Parent/Guardian, and
// Admin/Staff as specializations of Account (docs/database/DOMAIN_DATA_MODEL.md,
// Section 3). The generation scheme (Guid) is a pragmatic default for the
// Domain layer only — the physical identifier scheme remains a deferred
// decision (Section 6: Identity Strategy).
public sealed class AccountId : ValueObject
{
    private AccountId(Guid value) => Value = value;

    public Guid Value { get; }

    public static AccountId New() => new(Guid.NewGuid());

    public static AccountId From(Guid value) => new(Guard.Against.Default(value, nameof(value)));

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
