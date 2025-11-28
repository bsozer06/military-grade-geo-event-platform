namespace GeoEvents.Infrastructure.Persistence;

public sealed class PostgreSqlOptions
{
    public const string SectionName = "ConnectionStrings";

    public string PostgreSQL { get; set; } = "Host=localhost;Database=geoevents;Username=geouser;Password=geopass123";
}
