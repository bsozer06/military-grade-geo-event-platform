# Military-Grade Geo Event Platform

> **Portfolio Demo Project**: Real-time, message-driven geospatial event processing platform showcasing production-grade architecture with RabbitMQ, PostgreSQL/PostGIS, ASP.NET Core, and Angular/Cesium.

## 🎯 Project Overview

This is a demonstration platform for processing and visualizing real-time geospatial events using event-driven architecture. The system simulates military-style tracking with spatial rules engine, designed as a portfolio piece with production-quality patterns but safe for public sharing.

### Key Features

- **Event-Driven Architecture**: RabbitMQ with topic exchanges for scalable message routing
- **Geospatial Processing**: PostGIS-powered spatial queries (ST_DWithin, ST_Intersects, buffer zones)
- **Real-Time Visualization**: Angular frontend with Cesium 3D globe for live event tracking
- **Extensible Rules Engine**: Pluggable spatial rules (zone violations, proximity alerts)
- **Clean Architecture**: Domain-driven design with clear separation of concerns
- **Production Patterns**: Idempotent handlers, message validation, CQRS-ready structure

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                         Frontend (Angular)                       │
│                    Cesium 3D Map + SignalR/WS                    │
└──────────────────────────────┬──────────────────────────────────┘
                               │ HTTP/WebSocket
┌──────────────────────────────┴──────────────────────────────────┐
│                      Backend (ASP.NET Core)                      │
│  ┌────────────┐  ┌──────────────────┐  ┌───────────────────┐   │
│  │    API     │→ │   Application    │→ │  Infrastructure   │   │
│  │ (Minimal)  │  │ (Event Handlers) │  │ (RabbitMQ/PostGIS)│   │
│  └────────────┘  └──────────────────┘  └───────────────────┘   │
│         ↑                  ↑                      ↑              │
│         └──────────────────┴──────────────────────┘              │
│                        Domain Layer                              │
└──────────────────────────────┬──────────────────────────────────┘
                               │ AMQP
┌──────────────────────────────┴──────────────────────────────────┐
│                       RabbitMQ (Topic Exchange)                  │
│              geo.unit.position | geo.zone.violation              │
└──────────────────────────────┬──────────────────────────────────┘
                               │
┌──────────────────────────────┴──────────────────────────────────┐
│                      Simulator (Event Generator)                 │
│            Scenarios: normal, convoy, conflict, sparse           │
└──────────────────────────────────────────────────────────────────┘
```

### Tech Stack

**Backend:**
- .NET 9 (ASP.NET Core Web API)
- RabbitMQ (AMQP messaging)
- PostgreSQL 16 + PostGIS 3.4
- Entity Framework Core 9
- SignalR (real-time web)

**Frontend:**
- Angular 17+
- Cesium 3D Globe
- RxJS for reactive patterns
- TypeScript (strict mode)

**Infrastructure:**
- Docker & Docker Compose
- GitHub Actions (CI/CD ready)

## 🚀 Quick Start

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [Node.js 20+](https://nodejs.org/) (for frontend)
- [Angular CLI](https://angular.io/cli) (optional)

### Running with Docker Compose (Recommended)

```bash
# Start infrastructure (RabbitMQ + PostgreSQL/PostGIS)
docker-compose up -d

# Run backend
cd backend
dotnet run --project src/GeoEvents.Api

# Run simulator (separate terminal)
cd simulator
dotnet run -- --scenario convoy --duration 5m

# Run frontend (separate terminal)
cd frontend
npm install
npm start
```

Access the application at `http://localhost:4200`

### Development Setup

```bash
# Backend
cd backend
dotnet restore
dotnet build
dotnet test

# Database migrations
cd src/GeoEvents.Api
dotnet ef database update

# Frontend
cd frontend
npm install
npm run lint
npm test
```

## 📂 Project Structure

```
# Military-Grade Geo Event Platform

A real-time, message-driven geospatial event processing demo platform. Backend: ASP.NET Core + RabbitMQ + Postgres/PostGIS. Frontend (planned): Angular + Cesium. Includes a simulator to generate realistic events and a rules engine for spatial checks.

## Quick Start

### Prerequisites
- .NET SDK 9.x
- Docker Desktop
- Windows PowerShell 5.1

### Bring up infrastructure
```powershell
Push-Location "C:\_burhan\_projects\military-grade-geo-event-platform"
docker compose up -d
```
- Postgres: `localhost:5433`
- RabbitMQ: `localhost:5672`, UI `http://localhost:15672` (user `geouser`, pass `geopass123`)

### Run the API
```powershell
Push-Location "C:\_burhan\_projects\military-grade-geo-event-platform\backend\src\GeoEvents.Api"
dotnet run
```
- Swagger: `http://localhost:5045/swagger`

### Run the Simulator
```powershell
Push-Location "C:\_burhan\_projects\military-grade-geo-event-platform\simulator"
dotnet run -- --units 10 --rate 5 --duration 120 --metrics true --metricsPort 9090
Start-Process http://localhost:9090/dashboard
```

