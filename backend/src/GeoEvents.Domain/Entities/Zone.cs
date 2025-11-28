using GeoEvents.Domain.Common;
using GeoEvents.Domain.ValueObjects;

namespace GeoEvents.Domain.Entities;

/// <summary>
/// Represents a geographic zone/area with spatial boundaries.
/// Can be used for restricted zones, areas of interest, operational boundaries, etc.
/// </summary>
public class Zone : Entity
{
    private readonly List<IDomainEvent> _domainEvents = new();

    /// <summary>
    /// Unique identifier for the zone (e.g., "no-fly-zone-1", "restricted-area-alpha").
    /// </summary>
    public string Identifier { get; private set; }

    /// <summary>
    /// Display name for the zone.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Type of zone (e.g., "Restricted", "SafeZone", "OperationalArea", "NoFlyZone").
    /// </summary>
    public string ZoneType { get; private set; }

    /// <summary>
    /// WKT (Well-Known Text) representation of the zone geometry.
    /// Can be POINT, POLYGON, MULTIPOLYGON, etc.
    /// Will be converted to PostGIS geometry type in infrastructure layer.
    /// </summary>
    public string GeometryWkt { get; private set; }

    /// <summary>
    /// Center point of the zone (calculated or specified).
    /// Useful for quick distance calculations.
    /// </summary>
    public GeoCoordinate? CenterPoint { get; private set; }

    /// <summary>
    /// Approximate radius in meters (for circular zones).
    /// Null for polygonal zones.
    /// </summary>
    public double? RadiusMeters { get; private set; }

    /// <summary>
    /// Whether this zone triggers alerts when entered.
    /// </summary>
    public bool IsRestricted { get; private set; }

    /// <summary>
    /// Zone priority/severity level (1-10, higher is more critical).
    /// </summary>
    public int PriorityLevel { get; private set; }

    /// <summary>
    /// When the zone becomes active.
    /// </summary>
    public DateTimeOffset? ActiveFrom { get; private set; }

    /// <summary>
    /// When the zone expires/deactivates.
    /// </summary>
    public DateTimeOffset? ActiveUntil { get; private set; }

    /// <summary>
    /// Additional metadata as JSON.
    /// </summary>
    public string? Metadata { get; private set; }

    /// <summary>
    /// When the zone was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    // Private constructor for EF Core
    private Zone() : base() 
    {
        Identifier = string.Empty;
        Name = string.Empty;
        ZoneType = string.Empty;
        GeometryWkt = string.Empty;
    }

    private Zone(
        string identifier,
        string name,
        string zoneType,
        string geometryWkt,
        bool isRestricted,
        int priorityLevel) : base()
    {
        if (string.IsNullOrWhiteSpace(identifier))
            throw new ArgumentException("Identifier cannot be empty", nameof(identifier));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));
        if (string.IsNullOrWhiteSpace(geometryWkt))
            throw new ArgumentException("Geometry cannot be empty", nameof(geometryWkt));
        if (priorityLevel < 1 || priorityLevel > 10)
            throw new ArgumentOutOfRangeException(nameof(priorityLevel), "Priority must be between 1 and 10");

        Identifier = identifier;
        Name = name;
        ZoneType = zoneType;
        GeometryWkt = geometryWkt;
        IsRestricted = isRestricted;
        PriorityLevel = priorityLevel;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Creates a circular zone from a center point and radius.
    /// </summary>
    public static Zone CreateCircular(
        string identifier,
        string name,
        string zoneType,
        GeoCoordinate center,
        double radiusMeters,
        bool isRestricted = true,
        int priorityLevel = 5)
    {
        if (radiusMeters <= 0)
            throw new ArgumentOutOfRangeException(nameof(radiusMeters), "Radius must be positive");

        // Create a simple WKT representation (will be converted to proper buffer in PostGIS)
        var wkt = center.ToWkt();

        var zone = new Zone(identifier, name, zoneType, wkt, isRestricted, priorityLevel)
        {
            CenterPoint = center,
            RadiusMeters = radiusMeters
        };

        zone._domainEvents.Add(new ZoneCreatedEvent(
            zone.Id,
            zone.Identifier,
            zone.ZoneType,
            center,
            radiusMeters,
            DateTimeOffset.UtcNow));

        return zone;
    }

    /// <summary>
    /// Creates a polygonal zone from WKT geometry.
    /// </summary>
    public static Zone CreatePolygonal(
        string identifier,
        string name,
        string zoneType,
        string polygonWkt,
        bool isRestricted = true,
        int priorityLevel = 5,
        GeoCoordinate? centerPoint = null)
    {
        var zone = new Zone(identifier, name, zoneType, polygonWkt, isRestricted, priorityLevel)
        {
            CenterPoint = centerPoint
        };

        zone._domainEvents.Add(new ZoneCreatedEvent(
            zone.Id,
            zone.Identifier,
            zone.ZoneType,
            centerPoint,
            null,
            DateTimeOffset.UtcNow));

        return zone;
    }

    /// <summary>
    /// Sets the active time window for this zone.
    /// </summary>
    public void SetActiveWindow(DateTimeOffset? from, DateTimeOffset? until)
    {
        if (from.HasValue && until.HasValue && from.Value >= until.Value)
            throw new ArgumentException("ActiveFrom must be before ActiveUntil");

        ActiveFrom = from;
        ActiveUntil = until;
    }

    /// <summary>
    /// Checks if the zone is currently active based on time window.
    /// </summary>
    public bool IsActive(DateTimeOffset? at = null)
    {
        var checkTime = at ?? DateTimeOffset.UtcNow;

        if (ActiveFrom.HasValue && checkTime < ActiveFrom.Value)
            return false;

        if (ActiveUntil.HasValue && checkTime > ActiveUntil.Value)
            return false;

        return true;
    }

    /// <summary>
    /// Updates zone metadata.
    /// </summary>
    public void UpdateMetadata(string? metadata)
    {
        Metadata = metadata;
    }

    /// <summary>
    /// Changes the zone's restriction status.
    /// </summary>
    public void UpdateRestrictionStatus(bool isRestricted)
    {
        if (IsRestricted == isRestricted)
            return;

        IsRestricted = isRestricted;

        _domainEvents.Add(new ZoneRestrictionChangedEvent(
            Id,
            Identifier,
            isRestricted,
            DateTimeOffset.UtcNow));
    }

    public void ClearDomainEvents() => _domainEvents.Clear();
}

// Domain Events
public record ZoneCreatedEvent(
    Guid ZoneId,
    string Identifier,
    string ZoneType,
    GeoCoordinate? CenterPoint,
    double? RadiusMeters,
    DateTimeOffset OccurredAt) : DomainEvent;

public record ZoneRestrictionChangedEvent(
    Guid ZoneId,
    string Identifier,
    bool IsRestricted,
    DateTimeOffset OccurredAt) : DomainEvent;
