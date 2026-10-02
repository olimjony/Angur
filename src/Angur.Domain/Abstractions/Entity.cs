namespace Angur.Domain.Abstractions;

public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    protected Entity(TId id) => Id = id;

    public TId Id { get; private init; }

    public bool Equals(Entity<TId>? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;

        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override bool Equals(object? obj)
        => Equals(obj as Entity<TId>);

    public override int GetHashCode()
        => HashCode.Combine(GetType(), Id);

    public static bool operator == (Entity<TId>? left, Entity<TId>? right)
        => Equals(left, right);

    public static bool operator != (Entity<TId>? left, Entity<TId>? right)
        => !Equals(left, right);
}