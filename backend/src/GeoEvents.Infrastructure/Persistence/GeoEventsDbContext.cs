using GeoEvents.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace GeoEvents.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext with PostGIS support.
/// </summary>
public sealed class GeoEventsDbContext : DbContext
{
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Zone> Zones => Set<Zone>();
    public DbSet<GeoEvent> GeoEvents => Set<GeoEvent>();

    public GeoEventsDbContext(DbContextOptions<GeoEventsDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("postgis");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GeoEventsDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
