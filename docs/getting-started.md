# Getting Started

## Prerequisites
- Windows PowerShell 5.1 (default)
- .NET SDK 9.x
- Docker Desktop

## Bring Up Infrastructure
```powershell
Push-Location "C:\_burhan\_projects\military-grade-geo-event-platform"
docker compose up -d
```
- Postgres: `localhost:5433`
- RabbitMQ: `localhost:5672`, UI `http://localhost:15672` (user `geouser`, pass `geopass123`)

## Run API
```powershell
Push-Location "C:\_burhan\_projects\military-grade-geo-event-platform\backend\src\GeoEvents.Api"
dotnet run
```
- Swagger: `http://localhost:5045/swagger`
- Watch consumer logs in the console

## Run Simulator
```powershell
Push-Location "C:\_burhan\_projects\military-grade-geo-event-platform\simulator"
dotnet run -- --units 10 --rate 5 --duration 120 --metrics true --metricsPort 9090
Start-Process http://localhost:9090/dashboard
```

## Verify Flow
- RabbitMQ UI → Exchange `geo.events` and bound queues; watch rates
- API logs → background consumer receiving and processing messages
- Postgres → verify data (optional) using any SQL client

## Troubleshooting
- Port conflicts: Postgres uses host `5433`; ensure no service listening there
- If simulator metrics endpoints error near run end, the server is shutting down — probe during active run
- Ensure `appsettings.Development.json` aligns with Docker credentials
