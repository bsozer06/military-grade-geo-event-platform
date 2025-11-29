record UnitPositionEventDto(
    Guid EventId,
    string Type,
    DateTimeOffset Timestamp,
    string Source,
    double Latitude,
    double Longitude,
    Dictionary<string, object> Metadata);
