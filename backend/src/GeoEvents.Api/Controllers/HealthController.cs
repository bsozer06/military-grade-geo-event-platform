using Microsoft.AspNetCore.Mvc;

namespace GeoEvents.Api.Controllers;

/// <summary>
/// Health check and diagnostics endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Basic health check endpoint.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTimeOffset.UtcNow,
            service = "GeoEvents.Api"
        });
    }

    /// <summary>
    /// Detailed system information.
    /// </summary>
    [HttpGet("info")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetInfo()
    {
        return Ok(new
        {
            service = "GeoEvents.Api",
            version = "1.0.0",
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
            timestamp = DateTimeOffset.UtcNow,
            uptime = TimeSpan.FromMilliseconds(Environment.TickCount64),
            framework = Environment.Version.ToString()
        });
    }
}
