using System.Threading;
using System.Threading.Tasks;

namespace GeoEvents.Application.Abstractions;

/// <summary>
/// Handles a specific event DTO type. Implementations must be idempotent.
/// </summary>
/// <typeparam name="TEventDto">Concrete event DTO type.</typeparam>
public interface IEventHandler<in TEventDto>
{
    /// <summary>
    /// Processes the event. Must be idempotent based on eventId.
    /// </summary>
    Task HandleAsync(TEventDto dto, CancellationToken cancellationToken = default);
}
