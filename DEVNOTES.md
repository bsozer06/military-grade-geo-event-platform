# Military-Grade Geo Event Platform - Development Notes

## Project Status: ✅ Phase 1 Complete

### Completed (November 28, 2025)

#### Backend Structure (.NET 9)
- ✅ Solution created with Clean Architecture layers
- ✅ **GeoEvents.Domain** - Domain entities and value objects
- ✅ **GeoEvents.Application** - Use cases and interfaces
- ✅ **GeoEvents.Infrastructure** - External integrations (RabbitMQ, PostGIS)
- ✅ **GeoEvents.Api** - ASP.NET Core Web API

#### Infrastructure
- ✅ Docker Compose configuration
  - PostgreSQL 16 + PostGIS 3.4
  - RabbitMQ 3.13 with management UI
  - Pre-configured exchanges and queues
- ✅ Database initialization scripts
- ✅ RabbitMQ topology definitions

### Next Steps

#### Immediate (Phase 2): Domain Layer
1. Create core domain entities:
   - `Unit` entity with position tracking
   - `Zone` entity with geometry
   - `GeoEvent` base class
2. Value objects:
   - `GeoCoordinate` (lat/lon with validation)
   - `Heading` (0-359 degrees)
   - `Velocity` (speed with units)
3. Domain services for spatial calculations

#### Phase 3: Application Layer
- Event DTOs and mapping
- `IEventHandler<TEvent>` interface
- `ISpatialRule` interface
- Message validation logic

#### Phase 4: Infrastructure Implementation
- RabbitMQ publisher/subscriber
- EF Core DbContext with PostGIS support
- Repository implementations
- Entity configurations and migrations

#### Phase 5: Simulator
- Console application for event generation
- Scenario configurations (convoy, patrol, etc.)
- Deterministic and random modes

#### Phase 6: Frontend
- Angular 17+ application
- Cesium integration
- SignalR for real-time updates

## Quick Commands

```bash
# Start infrastructure
docker-compose up -d

# Check infrastructure health
docker ps
docker logs geoevents-postgres
docker logs geoevents-rabbitmq

# Backend development
cd backend
dotnet build
dotnet run --project src/GeoEvents.Api

# Run tests (when added)
dotnet test

# RabbitMQ Management UI
http://localhost:15672
# Login: geouser / geopass123
```

## Architecture Decisions

### Why Topic Exchange?
- Flexible routing with wildcards
- Supports multiple consumers per event type
- Easy to add new event types without queue changes

### Why Clean Architecture?
- Testable domain logic
- Independent of frameworks
- Flexible infrastructure swapping

### Why PostGIS?
- Native spatial indexing (GiST)
- Rich spatial functions (ST_DWithin, ST_Buffer)
- Industry standard for geospatial

## Development Principles

1. **Security First**: No real data, synthetic coordinates only
2. **Test Coverage**: Aim for >80% on business logic
3. **Performance**: Index all spatial queries
4. **Observability**: Structured logging, correlation IDs
5. **Idempotency**: All event handlers must be idempotent

## Notes for AI Assistants

- Follow copilot-instructions.md for coding patterns
- Use async/await for all IO operations
- Prefer record types for DTOs
- Always include CancellationToken parameters
- Use parameterized queries, never string interpolation
