using System.Net.Http.Json;

class Program
{
    static async Task Main(string[] args)
    {
        var config = SimulatorConfig.Parse(args);
        Console.WriteLine($"Simulator starting: units={config.UnitCount}, intervalMs={config.IntervalMs}, durationSec={config.DurationSeconds}, api={config.ApiBaseUrl}");

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); Console.WriteLine("Cancellation requested (Ctrl+C)."); };

        var simulator = new UnitSimulator(config);

        // Health check before sending any events
        if (!await simulator.WaitForHealthyAsync(maxAttempts: 12, delayMs: 500, cts.Token))
        {
            Console.WriteLine("API not healthy after retries. Aborting simulation.");
            return;
        }

        await simulator.RunAsync(cts.Token);

        Console.WriteLine($"Finished. Total events sent: {simulator.TotalSent}");
    }
}

// Handles simulation logic
class UnitSimulator
{
    private readonly SimulatorConfig _config;
    private readonly HttpClient _http;
    private readonly Random _rng;
    private UnitState[] _units;
    public int TotalSent { get; private set; }

    public UnitSimulator(SimulatorConfig config)
    {
        _config = config;
        _http = new HttpClient { BaseAddress = new Uri(config.ApiBaseUrl) };
        _rng = new Random(config.Seed);
        _units = InitializeUnits();
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var start = DateTimeOffset.UtcNow;
        var nextPrint = start + TimeSpan.FromSeconds(5);

        while (!cancellationToken.IsCancellationRequested &&
               (DateTimeOffset.UtcNow - start).TotalSeconds < _config.DurationSeconds)
        {
            var loopStart = DateTimeOffset.UtcNow;

            await SendUnitEventsAsync(cancellationToken);

            if (DateTimeOffset.UtcNow >= nextPrint)
            {
                Console.WriteLine($"Progress: sent={TotalSent} elapsed={(DateTimeOffset.UtcNow-start).TotalSeconds:F1}s");
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

    private UnitState[] InitializeUnits() =>
        Enumerable.Range(0, _config.UnitCount)
            .Select(i => new UnitState(
                Identifier: $"unit-{i+1:D2}",
                Lat: _config.OriginLat + (_rng.NextDouble() - 0.5) * 0.01,
                Lon: _config.OriginLon + (_rng.NextDouble() - 0.5) * 0.01,
                HeadingDeg: _rng.Next(0,360)))
            .ToArray();

    private async Task SendUnitEventsAsync(CancellationToken cancellationToken)
    {
        for (int i = 0; i < _units.Length; i++)
        {
            _units[i] = UpdateUnit(_units[i]);
            var evt = CreateEvent(_units[i]);
            try
            {
                using var resp = await _http.PostAsJsonAsync("api/events/position", evt, cancellationToken: cancellationToken);
                if (resp.IsSuccessStatusCode)
                    TotalSent++;
                else
                    Console.WriteLine($"WARN post failed {resp.StatusCode}");
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine($"ERROR posting event: {ex.Message}");
            }
        }
    }

    private UnitState UpdateUnit(UnitState unit)
    {
        var speedMps = _config.MinSpeedMps + _rng.NextDouble() * (_config.MaxSpeedMps - _config.MinSpeedMps);
        var headingRad = unit.HeadingDeg * Math.PI / 180.0;
        var distanceMeters = speedMps * (_config.IntervalMs / 1000.0);
        var dLat = (distanceMeters / 111_320.0) * Math.Cos(headingRad);
        var dLon = (distanceMeters / (111_320.0 * Math.Cos(unit.Lat * Math.PI / 180.0))) * Math.Sin(headingRad);

        return unit with
        {
            Lat = unit.Lat + dLat,
            Lon = unit.Lon + dLon,
            HeadingDeg = (unit.HeadingDeg + _rng.Next(-10, 11) + 360) % 360
        };
    }

    private UnitPositionEventDto CreateEvent(UnitState unit)
    {
        var speedMps = _config.MinSpeedMps + _rng.NextDouble() * (_config.MaxSpeedMps - _config.MinSpeedMps);
        return new UnitPositionEventDto(
            EventId: Guid.NewGuid(),
            Type: "UNIT_POSITION",
            Timestamp: DateTimeOffset.UtcNow,
            Source: unit.Identifier,
            Latitude: unit.Lat,
            Longitude: unit.Lon,
            Metadata: new Dictionary<string, object>
            {
                ["speedMps"] = Math.Round(speedMps,2),
                ["heading"] = unit.HeadingDeg
            }
        );
    }
}

// ===== Types =====

record UnitState(string Identifier, double Lat, double Lon, int HeadingDeg);

record UnitPositionEventDto(
    Guid EventId,
    string Type,
    DateTimeOffset Timestamp,
    string Source,
    double Latitude,
    double Longitude,
    Dictionary<string, object> Metadata);

sealed record SimulatorConfig
{
    public int UnitCount { get; init; } = 5;
    public int IntervalMs { get; init; } = 1000;
    public int DurationSeconds { get; init; } = 60;
    public int Seed { get; init; } = 12345;
    public double OriginLat { get; init; } = 41.10;
    public double OriginLon { get; init; } = 29.00;
    // Default matches API launchSettings http profile
    public string ApiBaseUrl { get; init; } = "http://localhost:5045/";
    public double MinSpeedMps { get; init; } = 2;
    public double MaxSpeedMps { get; init; } = 15;

    public static SimulatorConfig Parse(string[] args)
    {
        var cfg = new SimulatorConfig();
        foreach (var arg in args)
        {
            var parts = arg.Split('=', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2) continue;
            var k = parts[0].Trim().ToLowerInvariant();
            var v = parts[1].Trim();
            switch (k)
            {
                case "units": if (int.TryParse(v, out var u)) cfg = cfg with { UnitCount = u }; break;
                case "interval": if (int.TryParse(v, out var i)) cfg = cfg with { IntervalMs = i }; break;
                case "duration": if (int.TryParse(v, out var d)) cfg = cfg with { DurationSeconds = d }; break;
                case "seed": if (int.TryParse(v, out var s)) cfg = cfg with { Seed = s }; break;
                case "originlat": if (double.TryParse(v, out var olat)) cfg = cfg with { OriginLat = olat }; break;
                case "originlon": if (double.TryParse(v, out var olon)) cfg = cfg with { OriginLon = olon }; break;
                case "api": cfg = cfg with { ApiBaseUrl = v.EndsWith('/') ? v : v + '/' }; break;
                case "minspeed": if (double.TryParse(v, out var mins)) cfg = cfg with { MinSpeedMps = mins }; break;
                case "maxspeed": if (double.TryParse(v, out var maxs)) cfg = cfg with { MaxSpeedMps = maxs }; break;
            }
        }
        return cfg;
    }
}
