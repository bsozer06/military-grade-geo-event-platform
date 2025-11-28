using GeoEvents.Application.DTOs;

namespace GeoEvents.Simulator.Scenarios;

public sealed class ConvoyMovingScenario
{
    private readonly int _unitCount;
    private readonly double _speedMps;
    private readonly double _headingDegrees;
    private readonly (double lat, double lon) _start;
    private readonly Random _rng;

    public ConvoyMovingScenario(int unitCount, double speedMps, double headingDegrees, (double lat, double lon) start, int seed)
    {
        _unitCount = unitCount;
        _speedMps = speedMps;
        _headingDegrees = headingDegrees % 360.0;
        _start = start;
        _rng = new Random(seed);
    }

    public IEnumerable<UnitPositionEventDto> Tick(int tickIndexUtcSeconds)
    {
        var now = DateTimeOffset.UtcNow;
        for (int i = 0; i < _unitCount; i++)
        {
            var idSuffix = (i + 1).ToString("D2");
            var source = $"convoy-unit-{idSuffix}";
            // Stagger each unit by small offset so they don't overlap exactly
            var offsetMeters = i * 20.0; // 20m spacing
            var (lat, lon) = OffsetFromStart(tickIndexUtcSeconds, offsetMeters);

            yield return new UnitPositionEventDto(
                EventId: Guid.NewGuid(),
                Timestamp: now,
                Source: source,
                Latitude: lat,
                Longitude: lon,
                Altitude: null,
                HeadingDegrees: _headingDegrees,
                SpeedMps: _speedMps
            );
        }
    }

    private (double lat, double lon) OffsetFromStart(int seconds, double lateralOffsetMeters)
    {
        // Move forward along heading by distance = speed * time
        var distance = _speedMps * seconds; // meters
        // Convert meters to lat/lon deltas using simple equirectangular approximation
        var latRad = _start.lat * Math.PI / 180.0;
        const double metersPerDegLat = 111_320.0;
        var metersPerDegLon = Math.Cos(latRad) * 111_320.0;

        var headingRad = _headingDegrees * Math.PI / 180.0;
        var dx = distance * Math.Cos(headingRad);
        var dy = distance * Math.Sin(headingRad);

        // Lateral offset to form a line/convoy width (perpendicular to heading)
        var lateralRad = (headingRad + Math.PI / 2.0);
        dx += lateralOffsetMeters * Math.Cos(lateralRad);
        dy += lateralOffsetMeters * Math.Sin(lateralRad);

        var dLon = dx / metersPerDegLon;
        var dLat = dy / metersPerDegLat;
        return (_start.lat + dLat, _start.lon + dLon);
    }
}
