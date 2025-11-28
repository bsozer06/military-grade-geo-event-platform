using GeoEvents.Application.DTOs;
using GeoEvents.Simulator.Scenarios;
using GeoEvents.Simulator.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

// Simple args parser: --key value pairs override env/defaults
var argDict = ParseArgs(args);

string GetStr(string key, string env, string def) =>
    argDict.TryGetValue(key, out var v) ? v : (Environment.GetEnvironmentVariable(env) ?? def);

int GetInt(string key, string env, int def) =>
    argDict.TryGetValue(key, out var v) && int.TryParse(v, out var n)
        ? n
        : (int.TryParse(Environment.GetEnvironmentVariable(env), out var e) ? e : def);

double GetDouble(string key, string env, double def) =>
    argDict.TryGetValue(key, out var v) && double.TryParse(v, out var n)
        ? n
        : (double.TryParse(Environment.GetEnvironmentVariable(env), out var e) ? e : def);

var host = GetStr("host", "RabbitMQ__Host", "localhost");
var port = GetInt("port", "RabbitMQ__Port", 5672);
var user = GetStr("user", "RabbitMQ__Username", "geouser");
var pass = GetStr("pass", "RabbitMQ__Password", "geopass123");
var vhost = GetStr("vhost", "RabbitMQ__VHost", "/");
var exchange = GetStr("exchange", "RabbitMQ__Exchange", "geo.events");
var routingKey = GetStr("rk", "Simulator__RoutingKey", "geo.unit.position");

var units = GetInt("units", "Simulator__Units", 10);
var rate = GetDouble("rate", "Simulator__Rate", 1.0);
var speed = GetDouble("speed", "Simulator__SpeedMps", 12.0);
var heading = GetDouble("heading", "Simulator__Heading", 90.0);
var lat = GetDouble("lat", "Simulator__Lat", 41.015137);
var lon = GetDouble("lon", "Simulator__Lon", 28.979530);
var seed = GetInt("seed", "Simulator__Seed", 1337);
var duration = GetInt("duration", "Simulator__Duration", 60);

Console.WriteLine($"Starting simulator → {units} units, {rate}/s, heading {heading}°, speed {speed} m/s");
using var publisher = new RabbitPublisher(host, port, user, pass, vhost, exchange);
var scenario = new ConvoyMovingScenario(units, speed, heading, (lat, lon), seed);

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

// Metrics config
var metricsEnabled = GetBool("metrics", "Simulator__MetricsEnabled", true);
var metricsPort = GetInt("metricsPort", "Simulator__MetricsPort", 9090);
var publishedCount = 0L;
var startTime = DateTimeOffset.UtcNow;

// Start metrics server in background (best-effort)
Task? metricsTask = null;
if (metricsEnabled)
{
    metricsTask = RunMetricsServerAsync(() => Interlocked.Read(ref publishedCount), startTime, metricsPort, cts.Token);
}

var intervalMs = (int)Math.Max(1, 1000.0 / rate);
var sw = System.Diagnostics.Stopwatch.StartNew();
var tick = 0;

try
{
    while (!cts.IsCancellationRequested)
    {
        foreach (var evt in scenario.Tick(tick))
        {
            await publisher.PublishAsync<UnitPositionEventDto>(evt, routingKey, cts.Token);
            Interlocked.Increment(ref publishedCount);
        }

        tick++;
        if (duration > 0 && sw.Elapsed.TotalSeconds >= duration)
            break;

        await Task.Delay(intervalMs, cts.Token);
    }
}
catch (OperationCanceledException)
{
    // graceful shutdown due to cancellation
}
finally
{
    // Graceful stop
    cts.Cancel();
    if (metricsTask != null)
    {
        try { await metricsTask; } catch { /* ignore */ }
    }
}

Console.WriteLine("Simulator finished.");
return 0;

static Dictionary<string, string> ParseArgs(string[] args)
{
    var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    for (int i = 0; i < args.Length; i++)
    {
        var a = args[i];
        if (a.StartsWith("--"))
        {
            var key = a.Substring(2);
            var val = i + 1 < args.Length && !args[i + 1].StartsWith("--") ? args[++i] : "true";
            dict[key] = val;
        }
    }
    return dict;
}

