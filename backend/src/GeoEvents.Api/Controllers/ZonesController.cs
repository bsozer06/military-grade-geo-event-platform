using GeoEvents.Domain.Entities;
using GeoEvents.Domain.ValueObjects;
using GeoEvents.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace GeoEvents.Api.Controllers;

/// <summary>
/// Endpoints for managing geographic zones.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class ZonesController : ControllerBase
{
    private readonly ZoneRepository _zoneRepository;
    private readonly ILogger<ZonesController> _logger;

    public ZonesController(
        ZoneRepository zoneRepository,
        ILogger<ZonesController> logger)
    {
        _zoneRepository = zoneRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get all zones.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken = default)
    {
        var zones = await _zoneRepository.GetAllActiveAsync(cancellationToken);
        return Ok(zones);
    }

    /// <summary>
    /// Get zone by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var zone = await _zoneRepository.GetByIdAsync(id, cancellationToken);
        if (zone == null)
        {
            return NotFound(new { message = $"Zone with ID {id} not found" });
        }
        return Ok(zone);
    }

    /// <summary>
    /// Get zones that contain a specific point.
    /// </summary>
    [HttpGet("containing")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetContainingPoint(
        [FromQuery] double latitude,
        [FromQuery] double longitude,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var coordinate = GeoCoordinate.Create(latitude, longitude);
            var zones = await _zoneRepository.GetZonesContainingPointAsync(
                coordinate,
                cancellationToken);
            
            return Ok(new
            {
                point = new { latitude, longitude },
                count = zones.Count,
                zones
            });
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Create a new zone (demo purposes).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateZoneRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Zone zone;
            
            if (request.Type == "circular")
            {
                var center = GeoCoordinate.Create(request.Latitude!.Value, request.Longitude!.Value);
                var identifier = $"zone-{Guid.NewGuid():N}";
                zone = Zone.CreateCircular(
                    identifier,
                    request.Name,
                    request.ZoneType ?? "Custom",
                    center,
                    request.RadiusMeters!.Value,
                    request.IsRestricted ?? true,
                    request.PriorityLevel ?? 5);
            }
            else if (request.Type == "polygon")
            {
                var identifier = $"zone-{Guid.NewGuid():N}";
                // Using reflection to call private constructor - not ideal but works for demo
                var constructor = typeof(Zone).GetConstructor(
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                    null,
                    new[] { typeof(string), typeof(string), typeof(string), typeof(string), typeof(bool), typeof(int) },
                    null);
                
                if (constructor == null)
                    return BadRequest(new { message = "Unable to create polygon zone" });
                
                zone = (Zone)constructor.Invoke(new object[]
                {
                    identifier,
                    request.Name,
                    request.ZoneType ?? "Custom",
                    request.WktGeometry!,
                    request.IsRestricted ?? true,
                    request.PriorityLevel ?? 5
                });
            }
            else
            {
                return BadRequest(new { message = "Type must be 'circular' or 'polygon'" });
            }

            await _zoneRepository.AddAsync(zone, cancellationToken);
            await _zoneRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created zone {ZoneName} with ID {ZoneId}", zone.Name, zone.Id);

            return CreatedAtAction(
                nameof(GetById),
                new { id = zone.Id },
                zone);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

public record CreateZoneRequest(
    string Name,
    string Type, // "circular" or "polygon"
    double? Latitude,
    double? Longitude,
    double? RadiusMeters,
    string? WktGeometry,
    string? ZoneType,
    bool? IsRestricted,
    int? PriorityLevel);
