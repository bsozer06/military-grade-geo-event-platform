using GeoEvents.Domain.ValueObjects;
using GeoEvents.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace GeoEvents.Api.Controllers;

/// <summary>
/// Endpoints for querying military units.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class UnitsController : ControllerBase
{
    private readonly UnitRepository _unitRepository;
    private readonly ILogger<UnitsController> _logger;

    public UnitsController(
        UnitRepository unitRepository,
        ILogger<UnitsController> logger)
    {
        _unitRepository = unitRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get all units.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken = default)
    {
        var units = await _unitRepository.GetAllAsync(cancellationToken);
        return Ok(units);
    }

    /// <summary>
    /// Get unit by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var unit = await _unitRepository.GetByIdAsync(id, cancellationToken);
        if (unit == null)
        {
            return NotFound(new { message = $"Unit with ID {id} not found" });
        }
        return Ok(unit);
    }

    /// <summary>
    /// Get units within a certain distance from a point.
    /// </summary>
    [HttpGet("nearby")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetNearby(
        [FromQuery] double latitude,
        [FromQuery] double longitude,
        [FromQuery] double radiusMeters = 5000,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var coordinate = GeoCoordinate.Create(latitude, longitude);
            var units = await _unitRepository.GetUnitsWithinDistanceAsync(
                coordinate,
                radiusMeters,
                cancellationToken);
            
            return Ok(new
            {
                center = new { latitude, longitude },
                radiusMeters,
                count = units.Count,
                units
            });
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get unit count statistics.
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken = default)
    {
        var units = await _unitRepository.GetAllAsync(cancellationToken);
        
        return Ok(new
        {
            totalUnits = units.Count,
            unitsByIdentifier = units.GroupBy(u => u.Identifier).Select(g => new
            {
                identifier = g.Key,
                count = g.Count()
            }).ToList(),
            unitsByType = units.GroupBy(u => u.UnitType).Select(g => new
            {
                unitType = g.Key,
                count = g.Count()
            }).ToList(),
            timestamp = DateTimeOffset.UtcNow
        });
    }
}
