using System.Threading;
using System.Threading.Tasks;

namespace GeoEvents.Application.Abstractions;

/// <summary>
/// Abstraction for broadcasting events to real-time clients (e.g., SignalR).
/// </summary>
public interface IEventBroadcaster
{
    Task BroadcastAsync(object eventDto, CancellationToken cancellationToken = default);
}
