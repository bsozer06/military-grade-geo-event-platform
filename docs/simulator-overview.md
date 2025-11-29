# Simulator Overview

The simulator is a .NET 9 console app that generates geospatial events and publishes them to RabbitMQ for backend processing. It includes a metrics HTTP server for visibility.

## Key Features
- Deterministic "convoy-moving" scenario
- Publishes `UNIT_POSITION` events to `geo.events` exchange with routing key `geo.unit.position`
- Metrics server: `GET /health`, `GET /metrics` (JSON), `GET /dashboard` (simple HTML)
- Configurable units, rate (msgs/s), speed, heading, start lat/lon, duration, seed

## Run Commands
```powershell
Push-Location "C:\_burhan\_projects\military-grade-geo-event-platform\simulator"
# Basic run, 10 units at 5/s for 120s
dotnet run -- --units 10 --rate 5 --duration 120 --metrics true --metricsPort 9090

# Higher throughput example
dotnet run -- --units 20 --rate 20 --duration 120 --metrics true --metricsPort 9090

# Probe metrics
Invoke-RestMethod -Uri http://localhost:9090/metrics | Format-List
Start-Process http://localhost:9090/dashboard
```

## Configuration
- CLI flags: `--key value` pairs (e.g., `--units 10 --rate 5 --duration 60`)
- Env vars (optional): `RabbitMQ__Host`, `RabbitMQ__Port`, `RabbitMQ__Username`, `RabbitMQ__Password`, `Simulator__Units`, `Simulator__Rate`, `Simulator__Duration`, `Simulator__MetricsEnabled`, `Simulator__MetricsPort`

## Structure
- `Program.cs`: CLI parsing, publishing loop, metrics server
- `Scenarios/ConvoyMovingScenario.cs`: deterministic movement logic
- `Services/RabbitPublisher.cs`: RabbitMQ publisher using sync `IModel.BasicPublish`

## Metrics JSON Shape
```json
{
  "published": 12345,
  "start": "2025-01-01T12:00:00Z",
  "now": "2025-01-01T12:01:23Z",
  "elapsedSeconds": 83.1,
  "ratePerSecond": 148.7
}
```

## Notes
- Graceful shutdown avoids unhandled TaskCanceled/OperationCanceled exceptions.
- If `/health` fails exactly when the run ends, the server is stopping — retry during active runs.
