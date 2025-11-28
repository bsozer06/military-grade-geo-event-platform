using System;
using System.Threading;
using System.Threading.Tasks;

namespace GeoEvents.Application.Abstractions;

/// <summary>
/// Persists processed event identifiers to guarantee idempotent handling.
/// </summary>
public interface IIdempotencyStore
{
    /// <summary>
    /// Attempts to mark the eventId as processed. Returns false if already processed.
    /// </summary>
    Task<bool> TryMarkProcessedAsync(Guid eventId, CancellationToken cancellationToken = default);
}
