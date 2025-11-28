using GeoEvents.Domain.Common;

namespace GeoEvents.Domain.ValueObjects;

/// <summary>
/// Represents velocity/speed with unit.
/// Immutable value object.
/// </summary>
public sealed class Velocity : ValueObject
{
    public double Speed { get; }
    public VelocityUnit Unit { get; }

    private Velocity(double speed, VelocityUnit unit)
    {
        Speed = speed;
        Unit = unit;
    }

    /// <summary>
    /// Creates a velocity value.
    /// </summary>
    /// <param name="speed">Speed value (must be non-negative)</param>
    /// <param name="unit">Unit of measurement</param>
    public static Velocity Create(double speed, VelocityUnit unit = VelocityUnit.MetersPerSecond)
    {
        if (speed < 0)
            throw new ArgumentOutOfRangeException(nameof(speed), 
                "Speed cannot be negative");

        if (speed > 1000 && unit == VelocityUnit.MetersPerSecond)
            throw new ArgumentOutOfRangeException(nameof(speed), 
                "Speed seems unrealistically high (>1000 m/s)");

        return new Velocity(speed, unit);
    }

    /// <summary>
    /// Converts velocity to meters per second.
    /// </summary>
    public double ToMetersPerSecond()
    {
        return Unit switch
        {
            VelocityUnit.MetersPerSecond => Speed,
            VelocityUnit.KilometersPerHour => Speed / 3.6,
            VelocityUnit.MilesPerHour => Speed * 0.44704,
            VelocityUnit.Knots => Speed * 0.514444,
            _ => throw new ArgumentOutOfRangeException(nameof(Unit))
        };
    }

    /// <summary>
    /// Converts velocity to kilometers per hour.
    /// </summary>
    public double ToKilometersPerHour()
    {
        return ToMetersPerSecond() * 3.6;
    }

    /// <summary>
    /// Converts velocity to the specified unit.
    /// </summary>
    public Velocity ConvertTo(VelocityUnit targetUnit)
    {
        var metersPerSecond = ToMetersPerSecond();
        
        var converted = targetUnit switch
        {
            VelocityUnit.MetersPerSecond => metersPerSecond,
            VelocityUnit.KilometersPerHour => metersPerSecond * 3.6,
            VelocityUnit.MilesPerHour => metersPerSecond / 0.44704,
            VelocityUnit.Knots => metersPerSecond / 0.514444,
            _ => throw new ArgumentOutOfRangeException(nameof(targetUnit))
        };

        return new Velocity(converted, targetUnit);
    }

    /// <summary>
    /// Checks if this velocity is approximately zero.
    /// </summary>
    public bool IsStationary(double threshold = 0.1)
    {
        return ToMetersPerSecond() < threshold;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        // Compare in m/s for consistency across units
        yield return Math.Round(ToMetersPerSecond(), 2);
    }

    public override string ToString()
    {
        var unitSymbol = Unit switch
        {
            VelocityUnit.MetersPerSecond => "m/s",
            VelocityUnit.KilometersPerHour => "km/h",
            VelocityUnit.MilesPerHour => "mph",
            VelocityUnit.Knots => "kn",
            _ => ""
        };

        return $"{Speed:F2} {unitSymbol}";
    }
}

public enum VelocityUnit
{
    MetersPerSecond,
    KilometersPerHour,
    MilesPerHour,
    Knots
}
