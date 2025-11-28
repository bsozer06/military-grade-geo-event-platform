using GeoEvents.Application.DTOs;
using GeoEvents.Domain.Entities;
using GeoEvents.Domain.ValueObjects;

namespace GeoEvents.Application.Mapping;

/// <summary>
/// Maps domain entities to application DTOs.
/// </summary>
public static class GeoEventMapper
{
    public static UnitPositionEventDto ToUnitPositionDto(GeoEvent geoEvent)
    {
        var headingDegrees = geoEvent.Heading?.Degrees;
        var speedMps = geoEvent.Velocity?.ToMetersPerSecond();
        return new UnitPositionEventDto(
            geoEvent.Id,
            geoEvent.Timestamp,
            geoEvent.Source,
            geoEvent.Location.Latitude,
            geoEvent.Location.Longitude,
            geoEvent.Location.Altitude,
            headingDegrees,
            speedMps);
    }

    public static ZoneViolationEventDto ToZoneViolationDto(GeoEvent geoEvent, string zoneIdentifier, int severity, string? metadata)
    {
        return new ZoneViolationEventDto(
            geoEvent.Id,
            geoEvent.Timestamp,
            geoEvent.Source,
            geoEvent.Location.Latitude,
            geoEvent.Location.Longitude,
            geoEvent.Location.Altitude,
            zoneIdentifier,
            severity,
            metadata);
    }

    public static ProximityAlertEventDto ToProximityAlertDto(GeoEvent geoEvent, string targetIdentifier, double distanceMeters, int severity)
    {
        return new ProximityAlertEventDto(
            geoEvent.Id,
            geoEvent.Timestamp,
            geoEvent.Source,
            geoEvent.Location.Latitude,
            geoEvent.Location.Longitude,
            geoEvent.Location.Altitude,
            targetIdentifier,
            distanceMeters,
            severity);
    }
}
