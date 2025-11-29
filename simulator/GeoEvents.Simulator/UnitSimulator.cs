using System.Net.Http.Json;
using System.Threading;
using System.Text.Json;

class UnitSimulator
{
    private readonly SimulatorConfig _config;
    private readonly HttpClient _http;
    private readonly Random _rng;
    private UnitState[] _units;
    private readonly ScenarioModel? _scenario;
    public int TotalSent { get; private set; }
    private int _zoneViolationsSent;
    private readonly Dictionary<string, DateTimeOffset> _proximityLastSent = new();

    public UnitSimulator(SimulatorConfig config)
    {
        _config = config;
        _http = new HttpClient { BaseAddress = new Uri(config.ApiBaseUrl) };
        _rng = new Random(config.Seed);
        _scenario = ScenarioCache.Model;
        _units = InitializeUnits();
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var start = DateTimeOffset.UtcNow;
        var nextPrint = start + TimeSpan.FromSeconds(5);
        while (!cancellationToken.IsCancellationRequested && (DateTimeOffset.UtcNow - start).TotalSeconds < _config.DurationSeconds)
        {
            var loopStart = DateTimeOffset.UtcNow;
            Console.WriteLine($"EmitZoneViolations: {_config.EmitZoneViolations}, EmitProximityAlerts: {_config.EmitProximityAlerts}");
            await SendUnitEventsAsync(cancellationToken);
            if (_config.EmitZoneViolations)
                await EmitZoneViolationsAsync(cancellationToken);
            if (_config.EmitProximityAlerts)
                await EmitProximityAlertsAsync(cancellationToken);
            if (DateTimeOffset.UtcNow >= nextPrint)
            {
                // Console.WriteLine($"Progress: sent={TotalSent} elapsed={(DateTimeOffset.UtcNow-start).TotalSeconds:F1}s");
                nextPrint = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(5);
            }
            var elapsed = (DateTimeOffset.UtcNow - loopStart).TotalMilliseconds;
            var sleep = _config.IntervalMs - elapsed;
            if (sleep > 0)
                await Task.Delay(TimeSpan.FromMilliseconds(sleep), cancellationToken);
        }
    }

    public async Task<bool> WaitForHealthyAsync(int maxAttempts, int delayMs, CancellationToken cancellationToken)
    {
        for (int attempt = 1; attempt <= maxAttempts && !cancellationToken.IsCancellationRequested; attempt++)
        {
            try
            {
                using var resp = await _http.GetAsync("health", cancellationToken);
                if (resp.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Health check OK (attempt {attempt}).");
                    return true;
                }
                Console.WriteLine($"Health check status {(int)resp.StatusCode} (attempt {attempt}/{maxAttempts}).");
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine($"Health check failed: {ex.Message} (attempt {attempt}/{maxAttempts}).");
            }
            await Task.Delay(delayMs, cancellationToken);
        }
        return false;
    }

    private UnitState[] InitializeUnits()
    {
        if (_scenario?.Units?.Length > 0)
        {
            return _scenario.Units.Select(u => new UnitState(
                Identifier: u.Identifier ?? $"unit-{Guid.NewGuid():N}",
                Lat: u.Lat,
                Lon: u.Lon,
                HeadingDeg: u.HeadingDeg ?? _rng.Next(0,360))).ToArray();
        }
        if (_config.EmitZoneViolations && _config.SpawnUnitsInZone)
        {
            var list = new List<UnitState>();
            for (int i = 0; i < _config.UnitCount; i++)
            {
                var id = $"unit-{i+1:D2}";
                var heading = _rng.Next(0,360);
                var r = _rng.NextDouble() * _config.ZoneRadiusMeters * 0.8;
                var theta = _rng.NextDouble() * Math.PI * 2;
                var metersPerDegLat = 111320.0;
                var metersPerDegLon = 111320.0 * Math.Cos(_config.ZoneCenterLat * Math.PI / 180.0);
                var dLat = (Math.Sin(theta) * r) / metersPerDegLat;
                var dLon = (Math.Cos(theta) * r) / metersPerDegLon;
                var lat = _config.ZoneCenterLat + dLat;
                var lon = _config.ZoneCenterLon + dLon;
                list.Add(new UnitState(id, lat, lon, heading));
            }
            Console.WriteLine($"Spawned {list.Count} units inside zone radius {_config.ZoneRadiusMeters}m (center {_config.ZoneCenterLat},{_config.ZoneCenterLon})");
            return list.ToArray();
        }
        return Enumerable.Range(0, _config.UnitCount)
            .Select(i => new UnitState(
                Identifier: $"unit-{i+1:D2}",
                Lat: _config.OriginLat + (_rng.NextDouble() - 0.5) * 0.01,
                Lon: _config.OriginLon + (_rng.NextDouble() - 0.5) * 0.01,
                HeadingDeg: _rng.Next(0,360))).ToArray();
    }

    private async Task SendUnitEventsAsync(CancellationToken cancellationToken)
    {
        for (int i = 0; i < _units.Length; i++)
        {
            _units[i] = UpdateUnit(_units[i]);
            var evt = CreateUnitPositionEvent(_units[i]);
            try
            {
                using var resp = await _http.PostAsJsonAsync("api/events/position", evt, cancellationToken: cancellationToken);
                if (resp.IsSuccessStatusCode)
                {
                    TotalSent++;
                    Metrics.IncrementPositions();
                }
                else
                {
                    Console.WriteLine($"WARN post failed {resp.StatusCode}");
                    Metrics.IncrementFailures();
                }
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine($"ERROR posting event: {ex.Message}");
                Metrics.IncrementFailures();
            }
        }
    }

