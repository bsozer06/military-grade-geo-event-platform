using GeoEvents.Domain.ValueObjects;
using Xunit;

namespace GeoEvents.Domain.Tests.ValueObjects;

public class GeoCoordinateTests
{
    [Fact]
    public void Equality_SameValues_ShouldBeEqual()
    {
        var c1 = GeoCoordinate.Create(41.0, 29.0, 150);
        var c2 = GeoCoordinate.Create(41.0, 29.0, 150);
        Assert.Equal(c1, c2); // Value equality
        Assert.False(ReferenceEquals(c1, c2)); // Different instances
    }

    [Fact]
    public void Create_InvalidLatitude_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => GeoCoordinate.Create(91, 10));
    }

    [Fact]
    public void DistanceTo_ShouldReturnPositiveMeters()
    {
        var istanbul = GeoCoordinate.Create(41.0082, 28.9784);
        var ankara = GeoCoordinate.Create(39.9208, 32.8541);
        var distance = istanbul.DistanceTo(ankara);
        Assert.True(distance > 300_000 && distance < 500_000); // Rough sanity range
    }

    [Fact]
    public void ToWkt_AltitudeProvided_ShouldIncludeAltitude()
    {
        var c = GeoCoordinate.Create(10, 20, 100);
        var wkt = c.ToWkt();
        Assert.Equal("POINT(20 10 100)", wkt);
    }
}
