using System.Threading;
using System.Threading.Tasks;

namespace GeoEvents.Application.Abstractions;

/// <summary>
/// Dispatches raw event envelopes to registered handlers.
/// </summary>
public interface IEventDispatcher
{
    Task DispatchAsync(object dto, CancellationToken cancellationToken = default);
}
