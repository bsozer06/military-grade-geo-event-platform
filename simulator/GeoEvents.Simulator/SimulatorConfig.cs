sealed record SimulatorConfig
{
    public int UnitCount { get; init; } = 5;
    public int IntervalMs { get; init; } = 1000;
    public int DurationSeconds { get; init; } = 60;
    public int Seed { get; init; } = 12345;
    public double OriginLat { get; init; } = 41.10;
    public double OriginLon { get; init; } = 29.00;
    public string ApiBaseUrl { get; init; } = "http://localhost:5045/";
    public double MinSpeedMps { get; init; } = 100;
    public double MaxSpeedMps { get; init; } = 250;
    public bool EmitZoneViolations { get; init; } = false;
    public string ZoneId { get; init; } = "zone-alpha";
    public double ZoneCenterLat { get; init; } = 41.105;
    public double ZoneCenterLon { get; init; } = 29.005;
    public double ZoneRadiusMeters { get; init; } = 150;
    public string? ScenarioPath { get; init; }
    public int MetricsPort { get; init; } = 9091;
    public bool EmitProximityAlerts { get; init; } = false;
    public double ProximityThresholdMeters { get; init; } = 200;
    public int ProximityRepeatSeconds { get; init; } = 5;

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
                case "emitzone": if (bool.TryParse(v, out var ez)) cfg = cfg with { EmitZoneViolations = ez }; break;
                case "zoneid": cfg = cfg with { ZoneId = v }; break;
                case "zonecenterlat": if (double.TryParse(v, out var zcl)) cfg = cfg with { ZoneCenterLat = zcl }; break;
                case "zonecenterlon": if (double.TryParse(v, out var zco)) cfg = cfg with { ZoneCenterLon = zco }; break;
                case "zoneradius": if (double.TryParse(v, out var zr)) cfg = cfg with { ZoneRadiusMeters = zr }; break;
                case "scenario": cfg = cfg with { ScenarioPath = v }; break;
                case "metricsport": if (int.TryParse(v, out var mp)) cfg = cfg with { MetricsPort = mp }; break;
                case "emitproximity": if (bool.TryParse(v, out var ep)) cfg = cfg with { EmitProximityAlerts = ep }; break;
                case "proxthreshold": if (double.TryParse(v, out var pt)) cfg = cfg with { ProximityThresholdMeters = pt }; break;
                case "proxrepeat": if (int.TryParse(v, out var prs)) cfg = cfg with { ProximityRepeatSeconds = prs }; break;
            }
        }
        return cfg;
    }
}
