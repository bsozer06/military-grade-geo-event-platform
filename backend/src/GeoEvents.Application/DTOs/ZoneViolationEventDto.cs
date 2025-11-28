using System;

namespace GeoEvents.Application.DTOs;

public sealed record ZoneViolationEventDto(
    Guid EventId,
    DateTimeOffset Timestamp,
    string Source,
    double Latitude,
    double Longitude,
    double? Altitude,
    string ZoneIdentifier,
    int Severity,
    string? Metadata)
    : GeoEventDto(EventId, "ZONE_VIOLATION", Timestamp, Source, Latitude, Longitude, Altitude);
