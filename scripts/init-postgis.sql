-- Initialize PostGIS extension and create spatial indexes
-- This script runs automatically when PostgreSQL container starts

-- Enable PostGIS extension
CREATE EXTENSION IF NOT EXISTS postgis;
CREATE EXTENSION IF NOT EXISTS postgis_topology;

-- Verify PostGIS installation
SELECT PostGIS_Version();

-- Create schema for application tables (optional, for organization)
CREATE SCHEMA IF NOT EXISTS geoevents;

-- Set search path
ALTER DATABASE geoevents SET search_path TO public, geoevents, postgis;

-- Grant privileges to application user
GRANT ALL PRIVILEGES ON SCHEMA geoevents TO geouser;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA geoevents TO geouser;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA geoevents TO geouser;

-- Future migrations will be handled by EF Core
-- This script only sets up PostGIS foundation
