# Simple Geo Events Simulator

A minimal deterministic simulator that posts UNIT_POSITION events to the simplified API (`/api/events/position`). Messaging (RabbitMQ) was removed, so this uses HTTP only.

## Features
- Deterministic random walk (seed configurable)
- Multiple units with per-interval position updates
- Adjustable interval, duration, origin, speed range
- Graceful Ctrl+C cancellation

## Build
```powershell
cd simulator/GeoEvents.Simulator
dotnet build
```

## Run Examples
```powershell
# Default (5 units, 60s, 1s interval)
dotnet run

# 10 units, 2s interval, 30s duration, custom API base
dotnet run -- units=10 interval=2000 duration=30 api=https://localhost:5045/

# Faster stream: 8 units every 200ms for 20s
dotnet run -- units=8 interval=200 duration=20

# Different origin and seed
dotnet run -- originLat=40.98 originLon=28.80 seed=777 units=6
```

## Arguments
| Key        | Description                            | Default |
|------------|----------------------------------------|---------|
| `units`    | Number of units                        | 5       |
| `interval` | Interval between batches (ms)          | 1000    |
| `duration` | Total runtime (seconds)                | 60      |
| `seed`     | RNG seed for deterministic run         | 12345   |
| `originLat`| Start latitude center                  | 41.10   |
| `originLon`| Start longitude center                 | 29.00   |
| `api`      | API base URL                           | https://localhost:5045/ |
| `minspeed` | Min speed meters/sec                   | 2       |
| `maxspeed` | Max speed meters/sec                   | 15      |

## Output
Periodic progress logs and a final total of events sent.

## Notes
- Adjust API base URL if your backend runs on a different port.
- The lat/lon delta math is approximate (sufficient for demo visuals).
- Extend with zone violation or proximity events later if needed.
