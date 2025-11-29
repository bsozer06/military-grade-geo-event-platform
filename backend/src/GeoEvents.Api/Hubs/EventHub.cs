using Microsoft.AspNetCore.SignalR;

namespace GeoEvents.Api.Hubs;

/// <summary>
/// SignalR hub for broadcasting real-time geo-events to connected clients.
/// Frontend listens on 'geo-event' method for incoming event payloads.
/// </summary>
public class EventHub : Hub
{
    /// <summary>
    /// Broadcast an event to all connected clients.
    /// Called internally by the backend when processing RabbitMQ messages.
    /// </summary>
    public async Task BroadcastEvent(object eventPayload)
    {
        await Clients.All.SendAsync("geo-event", eventPayload);
    }
}
