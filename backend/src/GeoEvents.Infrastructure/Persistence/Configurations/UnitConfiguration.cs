using GeoEvents.Domain.Entities;
using GeoEvents.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NetTopologySuite.Geometries;

namespace GeoEvents.Infrastructure.Persistence.Configurations;

public sealed class UnitConfiguration : IEntityTypeConfiguration<Unit>
{
    public void Configure(EntityTypeBuilder<Unit> builder)
    {
        builder.ToTable("units");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Identifier)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(u => u.Identifier)
            .IsUnique();

        builder.Property(u => u.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(u => u.UnitType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(u => u.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        // Position as PostGIS Point (SRID 4326 = WGS 84)
        builder.Property(u => u.Position)
            .HasConversion(
                v => ToPoint(v),
                v => FromPoint(v))
            .HasColumnType("geometry(Point, 4326)")
            .IsRequired();

        builder.HasIndex(u => u.Position)
            .HasMethod("GIST");

        // Heading as double (degrees)
        builder.Property(u => u.CurrentHeading)
            .HasConversion(
                v => v != null ? (double?)v.Degrees : null,
                v => ConvertToHeading(v));

        // Velocity as m/s
        builder.Property(u => u.CurrentVelocity)
            .HasConversion(
                v => v != null ? (double?)v.ToMetersPerSecond() : null,
                v => ConvertToVelocity(v));

        builder.Property(u => u.Metadata)
            .HasColumnType("jsonb");

        builder.Property(u => u.LastPositionUpdate)
            .IsRequired();

        builder.Property(u => u.CreatedAt)
            .IsRequired();

        builder.Ignore(u => u.DomainEvents);
    }

    private static Point ToPoint(GeoCoordinate coord)
    {
        var point = new Point(coord.Longitude, coord.Latitude) { SRID = 4326 };
        if (coord.Altitude.HasValue)
        {
            point = new Point(coord.Longitude, coord.Latitude, coord.Altitude.Value) { SRID = 4326 };
        }
        return point;
    }

    private static GeoCoordinate FromPoint(Point point)
    {
        var alt = point.CoordinateSequence.Dimension > 2 ? point.Z : (double?)null;
        if (alt.HasValue)
            return GeoCoordinate.Create(point.Y, point.X, alt.Value);
        return GeoCoordinate.Create(point.Y, point.X);
    }

    private static Heading? ConvertToHeading(double? degrees)
    {
        if (!degrees.HasValue) return null;
        return Heading.FromDegrees(degrees.Value);
    }

    private static Velocity? ConvertToVelocity(double? metersPerSecond)
    {
        if (!metersPerSecond.HasValue) return null;
        return Velocity.Create(metersPerSecond.Value);
    }
}
