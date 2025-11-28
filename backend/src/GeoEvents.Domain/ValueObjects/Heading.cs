using GeoEvents.Domain.Common;

namespace GeoEvents.Domain.ValueObjects;

/// <summary>
/// Represents a compass heading/bearing in degrees.
/// Valid range: 0-359 degrees (0 = North, 90 = East, 180 = South, 270 = West).
/// Immutable value object.
/// </summary>
public sealed class Heading : ValueObject
{
    public double Degrees { get; }

    private Heading(double degrees)
    {
        Degrees = degrees;
    }

    /// <summary>
    /// Creates a heading from degrees.
    /// </summary>
    /// <param name="degrees">Heading in degrees (0-359)</param>
    /// <exception cref="ArgumentOutOfRangeException">When degrees is outside valid range</exception>
    public static Heading FromDegrees(double degrees)
    {
        if (degrees < 0 || degrees >= 360)
            throw new ArgumentOutOfRangeException(nameof(degrees), 
                $"Heading must be between 0 and 359 degrees. Got: {degrees}");

        return new Heading(degrees);
    }

    /// <summary>
    /// Creates a heading from radians.
    /// </summary>
    public static Heading FromRadians(double radians)
    {
        var degrees = radians * (180.0 / Math.PI);
        degrees = NormalizeDegrees(degrees);
        return new Heading(degrees);
    }

    /// <summary>
    /// Normalizes degrees to 0-359 range.
    /// </summary>
    public static double NormalizeDegrees(double degrees)
    {
        degrees = degrees % 360;
        if (degrees < 0)
            degrees += 360;
        return degrees;
    }

    /// <summary>
    /// Gets the opposite heading (180 degrees difference).
    /// </summary>
    public Heading Opposite()
    {
        var opposite = (Degrees + 180) % 360;
        return new Heading(opposite);
    }

    /// <summary>
    /// Calculates the angular difference to another heading.
    /// Returns the shortest angle between the two headings (-180 to 180).
    /// </summary>
    public double DifferenceTo(Heading other)
    {
        var diff = other.Degrees - Degrees;
        
        // Normalize to -180 to 180
        while (diff > 180) diff -= 360;
        while (diff < -180) diff += 360;
        
        return diff;
    }

    /// <summary>
    /// Gets the cardinal direction name.
    /// </summary>
    public string GetCardinalDirection()
    {
        return Degrees switch
        {
            >= 337.5 or < 22.5 => "N",
            >= 22.5 and < 67.5 => "NE",
            >= 67.5 and < 112.5 => "E",
            >= 112.5 and < 157.5 => "SE",
            >= 157.5 and < 202.5 => "S",
            >= 202.5 and < 247.5 => "SW",
            >= 247.5 and < 292.5 => "W",
            >= 292.5 and < 337.5 => "NW",
            _ => "N"
        };
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Math.Round(Degrees, 2); // Round to avoid floating point issues
    }

    public override string ToString() => $"{Degrees:F1}° ({GetCardinalDirection()})";
}