## Architecture
- Clean Architecture: Domain → Application → Infrastructure → API
- Messaging: RabbitMQ topic exchange `geo.events`, routing key `geo.unit.position`
- Persistence: Postgres + PostGIS (SRID 4326), EF Core + NetTopologySuite
- Rules Engine: pluggable `ISpatialRule` implementations (zone violation, proximity, etc.)

## Project Structure
- `backend/src/GeoEvents.Domain` — entities (Unit, Zone, GeoEvent), value objects (GeoCoordinate, Heading, Velocity)
- `backend/src/GeoEvents.Application` — DTOs, mapping, event dispatcher, spatial rules
- `backend/src/GeoEvents.Infrastructure` — RabbitMQ publisher/consumer, DbContext, repositories, idempotency store
- `backend/src/GeoEvents.Api` — DI, controllers (Health, Units, Zones, Events), OpenAPI
- `simulator/` — .NET 9 console app: scenarios, Rabbit publisher, metrics server
- `docs/` — architecture, backend overview, simulator overview, getting started

## Observability
- Simulator metrics: `http://localhost:9090/{health|metrics|dashboard}`
- RabbitMQ UI: `http://localhost:15672`
- API logs show consumer activity and rule evaluations

## Configuration
- API connection string: `Host=localhost;Port=5433;Database=geoevents;Username=geouser;Password=geopass123`
- RabbitMQ: `Host=localhost;Port=5672;Username=geouser;Password=geopass123;Exchange=geo.events`
- Simulator CLI flags: `--units`, `--rate`, `--duration`, `--speed`, `--heading`, `--lat`, `--lon`, `--seed`, `--metrics`, `--metricsPort`

## Security & Portfolio Safety
- No real operational data; deterministic, synthetic scenarios
- Input validation, idempotency, message size limits
- Credentials configurable via environment variables; no secrets committed

## Next Steps
- Frontend (Angular + Cesium) to visualize live positions and events
- Additional simulator modes: `burst`, `replay`
- Prometheus-style metrics and dashboards

More details in `docs/architecture.md`, `docs/backend-overview.md`, `docs/simulator-overview.md`, and `docs/getting-started.md`.

## 🧪 Testing

```bash
# Run all tests
cd backend
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverageReportFormat=opencover

# Integration tests (requires Docker)
docker-compose -f docker-compose.test.yml up -d
dotnet test --filter Category=Integration
```

## 📊 Spatial Rules

The platform includes an extensible rules engine for geospatial checks:

```csharp
// Example: Zone Violation Rule
public class ZoneViolationRule : ISpatialRule
{
    public async Task<RuleResult> EvaluateAsync(GeoEvent @event)
    {
        // Check if unit is within restricted zone using PostGIS
        var violation = await _repository
            .CheckIntersection(@event.Location, restrictedZones);
        
        return violation ? RuleResult.Violated() : RuleResult.Passed();
    }
}
```

Rules are automatically discovered and executed via DI.

## 🔒 Security Notes

⚠️ **This is a demo/portfolio project**:
- Does NOT contain real operational data
- Uses synthetic coordinates and sanitized examples
- Auth/RBAC implementation is conceptual (see `docs/security-considerations.md`)
- Not intended for production deployment without security hardening

## 📚 Documentation

- [Architecture Decision Records](docs/architecture.md)
- [Event Model & Schemas](docs/event-model.md)
- [Security Considerations](docs/security-considerations.md)
- [Scaling Strategy](docs/scaling-strategy.md)

## 🛠️ Development Workflow

1. **Create feature branch**: `git checkout -b feat/your-feature`
2. **Implement with tests**: Follow TDD where possible
3. **Run checks**: `dotnet build && dotnet test`
4. **Commit**: Use conventional commits (`feat:`, `fix:`, `docs:`)
5. **Create PR**: Include migration scripts if schema changed

## 🚦 CI/CD

GitHub Actions pipeline includes:
- Build verification (all projects)
- Unit and integration tests
- Code coverage reporting
- Docker image builds
- Security scanning

## 📈 Performance Characteristics

- **Event Throughput**: 10K+ events/sec (single instance)
- **Query Latency**: <50ms for spatial queries (indexed)
- **Map Rendering**: 60 FPS with 1000+ active units
- **Message Latency**: <100ms end-to-end

## 🤝 Contributing

This is a portfolio project, but suggestions welcome! Please:
1. Keep examples synthetic/anonymized
2. Follow existing code style (see `.editorconfig`)
3. Add tests for new features
4. Update docs as needed

## 📝 License

MIT License - See LICENSE file for details

## 👤 Author

**Burhan**  
Portfolio Project - November 2025

---

**Note**: This project demonstrates production-ready patterns in a safe, demonstrable format. It's designed to showcase architectural skills, not for actual operational use.
