using GeoEvents.Domain.Entities;
using GeoEvents.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NetTopologySuite.Geometries;

namespace GeoEvents.Infrastructure.Persistence.Configurations;

public sealed class GeoEventConfiguration : IEntityTypeConfiguration<GeoEvent>
{
    public void Configure(EntityTypeBuilder<GeoEvent> builder)
    {
        builder.ToTable("geo_events");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.EventType)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(e => e.EventType);

        builder.Property(e => e.Source)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(e => e.Source);

        builder.Property(e => e.Location)
            .HasConversion(
                v => ToPoint(v),
                v => FromPoint(v))
            .HasColumnType("geometry(Point, 4326)")
            .IsRequired();

        builder.HasIndex(e => e.Location)
            .HasMethod("GIST");

        builder.Property(e => e.Timestamp)
            .IsRequired();

        builder.HasIndex(e => e.Timestamp);

        builder.Property(e => e.Heading)
            .HasConversion(
                v => v != null ? (double?)v.Degrees : null,
                v => ConvertToHeading(v));

        builder.Property(e => e.Velocity)
            .HasConversion(
                v => v != null ? (double?)v.ToMetersPerSecond() : null,
                v => ConvertToVelocity(v));

        builder.Property(e => e.Severity)
            .IsRequired();

        builder.Property(e => e.Metadata)
            .HasColumnType("jsonb");

        builder.Property(e => e.ProcessedAt)
            .IsRequired();

        builder.Property(e => e.CorrelationId);

        builder.HasIndex(e => e.CorrelationId);
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
