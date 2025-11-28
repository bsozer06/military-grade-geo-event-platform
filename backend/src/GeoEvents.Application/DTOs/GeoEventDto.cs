using System;

namespace GeoEvents.Application.DTOs;

/// <summary>
/// Base DTO representing a geospatial event.
/// </summary>
public abstract record GeoEventDto(
    Guid EventId,
    string Type,
    DateTimeOffset Timestamp,
    string Source,
    double Latitude,
    double Longitude,
    double? Altitude);
