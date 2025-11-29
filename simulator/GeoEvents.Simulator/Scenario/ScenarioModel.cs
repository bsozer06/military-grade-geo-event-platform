sealed record ScenarioModel(ScenarioUnit[]? Units, ScenarioZone? Zone);
sealed record ScenarioUnit(string? Identifier, double Lat, double Lon, int? HeadingDeg);
sealed record ScenarioZone(string? ZoneId, double CenterLat, double CenterLon, double RadiusMeters);
static class ScenarioCache { public static ScenarioModel? Model; }
