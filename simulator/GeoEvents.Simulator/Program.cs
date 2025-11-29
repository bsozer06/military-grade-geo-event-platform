using System.Text.Json;
using System.Threading;

class Program
{
    static async Task Main(string[] args)
    {
        var config = SimulatorConfig.Parse(args);

        // Scenario load (overrides units & zone)
        if (!string.IsNullOrWhiteSpace(config.ScenarioPath) && File.Exists(config.ScenarioPath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(config.ScenarioPath);
                var scenario = JsonSerializer.Deserialize<ScenarioModel>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (scenario != null)
                {
                    if (scenario.Zone != null)
                    {
                        config = config with
                        {
                            EmitZoneViolations = true,
                            ZoneId = scenario.Zone.ZoneId ?? config.ZoneId,
                            ZoneCenterLat = scenario.Zone.CenterLat,
                            ZoneCenterLon = scenario.Zone.CenterLon,
                            ZoneRadiusMeters = scenario.Zone.RadiusMeters
                        };
                    }
                    if (scenario.Units?.Length > 0)
                    {
                        config = config with { UnitCount = scenario.Units.Length };
                    }
                    Console.WriteLine($"Scenario loaded from {config.ScenarioPath} (units={scenario.Units?.Length ?? 0}, zone={(scenario.Zone != null ? scenario.Zone.ZoneId : "none")})");
                    ScenarioCache.Model = scenario;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load scenario {config.ScenarioPath}: {ex.Message}");
            }
        }

        Console.WriteLine($"Simulator starting: units={config.UnitCount}, intervalMs={config.IntervalMs}, durationSec={config.DurationSeconds}, api={config.ApiBaseUrl}");

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); Console.WriteLine("Cancellation requested (Ctrl+C)."); };

        using var metricsServer = MetricsServer.Start(config.MetricsPort);
        var simulator = new UnitSimulator(config);

        if (!await simulator.WaitForHealthyAsync(maxAttempts: 12, delayMs: 500, cts.Token))
        {
            Console.WriteLine("API not healthy after retries. Aborting simulation.");
            return;
        }

        try
        {
            await simulator.RunAsync(cts.Token);
            Console.WriteLine($"Finished. Total events sent: {simulator.TotalSent}");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Simulation cancelled.");
        }
        Console.WriteLine("Metrics server stopped.");
    }
}
