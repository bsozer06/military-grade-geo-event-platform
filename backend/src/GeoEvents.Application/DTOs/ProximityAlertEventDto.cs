using System;

namespace GeoEvents.Application.DTOs;

public sealed record ProximityAlertEventDto(
    Guid EventId,
    DateTimeOffset Timestamp,
    string Source,
    double Latitude,
    double Longitude,
    double? Altitude,
    string TargetIdentifier,
    double DistanceMeters,
    int Severity)
    : GeoEventDto(EventId, "PROXIMITY_ALERT", Timestamp, Source, Latitude, Longitude, Altitude);
