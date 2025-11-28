using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace GeoEvents.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for GeoEventsDbContext (used by EF Core migrations).
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<GeoEventsDbContext>
{
    public GeoEventsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<GeoEventsDbContext>();
        
        // Default connection string for design-time migrations
        // In production this comes from configuration
        var connectionString = "Host=localhost;Port=5433;Database=geoevents;Username=geouser;Password=geopass123";
        
        optionsBuilder.UseNpgsql(connectionString, o =>
        {
            o.UseNetTopologySuite(); // Enables PostGIS spatial support
            o.MigrationsAssembly(typeof(GeoEventsDbContext).Assembly.FullName);
        });
        
        return new GeoEventsDbContext(optionsBuilder.Options);
    }
}
