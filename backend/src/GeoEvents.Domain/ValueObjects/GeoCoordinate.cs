using GeoEvents.Domain.Common;

namespace GeoEvents.Domain.ValueObjects;

/// <summary>
/// Represents a geographic coordinate with latitude and longitude.
/// Immutable value object with validation.
/// SRID 4326 (WGS 84) is assumed for all coordinates.
/// </summary>
public sealed class GeoCoordinate : ValueObject
{
    public double Latitude { get; }
    public double Longitude { get; }
    
    /// <summary>
    /// Altitude in meters above sea level (optional).
    /// </summary>
    public double? Altitude { get; }

    private GeoCoordinate(double latitude, double longitude, double? altitude = null)
    {
        Latitude = latitude;
        Longitude = longitude;
        Altitude = altitude;
    }

    /// <summary>
    /// Creates a new geographic coordinate.
    /// </summary>
    /// <param name="latitude">Latitude in decimal degrees (-90 to 90)</param>
    /// <param name="longitude">Longitude in decimal degrees (-180 to 180)</param>
    /// <param name="altitude">Optional altitude in meters</param>
    /// <returns>A valid GeoCoordinate or throws exception if invalid</returns>
    /// <exception cref="ArgumentOutOfRangeException">When coordinates are out of valid range</exception>
    public static GeoCoordinate Create(double latitude, double longitude, double? altitude = null)
    {
        if (latitude < -90 || latitude > 90)
            throw new ArgumentOutOfRangeException(nameof(latitude), 
                $"Latitude must be between -90 and 90 degrees. Got: {latitude}");

        if (longitude < -180 || longitude > 180)
            throw new ArgumentOutOfRangeException(nameof(longitude), 
                $"Longitude must be between -180 and 180 degrees. Got: {longitude}");

        if (altitude.HasValue && altitude.Value < -500)
            throw new ArgumentOutOfRangeException(nameof(altitude), 
                $"Altitude seems unrealistic (< -500m). Got: {altitude}");

        return new GeoCoordinate(latitude, longitude, altitude);
    }

    /// <summary>
    /// Calculates approximate distance to another coordinate in meters using Haversine formula.
    /// Note: For precise spatial queries, use PostGIS ST_Distance instead.
    /// </summary>
    public double DistanceTo(GeoCoordinate other)
    {
        const double earthRadiusMeters = 6371000;

        var lat1Rad = DegreesToRadians(Latitude);
        var lat2Rad = DegreesToRadians(other.Latitude);
        var deltaLat = DegreesToRadians(other.Latitude - Latitude);
        var deltaLon = DegreesToRadians(other.Longitude - Longitude);

        var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return earthRadiusMeters * c;
    }

    /// <summary>
    /// Returns WKT (Well-Known Text) representation for use with PostGIS.
    /// Format: POINT(longitude latitude)
    /// </summary>
    public string ToWkt()
    {
        if (Altitude.HasValue)
            return $"POINT({Longitude} {Latitude} {Altitude})";
        
        return $"POINT({Longitude} {Latitude})";
    }

    /// <summary>
    /// Creates a GeoCoordinate from WKT string.
    /// </summary>
    public static GeoCoordinate FromWkt(string wkt)
    {
        // Simple parser for POINT(lon lat) or POINT(lon lat alt)
        var cleaned = wkt.Replace("POINT(", "").Replace(")", "").Trim();
        var parts = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 2)
            throw new ArgumentException($"Invalid WKT format: {wkt}");

        var lon = double.Parse(parts[0]);
        var lat = double.Parse(parts[1]);
        var alt = parts.Length > 2 ? double.Parse(parts[2]) : (double?)null;

        return Create(lat, lon, alt);
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Latitude;
        yield return Longitude;
        yield return Altitude;
    }

    public override string ToString()
    {
        if (Altitude.HasValue)
            return $"({Latitude:F6}°, {Longitude:F6}°, {Altitude:F1}m)";
        
        return $"({Latitude:F6}°, {Longitude:F6}°)";
    }
}
