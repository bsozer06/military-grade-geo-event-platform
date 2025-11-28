namespace GeoEvents.Domain.Common;

/// <summary>
/// Base class for all entities with unique identifier.
/// Entities are defined by their identity, not their attributes.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; protected set; }
    
    protected Entity(Guid id)
    {
        Id = id;
    }
    
    protected Entity()
    {
        Id = Guid.NewGuid();
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Entity other)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (GetType() != other.GetType())
            return false;

        return Id == other.Id;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public static bool operator ==(Entity? a, Entity? b)
    {
        if (a is null && b is null)
            return true;

        if (a is null || b is null)
            return false;

        return a.Equals(b);
    }

    public static bool operator !=(Entity? a, Entity? b)
    {
        return !(a == b);
    }
}
