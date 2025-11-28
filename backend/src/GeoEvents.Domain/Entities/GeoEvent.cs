using GeoEvents.Domain.Common;
using GeoEvents.Domain.ValueObjects;

namespace GeoEvents.Domain.Entities;

/// <summary>
/// Represents a geospatial event that occurred in the system.
/// This is the aggregate root for event data that flows through RabbitMQ.
/// Note: This is different from DomainEvent (which is for internal domain events).
/// </summary>
public class GeoEvent : Entity
{
    /// <summary>
    /// Type of the event (e.g., "UNIT_POSITION", "ZONE_VIOLATION", "PROXIMITY_ALERT").
    /// </summary>
    public string EventType { get; private set; }

    /// <summary>
    /// Source/origin of the event (e.g., unit identifier, sensor id).
    /// </summary>
    public string Source { get; private set; }

    /// <summary>
    /// Geographic location where the event occurred.
    /// </summary>
    public GeoCoordinate Location { get; private set; }

    /// <summary>
    /// When the event occurred (actual event time, not processing time).
    /// </summary>
    public DateTimeOffset Timestamp { get; private set; }

    /// <summary>
    /// Optional heading associated with the event.
    /// </summary>
    public Heading? Heading { get; private set; }

    /// <summary>
    /// Optional velocity associated with the event.
    /// </summary>
    public Velocity? Velocity { get; private set; }

    /// <summary>
    /// Severity level of the event (1-10).
    /// </summary>
    public int Severity { get; private set; }

    /// <summary>
    /// Additional event metadata as JSON.
    /// Can include event-specific fields like sensor readings, alert details, etc.
    /// </summary>
    public string? Metadata { get; private set; }

    /// <summary>
    /// When this event was processed/stored in the system.
    /// </summary>
    public DateTimeOffset ProcessedAt { get; private set; }

    /// <summary>
    /// Correlation ID for tracking related events.
    /// </summary>
    public Guid? CorrelationId { get; private set; }

    // Private constructor for EF Core
    private GeoEvent() : base() 
    {
        EventType = string.Empty;
        Source = string.Empty;
        Location = null!;
    }

    private GeoEvent(
        string eventType,
        string source,
        GeoCoordinate location,
        DateTimeOffset timestamp,
        int severity) : base()
    {
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("EventType cannot be empty", nameof(eventType));
        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("Source cannot be empty", nameof(source));
        if (severity < 1 || severity > 10)
            throw new ArgumentOutOfRangeException(nameof(severity), "Severity must be between 1 and 10");

        EventType = eventType;
        Source = source;
        Location = location ?? throw new ArgumentNullException(nameof(location));
        Timestamp = timestamp;
        Severity = severity;
        ProcessedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Creates a new geospatial event.
    /// </summary>
    public static GeoEvent Create(
        string eventType,
        string source,
        GeoCoordinate location,
        DateTimeOffset timestamp,
        int severity = 5,
        Heading? heading = null,
        Velocity? velocity = null,
        string? metadata = null,
        Guid? correlationId = null)
    {
        var geoEvent = new GeoEvent(eventType, source, location, timestamp, severity)
        {
            Heading = heading,
            Velocity = velocity,
            Metadata = metadata,
            CorrelationId = correlationId
        };

        return geoEvent;
    }

    /// <summary>
    /// Creates a unit position event.
    /// </summary>
    public static GeoEvent CreateUnitPosition(
        string unitIdentifier,
        GeoCoordinate location,
        DateTimeOffset timestamp,
        Heading? heading = null,
        Velocity? velocity = null,
        string? metadata = null)
    {
        return Create(
            "UNIT_POSITION",
            unitIdentifier,
            location,
            timestamp,
            severity: 1,
            heading,
            velocity,
            metadata);
    }

    /// <summary>
    /// Creates a zone violation event.
    /// </summary>
    public static GeoEvent CreateZoneViolation(
        string unitIdentifier,
        string zoneIdentifier,
        GeoCoordinate location,
        DateTimeOffset timestamp,
        int severity = 8,
        string? metadata = null)
    {
        return Create(
            "ZONE_VIOLATION",
            unitIdentifier,
            location,
            timestamp,
            severity,
            metadata: metadata);
    }

    /// <summary>
    /// Creates a proximity alert event.
    /// </summary>
    public static GeoEvent CreateProximityAlert(
        string unitIdentifier,
        string targetIdentifier,
        GeoCoordinate location,
        DateTimeOffset timestamp,
        double distanceMeters,
        int severity = 6)
    {
        var metadata = System.Text.Json.JsonSerializer.Serialize(new
        {
            targetUnit = targetIdentifier,
            distance = distanceMeters
        });

        return Create(
            "PROXIMITY_ALERT",
            unitIdentifier,
            location,
            timestamp,
            severity,
            metadata: metadata);
    }

    /// <summary>
    /// Checks if this event is recent (within specified duration).
    /// </summary>
    public bool IsRecent(TimeSpan within)
    {
        return DateTimeOffset.UtcNow - Timestamp <= within;
    }

    /// <summary>
    /// Checks if event processing was delayed.
    /// </summary>
    public bool WasDelayed(TimeSpan threshold)
    {
        return ProcessedAt - Timestamp > threshold;
    }
}
