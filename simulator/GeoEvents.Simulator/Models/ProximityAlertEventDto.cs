record ProximityAlertEventDto(
    Guid EventId,
    string Type,
    DateTimeOffset Timestamp,
    string Source,
    string OtherUnit,
    double DistanceMeters,
    Dictionary<string, object> Metadata);
