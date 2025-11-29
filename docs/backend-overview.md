# Backend Overview

## Technology Stack
- .NET 9 (ASP.NET Core)
- EF Core 9 + Npgsql + NetTopologySuite (SRID 4326)
- RabbitMQ (Client 6.8.x, topic exchange)
- Postgres 16 + PostGIS 3.4 (Docker)

## Projects
- `backend/src/GeoEvents.Domain`: Core domain model and value objects; pure C#.
- `backend/src/GeoEvents.Application`: DTOs, mapping, `IEventHandler`, `IEventDispatcher`, `ISpatialRule` and implementations.
- `backend/src/GeoEvents.Infrastructure`: Messaging (RabbitMqPublisher, RabbitMqConsumer), Persistence (DbContext, repositories), Idempotency store.
- `backend/src/GeoEvents.Api`: API composition root; controllers for Health, Units, Zones, Events; DI registrations; OpenAPI.

## Key Classes
- Domain
  - `Unit`, `Zone`, `GeoEvent`
  - `GeoCoordinate`, `Heading`, `Velocity`
- Application
  - DTOs: `GeoEventDto`, `UnitPositionEventDto`, `ZoneViolationEventDto`, `ProximityAlertEventDto`
  - `IEventHandler<T>`, `IEventDispatcher`, `IEventValidator`, `IIdempotencyStore`
  - Spatial rules: e.g., `ZoneViolationRule` (PostGIS-aware)
- Infrastructure
  - `RabbitMqPublisher.PublishAsync<T>(dto, routingKey, ct)`
  - `RabbitMqConsumer` (BackgroundService with EventingBasicConsumer)
  - `GeoEventsDbContext` + entity configurations; repositories (`UnitRepository`, `ZoneRepository`)

## DI Composition (API)
- Registers DbContext with Postgres + NetTopologySuite
- Registers messaging publisher/consumer and idempotency store
- Registers repositories and spatial rules
- Enables CORS, health checks, OpenAPI

## Endpoints
- `GET /health` — simple health info
- `GET /units/nearby?lat=&lon=&meters=` — nearby units via `ST_DWithin`
- `GET /zones` — active zones
- `POST /events/publish` — manual publish (demo)

## Configuration
Use `appsettings.json` or environment variables:
- ConnectionStrings: `Host=localhost;Port=5433;Database=geoevents;Username=geouser;Password=geopass123`
- RabbitMQ: `Host=localhost;Port=5672;Username=geouser;Password=geopass123;Exchange=geo.events`

## Migrations & Indexes
- Geometry columns use SRID 4326; migrations include GiST index creation guidance.

## Tests
- Domain unit tests (xUnit) — invariants/value objects
- Application rule tests — small geometries via WKT/GeoJSON
- Integration tests — Dockerized Postgres+PostGIS and RabbitMQ (optional harness)
