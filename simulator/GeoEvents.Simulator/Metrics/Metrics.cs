using System.Text.Json;
using System.Net;
using System.Threading;

static class Metrics
{
    private static long _positions; private static long _violations; private static long _proximities; private static long _failures;
    public static void IncrementPositions() => Interlocked.Increment(ref _positions);
    public static void IncrementZoneViolations() => Interlocked.Increment(ref _violations);
    public static void IncrementProximity() => Interlocked.Increment(ref _proximities);
    public static void IncrementFailures() => Interlocked.Increment(ref _failures);
    public static MetricsSnapshot Snapshot() => new(
        Interlocked.Read(ref _positions),
        Interlocked.Read(ref _violations),
        Interlocked.Read(ref _proximities),
        Interlocked.Read(ref _failures));
}

readonly record struct MetricsSnapshot(long PositionEvents, long ZoneViolations, long ProximityAlerts, long Failures);

sealed class MetricsServer : IDisposable
{
    private readonly HttpListener _listener;
    private readonly CancellationTokenSource _cts = new();
    private MetricsServer(int port)
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{port}/");
    }
    public static MetricsServer Start(int port)
    {
        var server = new MetricsServer(port);
        try
        {
            server._listener.Start();
            _ = Task.Run(() => server.RunAsync());
            Console.WriteLine($"Metrics server listening on http://localhost:{port}/metrics");
        }
        catch (HttpListenerException ex)
        {
            Console.WriteLine($"Metrics server failed to start: {ex.Message}");
        }
        return server;
    }
    private async Task RunAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            HttpListenerContext? ctx = null;
            try { ctx = await _listener.GetContextAsync(); }
            catch when (_cts.IsCancellationRequested) { break; }
            catch (Exception ex) { Console.WriteLine($"Metrics accept error: {ex.Message}"); continue; }
            if (ctx == null) continue;
            var path = ctx.Request.Url?.AbsolutePath;
            if (path == "/metrics")
            {
                var snap = Metrics.Snapshot();
                var payload = $"geo_simulator_position_events_total {snap.PositionEvents}\n" +
                              $"geo_simulator_zone_violations_total {snap.ZoneViolations}\n" +
                              $"geo_simulator_proximity_alerts_total {snap.ProximityAlerts}\n" +
                              $"geo_simulator_failures_total {snap.Failures}\n";
                var bytes = System.Text.Encoding.UTF8.GetBytes(payload);
                ctx.Response.ContentType = "text/plain";
                ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
                ctx.Response.OutputStream.Close();
            }
            else if (path == "/metrics/json")
            {
                var snap = Metrics.Snapshot();
                var json = JsonSerializer.Serialize(snap);
                var bytes = System.Text.Encoding.UTF8.GetBytes(json);
                ctx.Response.ContentType = "application/json";
                ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
                ctx.Response.OutputStream.Close();
            }
            else
            {
                ctx.Response.StatusCode = 404;
                ctx.Response.OutputStream.Close();
            }
        }
    }
    public void Dispose()
    {
        _cts.Cancel();
        try { _listener.Close(); } catch { }
    }
}
