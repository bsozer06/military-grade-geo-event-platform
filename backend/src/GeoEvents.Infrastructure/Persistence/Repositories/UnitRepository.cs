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

public sealed class UnitRepository
{
    private readonly GeoEventsDbContext _context;

    public UnitRepository(GeoEventsDbContext context)
    {
        _context = context;
    }

    public async Task<Unit?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Units.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<Unit?> GetByIdentifierAsync(string identifier, CancellationToken cancellationToken = default)
    {
        return await _context.Units
            .FirstOrDefaultAsync(u => u.Identifier == identifier, cancellationToken);
    }

    public async Task<List<Unit>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Units.ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Finds units within a specified distance from a point using PostGIS ST_DWithin.
    /// </summary>
    public async Task<List<Unit>> GetUnitsWithinDistanceAsync(
        GeoCoordinate center,
        double distanceMeters,
        CancellationToken cancellationToken = default)
    {
        var point = new Point(center.Longitude, center.Latitude) { SRID = 4326 };

        // Use raw SQL with ST_DWithin for accurate distance (geography)
        var units = await _context.Units
            .FromSqlRaw(@"
                SELECT * FROM units 
                WHERE ST_DWithin(""Position""::geography, ST_GeomFromText({0}, 4326)::geography, {1})",
                point.AsText(),
                distanceMeters)
            .ToListAsync(cancellationToken);

        return units;
    }

    public async Task AddAsync(Unit unit, CancellationToken cancellationToken = default)
    {
        await _context.Units.AddAsync(unit, cancellationToken);
    }

    public void Update(Unit unit)
    {
        _context.Units.Update(unit);
    }

    public void Remove(Unit unit)
    {
        _context.Units.Remove(unit);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
