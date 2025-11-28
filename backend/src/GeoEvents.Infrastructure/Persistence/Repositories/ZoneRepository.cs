using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GeoEvents.Domain.Entities;
using GeoEvents.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace GeoEvents.Infrastructure.Persistence.Repositories;

public sealed class ZoneRepository
{
    private readonly GeoEventsDbContext _context;

    public ZoneRepository(GeoEventsDbContext context)
    {
        _context = context;
    }

    public async Task<Zone?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Zones.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<Zone?> GetByIdentifierAsync(string identifier, CancellationToken cancellationToken = default)
    {
        return await _context.Zones
            .FirstOrDefaultAsync(z => z.Identifier == identifier, cancellationToken);
    }

    public async Task<List<Zone>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        return await _context.Zones
            .Where(z => (z.ActiveFrom == null || z.ActiveFrom <= now) &&
                        (z.ActiveUntil == null || z.ActiveUntil >= now))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Finds zones that intersect with a given point using PostGIS ST_Intersects.
    /// </summary>
    public async Task<List<Zone>> GetZonesContainingPointAsync(
        GeoCoordinate point,
        CancellationToken cancellationToken = default)
    {
        var geomPoint = new Point(point.Longitude, point.Latitude) { SRID = 4326 };

        // Raw SQL for ST_Intersects with computed geometry column
        var zones = await _context.Zones
            .FromSqlRaw(@"
                SELECT * FROM zones 
                WHERE ST_Intersects(""Geometry"", ST_GeomFromText({0}, 4326))",
                geomPoint.AsText())
            .ToListAsync(cancellationToken);

        return zones;
    }

    /// <summary>
    /// Finds zones within a buffer distance of a point.
    /// </summary>
    public async Task<List<Zone>> GetZonesNearPointAsync(
        GeoCoordinate point,
        double bufferMeters,
        CancellationToken cancellationToken = default)
    {
        var geomPoint = new Point(point.Longitude, point.Latitude) { SRID = 4326 };

        var zones = await _context.Zones
            .FromSqlRaw(@"
                SELECT * FROM zones 
                WHERE ST_DWithin(
                    ""Geometry""::geography, 
                    ST_GeomFromText({0}, 4326)::geography, 
                    {1})",
                geomPoint.AsText(),
                bufferMeters)
            .ToListAsync(cancellationToken);

        return zones;
    }

    public async Task AddAsync(Zone zone, CancellationToken cancellationToken = default)
    {
        await _context.Zones.AddAsync(zone, cancellationToken);
    }

    public void Update(Zone zone)
    {
        _context.Zones.Update(zone);
    }

    public void Remove(Zone zone)
    {
        _context.Zones.Remove(zone);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
