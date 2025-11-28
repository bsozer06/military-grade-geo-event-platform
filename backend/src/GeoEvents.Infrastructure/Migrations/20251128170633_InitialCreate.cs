using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace GeoEvents.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.CreateTable(
                name: "geo_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Location = table.Column<Point>(type: "geometry(Point, 4326)", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Heading = table.Column<double>(type: "double precision", nullable: true),
                    Velocity = table.Column<double>(type: "double precision", nullable: true),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_geo_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "units",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Identifier = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UnitType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Position = table.Column<Point>(type: "geometry(Point, 4326)", nullable: false),
                    CurrentHeading = table.Column<double>(type: "double precision", nullable: true),
                    CurrentVelocity = table.Column<double>(type: "double precision", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LastPositionUpdate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_units", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "zones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Identifier = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ZoneType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    geometry_wkt = table.Column<string>(type: "text", nullable: false),
                    CenterPoint = table.Column<Point>(type: "geometry(Point, 4326)", nullable: true),
                    RadiusMeters = table.Column<double>(type: "double precision", nullable: true),
                    IsRestricted = table.Column<bool>(type: "boolean", nullable: false),
                    PriorityLevel = table.Column<int>(type: "integer", nullable: false),
                    ActiveFrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ActiveUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Geometry = table.Column<Geometry>(type: "geometry", nullable: true, computedColumnSql: "ST_GeomFromText(geometry_wkt, 4326)", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_zones", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_geo_events_CorrelationId",
                table: "geo_events",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_geo_events_EventType",
                table: "geo_events",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_geo_events_Location",
                table: "geo_events",
                column: "Location")
                .Annotation("Npgsql:IndexMethod", "GIST");

            migrationBuilder.CreateIndex(
                name: "IX_geo_events_Source",
                table: "geo_events",
                column: "Source");

            migrationBuilder.CreateIndex(
                name: "IX_geo_events_Timestamp",
                table: "geo_events",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_units_Identifier",
                table: "units",
                column: "Identifier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_units_Position",
                table: "units",
                column: "Position")
                .Annotation("Npgsql:IndexMethod", "GIST");

            migrationBuilder.CreateIndex(
                name: "IX_zones_Geometry",
                table: "zones",
                column: "Geometry")
                .Annotation("Npgsql:IndexMethod", "GIST");

            migrationBuilder.CreateIndex(
                name: "IX_zones_Identifier",
                table: "zones",
                column: "Identifier",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "geo_events");

            migrationBuilder.DropTable(
                name: "units");

            migrationBuilder.DropTable(
                name: "zones");
        }
    }
}
