using System.Threading;
using System.Threading.Tasks;

namespace GeoEvents.Application.Abstractions;

/// <summary>
/// Validates incoming event payloads before dispatch.
/// </summary>
public interface IEventValidator<in TEventDto>
{
    Task ValidateAsync(TEventDto dto, CancellationToken cancellationToken = default);
}
