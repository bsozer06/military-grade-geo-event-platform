using GeoEvents.Application.DTOs;
using GeoEvents.Infrastructure.Messaging;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace GeoEvents.Api.Controllers;

/// <summary>
/// Endpoints for publishing events (demo/simulator purposes).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class EventsController : ControllerBase
{
    private readonly RabbitMqPublisher _publisher;
    private readonly ILogger<EventsController> _logger;

    public EventsController(
        RabbitMqPublisher publisher,
        ILogger<EventsController> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    /// <summary>
    /// Publish a unit position event (demo).
    /// </summary>
    [HttpPost("position")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PublishPositionEvent(
        [FromBody] UnitPositionEventDto eventDto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var routingKey = "geo.unit.position";
            
            await _publisher.PublishAsync(eventDto, routingKey, cancellationToken);
            
            _logger.LogInformation(
                "Published position event {EventId} at ({Lat}, {Lon})",
                eventDto.EventId,
                eventDto.Latitude,
                eventDto.Longitude);

            return Accepted(new
            {
                message = "Event published successfully",
                eventId = eventDto.EventId,
                routingKey
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish position event");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Publish a zone violation event (demo).
    /// </summary>
    [HttpPost("violation")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PublishViolationEvent(
        [FromBody] ZoneViolationEventDto eventDto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var routingKey = "geo.zone.violation";
            
            await _publisher.PublishAsync(eventDto, routingKey, cancellationToken);
            
            _logger.LogInformation(
                "Published zone violation event {EventId}",
                eventDto.EventId);

            return Accepted(new
            {
                message = "Event published successfully",
                eventId = eventDto.EventId,
                routingKey
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish violation event");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Publish a generic geo event (demo).
    /// </summary>
    [HttpPost("generic")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PublishGenericEvent(
        [FromBody] GeoEventDto eventDto,
        [FromQuery] string routingKey = "geo.generic.event",
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _publisher.PublishAsync(eventDto, routingKey, cancellationToken);
            
            _logger.LogInformation(
                "Published generic event {EventId}",
                eventDto.EventId);

            return Accepted(new
            {
                message = "Event published successfully",
                eventId = eventDto.EventId,
                routingKey
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish generic event");
            return BadRequest(new { message = ex.Message });
        }
    }
}
