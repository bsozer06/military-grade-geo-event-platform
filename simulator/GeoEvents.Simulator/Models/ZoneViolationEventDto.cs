record ZoneViolationEventDto(
    Guid EventId,
    string Type,
    DateTimeOffset Timestamp,
    string Source,
    string ZoneId,
    double Latitude,
    double Longitude,
    Dictionary<string, object> Metadata);