static bool GetBool(string key, string env, bool def)
{
    var dict = ParseArgs(Environment.GetCommandLineArgs());
    if (dict.TryGetValue(key, out var v))
    {
        if (bool.TryParse(v, out var b)) return b;
        if (int.TryParse(v, out var i)) return i != 0;
        return def;
    }
    var ev = Environment.GetEnvironmentVariable(env);
    if (string.IsNullOrWhiteSpace(ev)) return def;
    if (bool.TryParse(ev, out var eb)) return eb;
    if (int.TryParse(ev, out var ei)) return ei != 0;
    return def;
}

static async Task RunWebAppAsync(Func<long> getCount, DateTimeOffset start, int port, CancellationToken token)
{
    var builder = WebApplication.CreateBuilder();
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
        builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
    var app = builder.Build();

        app.UseCors();
    app.MapGet("/health", () => Results.Ok(new { status = "ok", now = DateTimeOffset.UtcNow }));
    app.MapGet("/metrics", () =>
    {
        var now = DateTimeOffset.UtcNow;
        var count = getCount();
        var elapsed = (now - start).TotalSeconds;
        var rate = elapsed > 0 ? count / elapsed : 0;
        return Results.Json(new
        {
            published = count,
            start = start,
            now,
            elapsedSeconds = elapsed,
            ratePerSecond = Math.Round(rate, 3)
        });
    });

        app.MapGet("/dashboard", () =>
        {
            const string html = """
    <!doctype html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>GeoEvents Simulator Dashboard</title>
    <style>
        body { font-family: ui-sans-serif, system-ui, -apple-system, Segoe UI, Roboto, sans-serif; margin: 24px; color: #0f172a; }
        h1 { margin: 0 0 8px 0; font-size: 22px; }
        .grid { display: grid; grid-template-columns: repeat(3, minmax(180px, 1fr)); gap: 12px; margin-top: 12px; }
        .card { border: 1px solid #e2e8f0; border-radius: 8px; padding: 16px; background: #ffffff; box-shadow: 0 1px 2px rgba(0,0,0,0.04); }
        .label { color: #64748b; font-size: 12px; text-transform: uppercase; letter-spacing: .08em; }
        .value { font-size: 28px; font-weight: 600; margin-top: 6px; }
        .muted { color: #64748b; font-size: 12px; margin-top: 6px; }
        .ok { color: #16a34a; }
        .bad { color: #dc2626; }
        a { color: #2563eb; text-decoration: none; }
    </style>
    <script>
        let timer;
        async function refresh() {
            try {
                const mRes = await fetch('/metrics', { cache: 'no-store' });
                const metrics = await mRes.json();
                document.getElementById('published').textContent = metrics.published.toLocaleString();
                document.getElementById('rate').textContent = metrics.ratePerSecond.toFixed(2);
                document.getElementById('elapsed').textContent = metrics.elapsedSeconds.toFixed(1) + 's';

                const hRes = await fetch('/health', { cache: 'no-store' });
                const ok = hRes.ok;
                document.getElementById('health').textContent = ok ? 'ok' : 'down';
                document.getElementById('health').className = ok ? 'value ok' : 'value bad';
            } catch (e) {
                document.getElementById('health').textContent = 'down';
                document.getElementById('health').className = 'value bad';
            }
        }
        function start() {
            refresh();
            timer = setInterval(refresh, 1000);
        }
        window.addEventListener('DOMContentLoaded', start);
    </script>
    </head>
    <body>
        <h1>GeoEvents Simulator Dashboard</h1>
        <div class="grid">
            <div class="card"><div class="label">Published</div><div id="published" class="value">-</div><div class="muted">Total messages sent</div></div>
            <div class="card"><div class="label">Rate</div><div id="rate" class="value">-</div><div class="muted">Msgs / second</div></div>
            <div class="card"><div class="label">Health</div><div id="health" class="value">-</div><div class="muted">Metrics server status</div></div>
        </div>
        <p class="muted" style="margin-top:16px">RabbitMQ UI: <a href="http://localhost:15672" target="_blank">http://localhost:15672</a></p>
    </body>
    </html>
""";
            return Results.Content(html, "text/html");
        });

    await app.StartAsync(token);
    try
    {
        await app.WaitForShutdownAsync(token);
    }
    catch (OperationCanceledException)
    {
        // expected on shutdown
    }
    finally
    {
        await app.StopAsync();
        await app.DisposeAsync();
    }
}

static async Task RunMetricsServerAsync(Func<long> getCount, DateTimeOffset start, int port, CancellationToken token)
{
    try
    {
        await RunWebAppAsync(getCount, start, port, token);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[metrics] disabled due to error: {ex.Message}");
    }
}