    private async Task EmitZoneViolationsAsync(CancellationToken cancellationToken)
    {
        for (int i = 0; i < _units.Length; i++)
        {
            var isInside = IsInsideZone(_units[i].Lat, _units[i].Lon);
            Console.WriteLine(isInside
                ? $"Unit {_units[i].Identifier} is INSIDE zone {_config.ZoneId}"
                : $"Unit {_units[i].Identifier} is outside zone {_config.ZoneId}");
            if (isInside)
            {
                var violation = new ZoneViolationEventDto(
                    EventId: Guid.NewGuid(), Type: "ZONE_VIOLATION", Timestamp: DateTimeOffset.UtcNow,
                    Source: _units[i].Identifier, ZoneId: _config.ZoneId,
                    Latitude: _units[i].Lat, Longitude: _units[i].Lon,
                    Metadata: new Dictionary<string, object>
                    {
                        ["radiusMeters"] = _config.ZoneRadiusMeters,
                        ["centerLat"] = _config.ZoneCenterLat,
                        ["centerLon"] = _config.ZoneCenterLon
                    }
                );
                try
                {
                    using var resp = await _http.PostAsJsonAsync("api/events/violation", violation, cancellationToken);
                    if (resp.IsSuccessStatusCode)
                    {
                        _zoneViolationsSent++;
                        Metrics.IncrementZoneViolations();
                    }
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine($"ERROR posting zone violation: {ex.Message}");
                    Metrics.IncrementFailures();
                }
            }
        }
    }

    private async Task EmitProximityAlertsAsync(CancellationToken cancellationToken)
    {
        for (int i = 0; i < _units.Length; i++)
        {
            for (int j = i + 1; j < _units.Length; j++)
            {
                var a = _units[i]; var b = _units[j];
                var distance = HaversineMeters(a.Lat, a.Lon, b.Lat, b.Lon);
                Console.WriteLine($"Distance between {a.Identifier} and {b.Identifier}: {distance} meters");
                if (distance <= _config.ProximityThresholdMeters)
                {
                    var key = a.Identifier.CompareTo(b.Identifier) < 0 ? $"{a.Identifier}|{b.Identifier}" : $"{b.Identifier}|{a.Identifier}";
                    if (_proximityLastSent.TryGetValue(key, out var last) && (DateTimeOffset.UtcNow - last).TotalSeconds < _config.ProximityRepeatSeconds)
                        continue;
                    _proximityLastSent[key] = DateTimeOffset.UtcNow;
                    var alert = new ProximityAlertEventDto(
                        EventId: Guid.NewGuid(), Type: "PROXIMITY_ALERT", Timestamp: DateTimeOffset.UtcNow,
                        Source: a.Identifier, OtherUnit: b.Identifier,
                        DistanceMeters: Math.Round(distance,2),
                        Metadata: new Dictionary<string, object> { ["thresholdMeters"] = _config.ProximityThresholdMeters }
                    );
                    try
                    {
                        using var resp = await _http.PostAsJsonAsync("api/events/generic", alert, cancellationToken);
                        if (resp.IsSuccessStatusCode)
                            Metrics.IncrementProximity();
                        else
                            Metrics.IncrementFailures();
                    }
                    catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                    {
                        Console.WriteLine($"ERROR posting proximity alert: {ex.Message}");
                        Metrics.IncrementFailures();
                    }
                }
            }
        }
    }

    private bool IsInsideZone(double lat, double lon)
    {
        var dLat = DegreesToRadians(lat - _config.ZoneCenterLat);
        var dLon = DegreesToRadians(lon - _config.ZoneCenterLon);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(_config.ZoneCenterLat)) * Math.Cos(DegreesToRadians(lat)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        const double earthRadiusMeters = 6371000;
        var distance = earthRadiusMeters * c;
        Console.WriteLine($"Distance to zone center: {distance:F2}m (radius {_config.ZoneRadiusMeters}m)");
        return distance <= _config.ZoneRadiusMeters;
    }

    private static double DegreesToRadians(double deg) => deg * Math.PI / 180.0;

    private UnitState UpdateUnit(UnitState unit)
    {
        var speedMps = _config.MinSpeedMps + _rng.NextDouble() * (_config.MaxSpeedMps - _config.MinSpeedMps);
        var headingRad = unit.HeadingDeg * Math.PI / 180.0;
        var distanceMeters = speedMps * (_config.IntervalMs / 1000.0);
        var dLat = (distanceMeters / 111_320.0) * Math.Cos(headingRad);
        var dLon = (distanceMeters / (111_320.0 * Math.Cos(unit.Lat * Math.PI / 180.0))) * Math.Sin(headingRad);
        return unit with { Lat = unit.Lat + dLat, Lon = unit.Lon + dLon, HeadingDeg = (unit.HeadingDeg + _rng.Next(-10, 11) + 360) % 360 };
    }

    private UnitPositionEventDto CreateUnitPositionEvent(UnitState unit)
    {
        var speedMps = _config.MinSpeedMps + _rng.NextDouble() * (_config.MaxSpeedMps - _config.MinSpeedMps);
        return new UnitPositionEventDto(
            EventId: Guid.NewGuid(), Type: "UNIT_POSITION", Timestamp: DateTimeOffset.UtcNow,
            Source: unit.Identifier, Latitude: unit.Lat, Longitude: unit.Lon,
            Metadata: new Dictionary<string, object> { ["speedMps"] = Math.Round(speedMps,2), ["heading"] = unit.HeadingDeg }
        );
    }

    private static double HaversineMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000;
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);
        var a = Math.Sin(dLat/2)*Math.Sin(dLat/2) + Math.Cos(DegreesToRadians(lat1))*Math.Cos(DegreesToRadians(lat2))*Math.Sin(dLon/2)*Math.Sin(dLon/2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1-a));
        return R * c;
    }
}
