using System;

namespace GeoEvents.Application.DTOs;

public sealed record UnitPositionEventDto(
    Guid EventId,
    DateTimeOffset Timestamp,
    string Source,
    double Latitude,
    double Longitude,
    double? Altitude,
    double? HeadingDegrees,
    double? SpeedMps)
    : GeoEventDto(EventId, "UNIT_POSITION", Timestamp, Source, Latitude, Longitude, Altitude);
