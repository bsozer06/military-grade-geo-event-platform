using System.Threading;
using System.Threading.Tasks;
using GeoEvents.Api.Hubs;
using GeoEvents.Application.Abstractions;
using Microsoft.AspNetCore.SignalR;

namespace GeoEvents.Api.Services;

/// <summary>
/// SignalR implementation of IEventBroadcaster.
/// Broadcasts events to all connected clients via the EventHub.
/// </summary>
public sealed class SignalREventBroadcaster : IEventBroadcaster
{
    private readonly IHubContext<EventHub> _hubContext;

    public SignalREventBroadcaster(IHubContext<EventHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task BroadcastAsync(object eventDto, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.All.SendAsync("geo-event", eventDto, cancellationToken);
    }
}
