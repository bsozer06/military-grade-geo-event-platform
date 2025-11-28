using GeoEvents.Domain.Common;
using GeoEvents.Domain.ValueObjects;

namespace GeoEvents.Domain.Entities;

/// <summary>
/// Represents a military unit, vehicle, or asset being tracked.
/// Aggregate root for unit-related operations.
/// </summary>
public class Unit : Entity
{
    private readonly List<IDomainEvent> _domainEvents = new();

    /// <summary>
    /// Unique identifier for the unit (e.g., "unit-alpha-1", "convoy-charlie-3").
    /// This is the business identifier, separate from the database Id.
    /// </summary>
    public string Identifier { get; private set; }

    /// <summary>
    /// Display name for the unit.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Type/classification of the unit (e.g., "Infantry", "Armored", "Air", "Naval").
    /// </summary>
    public string UnitType { get; private set; }

    /// <summary>
    /// Current geographic position of the unit.
    /// </summary>
    public GeoCoordinate Position { get; private set; }

    /// <summary>
    /// Current heading/direction of movement.
    /// </summary>
    public Heading? CurrentHeading { get; private set; }

    /// <summary>
    /// Current velocity/speed.
    /// </summary>
    public Velocity? CurrentVelocity { get; private set; }

    /// <summary>
    /// Operational status of the unit.
    /// </summary>
    public UnitStatus Status { get; private set; }

    /// <summary>
    /// Last time the unit's position was updated.
    /// </summary>
    public DateTimeOffset LastPositionUpdate { get; private set; }

    /// <summary>
    /// When the unit was first tracked/created in the system.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Additional metadata as JSON (for extensibility).
    /// </summary>
    public string? Metadata { get; private set; }

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    // Private constructor for EF Core
    private Unit() : base() 
    {
        Identifier = string.Empty;
        Name = string.Empty;
        UnitType = string.Empty;
        Position = null!;
    }

    private Unit(
        string identifier,
        string name,
        string unitType,
        GeoCoordinate initialPosition) : base()
    {
        Identifier = identifier ?? throw new ArgumentNullException(nameof(identifier));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        UnitType = unitType ?? throw new ArgumentNullException(nameof(unitType));
        Position = initialPosition ?? throw new ArgumentNullException(nameof(initialPosition));
        Status = UnitStatus.Active;
        CreatedAt = DateTimeOffset.UtcNow;
        LastPositionUpdate = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Creates a new unit with initial position.
    /// </summary>
    public static Unit Create(
        string identifier,
        string name,
        string unitType,
        GeoCoordinate initialPosition)
    {
        var unit = new Unit(identifier, name, unitType, initialPosition);
        
        unit._domainEvents.Add(new UnitCreatedEvent(
            unit.Id,
            unit.Identifier,
            unit.Position,
            DateTimeOffset.UtcNow));

        return unit;
    }

    /// <summary>
    /// Updates the unit's position and movement data.
    /// </summary>
    public void UpdatePosition(
        GeoCoordinate newPosition,
        Heading? heading = null,
        Velocity? velocity = null,
        DateTimeOffset? timestamp = null)
    {
        var oldPosition = Position;
        Position = newPosition ?? throw new ArgumentNullException(nameof(newPosition));
        CurrentHeading = heading;
        CurrentVelocity = velocity;
        LastPositionUpdate = timestamp ?? DateTimeOffset.UtcNow;

        _domainEvents.Add(new UnitPositionUpdatedEvent(
            Id,
            Identifier,
            oldPosition,
            newPosition,
            heading,
            velocity,
            LastPositionUpdate));
    }

    /// <summary>
    /// Changes the operational status of the unit.
    /// </summary>
    public void ChangeStatus(UnitStatus newStatus)
    {
        if (Status == newStatus)
            return;

        var oldStatus = Status;
        Status = newStatus;

        _domainEvents.Add(new UnitStatusChangedEvent(
            Id,
            Identifier,
            oldStatus,
            newStatus,
            DateTimeOffset.UtcNow));
    }

    /// <summary>
    /// Updates unit metadata.
    /// </summary>
    public void UpdateMetadata(string? metadata)
    {
        Metadata = metadata;
    }

    /// <summary>
    /// Checks if unit has moved significantly since last update.
    /// </summary>
    public bool HasMovedSignificantly(GeoCoordinate newPosition, double thresholdMeters = 10.0)
    {
        return Position.DistanceTo(newPosition) >= thresholdMeters;
    }

    /// <summary>
    /// Checks if the position data is stale.
    /// </summary>
    public bool IsPositionStale(TimeSpan threshold)
    {
        return DateTimeOffset.UtcNow - LastPositionUpdate > threshold;
    }

    public void ClearDomainEvents() => _domainEvents.Clear();
}

/// <summary>
/// Operational status of a unit.
/// </summary>
public enum UnitStatus
{
    Active,
    Inactive,
    Maintenance,
    Deployed,
    Returning,
    Offline
}

// Domain Events
public record UnitCreatedEvent(
    Guid UnitId,
    string Identifier,
    GeoCoordinate InitialPosition,
    DateTimeOffset OccurredAt) : DomainEvent;

public record UnitPositionUpdatedEvent(
    Guid UnitId,
    string Identifier,
    GeoCoordinate OldPosition,
    GeoCoordinate NewPosition,
    Heading? Heading,
    Velocity? Velocity,
    DateTimeOffset OccurredAt) : DomainEvent;

public record UnitStatusChangedEvent(
    Guid UnitId,
    string Identifier,
    UnitStatus OldStatus,
    UnitStatus NewStatus,
    DateTimeOffset OccurredAt) : DomainEvent;
