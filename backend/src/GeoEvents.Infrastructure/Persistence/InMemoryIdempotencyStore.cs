using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using GeoEvents.Application.Abstractions;

namespace GeoEvents.Infrastructure.Persistence;

/// <summary>
/// In-memory idempotency store (for demo/dev). 
/// Production: use Redis or Postgres table.
/// </summary>
public sealed class InMemoryIdempotencyStore : IIdempotencyStore
{
    private readonly ConcurrentDictionary<Guid, DateTimeOffset> _processedEvents = new();

    public Task<bool> TryMarkProcessedAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var added = _processedEvents.TryAdd(eventId, DateTimeOffset.UtcNow);
        return Task.FromResult(added);
    }

    public int Count => _processedEvents.Count;

    public void Clear() => _processedEvents.Clear();
}
