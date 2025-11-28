using GeoEvents.Domain.Entities;
using GeoEvents.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace GeoEvents.Infrastructure.Persistence.Configurations;

public sealed class ZoneConfiguration : IEntityTypeConfiguration<Zone>
{
    public void Configure(EntityTypeBuilder<Zone> builder)
    {
        builder.ToTable("zones");

        builder.HasKey(z => z.Id);

        builder.Property(z => z.Identifier)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(z => z.Identifier)
            .IsUnique();

        builder.Property(z => z.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(z => z.ZoneType)
            .IsRequired()
            .HasMaxLength(50);

        // Geometry stored as PostGIS geometry (SRID 4326)
        builder.Property(z => z.GeometryWkt)
            .HasConversion(
                v => v, // Store as-is in WKT column
                v => v)
            .HasColumnName("geometry_wkt")
            .HasColumnType("text")
            .IsRequired();

        // Computed PostGIS geometry column from WKT
        builder.Property<Geometry>("Geometry")
            .HasColumnType("geometry")
            .HasComputedColumnSql("ST_GeomFromText(geometry_wkt, 4326)", stored: true);

        builder.HasIndex("Geometry")
            .HasMethod("GIST");

        // Center point
        builder.Property(z => z.CenterPoint)
            .HasConversion(
                v => v != null ? ToPoint(v) : null,
                v => v != null ? FromPoint(v) : null)
            .HasColumnType("geometry(Point, 4326)");

        builder.Property(z => z.RadiusMeters);

        builder.Property(z => z.IsRestricted)
            .IsRequired();

        builder.Property(z => z.PriorityLevel)
            .IsRequired();

        builder.Property(z => z.ActiveFrom);
        builder.Property(z => z.ActiveUntil);

        builder.Property(z => z.Metadata)
            .HasColumnType("jsonb");

        builder.Property(z => z.CreatedAt)
            .IsRequired();

        builder.Ignore(z => z.DomainEvents);
    }

    private static Point? ToPoint(GeoCoordinate coord)
    {
        if (coord == null) return null;
        var point = new Point(coord.Longitude, coord.Latitude) { SRID = 4326 };
        if (coord.Altitude.HasValue)
        {
            point = new Point(coord.Longitude, coord.Latitude, coord.Altitude.Value) { SRID = 4326 };
        }
        return point;
    }

    private static GeoCoordinate? FromPoint(Point? point)
    {
        if (point == null) return null;
        var alt = point.CoordinateSequence.Dimension > 2 ? point.Z : (double?)null;
        return GeoCoordinate.Create(point.Y, point.X, alt);
    }
}
