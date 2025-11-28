# GitHub Copilot Instructions for
**military-grade-geo-event-platform**

> Purpose: tell Copilot how to assist when authoring, refactoring, or completing files in this repo. Keep suggestions safe for a public portfolio (no sensitive data), high-quality, and aligned with clean architecture, event-driven design, and geospatial best practices.

---

## 1. Short summary (what this repo is)
A real-time, message-driven geospatial event processing demo platform (backend: ASP.NET Core + RabbitMQ + Postgres/PostGIS; frontend: Angular + Cesium). The repo includes a simulator to generate realistic events and a rules engine for spatial checks (ST_DWithin, ST_Intersects, buffers). The project is intended as a portfolio "gold" project — production-like architecture but demo-ready and safe for public sharing.

## 2. Main goals for Copilot suggestions
- Prefer **clean architecture**, single responsibility, testable services, and DI-friendly components.
- Suggest **type-safe** DTOs and domain models for events and payloads (use `Guid`, `DateTimeOffset`, strongly typed value objects for geometry where appropriate).
- Produce **message-driven** patterns: topic exchanges, routing keys, idempotent handlers, message validation.
- Favor **PostGIS-aware queries** that are performant and index-friendly; when writing SQL suggest use of `ST_DWithin`, `ST_Buffer` and `&&` (bounding-box) optimizations where relevant.
- For spatial logic, prefer expressing rules in an extensible, code-driven rules engine (pluggable rule interfaces), not only raw SQL strings.
- For simulation code, produce deterministic seeds where possible and provide configurable scenarios (normal / conflict / noisy / sparse).
- Keep security-conscious defaults: validate incoming messages, size limits, schema validation, and avoid logging sensitive payloads.

## 3. Files & locations (what Copilot should create/complete)
- `backend/` — ASP.NET Core solution
  - `Api/` — controllers, minimal endpoints for health, metrics, and manual publish (only for simulator/demo)
  - `Application/`
    - `EventProcessing/` — message handlers, dispatchers, idempotency
    - `SpatialRules/` — rule interfaces, rule registry, sample rules (zone violation, proximity)
    - `DTOs/` — event DTOs and mappers
  - `Domain/` — entities (Unit, Zone, Event), value objects (GeoCoordinate, Heading)
  - `Infrastructure/`
    - `Messaging/RabbitMq*` — exchange setup, publishers, subscribers
    - `Persistence/PostgresRepository*` — EF Core (or Dapper) repos with PostGIS usage
  - `Tests/` — unit and integration tests (in-memory RabbitMQ test harness recommended)

- `frontend/` — Angular app
  - `src/app/services/socket.service.ts` (WebSocket / SignalR)
  - `src/app/map/` — map component using Cesium
  - `src/app/event-stream/` — live timeline, filters

- `simulator/` — event generator
  - scenario configs, deterministic/random modes, ability to publish to RabbitMQ

- `docs/` — architecture.md, event-model.md, security-considerations.md, scaling-strategy.md

## 4. Coding style & conventions
- C# (backend)
  - Use `async`/`await` everywhere for IO-bound work.
  - Prefer `I`-prefixed interfaces and constructor DI.
  - Keep methods ≤ 60 lines when possible; extract small private helpers.
  - Use `Record` types for immutable DTOs where appropriate.
  - Use `CancellationToken` on public async methods.
- SQL/PostGIS
  - Use parameterized queries, avoid string interpolation for SQL.
  - Add `CREATE INDEX` examples for geom columns (GIST index) in migrations.
- Angular
  - Use strict TypeScript options and typed services.
  - Keep UI logic in services; components should be presentational.
- Repo
  - Provide `README.md` with a quick demo-run using the simulator; no secrets.

## 5. Example prompts to drive Copilot completions
- "Create an `IEventHandler<T>` interface and a `RabbitMqEventDispatcher` that resolves handlers via DI and handles deduplication by eventId."
- "Generate a Postgres repository method `GetUnitsWithinDistance(Point p, double meters)` using EF Core raw SQL and `ST_DWithin`, and ensure the SQL uses the geometry column with SRID and a bounding box `&&` optimization." 
- "Write a sample `ZoneViolationRule` implementing `ISpatialRule` that checks `ST_DWithin(unit.geom, zone.geom, buffer)` and produces a `ZoneViolationEvent` DTO." 
- "Implement a deterministic simulator scenario 'convoy-moving' that spawns 10 units and publishes `UNIT_POSITION` every second using topic key `geo.unit.position`." 

## 6. Example event schema (use this shape for suggestions)
```json
{
  "eventId": "00000000-0000-0000-0000-000000000000",
  "type": "UNIT_POSITION",
  "timestamp": "2025-01-01T12:00:00Z",
  "source": "unit-alpha",
  "location": { "lat": 41.1, "lon": 29.0 },
  "metadata": { "speed": 45, "heading": 120 }
}
```
Copilot suggestions should align with this shape when generating DTOs, mappers, and validation code.

## 7. Tests & validation (what to include)
- Unit tests for each spatial rule using small, explicit geometries (use WKT or GeoJSON in tests).
- Integration tests that run against a Dockerized Postgres+PostGIS instance (if creating Dockerfiles, include `docker-compose.test.yml`).
- Message handler tests: use an in-memory or test RabbitMQ (or a lightweight test harness) to assert message flows and idempotency.
- Frontend snapshot tests for key components (map + event list) where rendering logic is non-trivial.

## 8. Security & portfolio safety
- Do not include real operational data or classified examples in the repo.
- Provide `security-considerations.md` that explains auth (OAuth2/JWT recommended), message signing/validation, replay prevention, and RBAC, but contains only concepts and sanitized examples.
- Suggest builders produce configuration via environment variables and do not hardcode credentials.

## 9. Performance & scaling hints
- Recommend publish/subscribe with topic exchange: `geo.events` and routing keys like `geo.unit.position`, `geo.sensor.alert`, `geo.zone.violation`.
- Suggest batching DB writes for high-throughput ingestion and using COPY where appropriate.
- Use geometric bounding-box checks (`&&`) before precise geometry functions to leverage GiST indexes.

## 10. Simulator & demo guidance
- Simulator should support these modes: `realtime`, `replay` (from recorded scenario), and `burst` (stress test).
- Provide a `demo.sh` script that populates sample zones/units and runs the simulator in a safe, local-only mode.

## 11. Commit messages & PR guidance
- Copilot should suggest small, focused commits and conventional messages, e.g. `feat(events): add spatial rule interface and zone-violation rule`.
- PR checklist suggestions: tests added, migration added/verified, message schema documented, simulation scenario included, docs updated.

## 12. When to avoid or alter suggestions
- Do **not** produce code that references or implies use of classified datasets, real unit identifiers, or operational communications.
- If a suggestion would leak sensitive operational patterns, reframe as an anonymized or synthetic example.

---

If you need a more compact template (short prompts or one-line completions) or a variant tuned for code generation only (vs docs + tests), say which and I will provide it.


