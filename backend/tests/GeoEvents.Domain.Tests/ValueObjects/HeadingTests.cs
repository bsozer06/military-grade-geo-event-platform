using GeoEvents.Domain.ValueObjects;
using Xunit;

namespace GeoEvents.Domain.Tests.ValueObjects;

public class HeadingTests
{
    [Theory]
    [InlineData(0, "N")]
    [InlineData(45, "NE")]
    [InlineData(90, "E")]
    [InlineData(180, "S")]
    [InlineData(270, "W")]
    public void CardinalDirection_ShouldMatchExpected(double degrees, string expected)
    {
        var h = Heading.FromDegrees(degrees);
        Assert.Equal(expected, h.GetCardinalDirection());
    }

    [Fact]
    public void DifferenceTo_ShouldReturnShortestAngle()
    {
        var a = Heading.FromDegrees(350);
        var b = Heading.FromDegrees(10);
        Assert.Equal(20, a.DifferenceTo(b));
        Assert.Equal(-20, b.DifferenceTo(a));
    }

    [Fact]
    public void Opposite_ShouldBe180Apart()
    {
        var h = Heading.FromDegrees(10);
        var opposite = h.Opposite();
        Assert.Equal(190, opposite.Degrees);
    }

    [Fact]
    public void FromDegrees_Invalid_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Heading.FromDegrees(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Heading.FromDegrees(360));
    }
}
