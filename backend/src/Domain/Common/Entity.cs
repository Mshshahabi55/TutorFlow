namespace TutorFlow.Domain.Common;

// TId is generic because the identifier scheme is a deferred technology decision
// (see docs/database/DOMAIN_DATA_MODEL.md, Section 6: Identity Strategy).
public abstract class Entity<TId> : IEntity where TId : notnull
{
    public TId Id { get; protected init; } = default!;

    protected Entity() { }

    protected Entity(TId id) => Id = id;

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other || other.GetType() != GetType())
        {
            return false;
        }

        return Id.Equals(other.Id);
    }

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !(left == right);
}
