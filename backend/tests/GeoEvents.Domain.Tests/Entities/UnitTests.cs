using GeoEvents.Domain.Entities;
using GeoEvents.Domain.ValueObjects;
using Xunit;

namespace GeoEvents.Domain.Tests.Entities;

public class UnitTests
{
    [Fact]
    public void Create_ShouldRaiseUnitCreatedEvent()
    {
        var position = GeoCoordinate.Create(41.0, 29.0);
        var unit = Unit.Create("unit-alpha-1", "Alpha 1", "Infantry", position);
        Assert.Single(unit.DomainEvents);
        Assert.Contains(unit.DomainEvents, e => e is UnitCreatedEvent);
    }

    [Fact]
    public void UpdatePosition_ShouldRaisePositionUpdatedEvent()
    {
        var unit = Unit.Create("unit-alpha-1", "Alpha 1", "Infantry", GeoCoordinate.Create(41.0, 29.0));
        unit.ClearDomainEvents();
        var newPos = GeoCoordinate.Create(41.0005, 29.0005);
        unit.UpdatePosition(newPos, Heading.FromDegrees(90), Velocity.Create(10));
        Assert.Single(unit.DomainEvents);
        Assert.Contains(unit.DomainEvents, e => e is UnitPositionUpdatedEvent);
    }

    [Fact]
    public void TwoDistinctUnits_WithSameInitialValues_ShouldNotBeEqual()
    {
        var p = GeoCoordinate.Create(41.0, 29.0);
        var u1 = Unit.Create("unit-alpha", "Alpha", "Infantry", p);
        var u2 = Unit.Create("unit-alpha", "Alpha", "Infantry", p);
        Assert.NotEqual(u1, u2); // Different identity (Id)
    }

    [Fact]
    public void HasMovedSignificantly_BelowThreshold_ShouldReturnFalse()
    {
        var unit = Unit.Create("unit-alpha", "Alpha", "Infantry", GeoCoordinate.Create(41.0, 29.0));
        var slightMove = GeoCoordinate.Create(41.00001, 29.00001);
        Assert.False(unit.HasMovedSignificantly(slightMove, thresholdMeters: 5));
    }
}
