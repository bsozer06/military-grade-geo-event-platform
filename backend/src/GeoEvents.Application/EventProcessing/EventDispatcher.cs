using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GeoEvents.Application.Abstractions;

namespace GeoEvents.Application.EventProcessing;

/// <summary>
/// Default event dispatcher that resolves handlers dynamically.
/// </summary>
public sealed class EventDispatcher : IEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private static readonly ConcurrentDictionary<Type, Type[]> _handlerCache = new();

    public EventDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task DispatchAsync(object dto, CancellationToken cancellationToken = default)
    {
        if (dto is null) throw new ArgumentNullException(nameof(dto));
        var dtoType = dto.GetType();

        var handlerTypes = _handlerCache.GetOrAdd(dtoType, t =>
        {
            // Find all IEventHandler<T> where T == dtoType
            return AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(tp => !tp.IsAbstract && !tp.IsInterface)
                .SelectMany(tp => tp.GetInterfaces(), (tp, i) => new { tp, i })
                .Where(x => x.i.IsGenericType && x.i.GetGenericTypeDefinition() == typeof(IEventHandler<>))
                .Where(x => x.i.GenericTypeArguments[0] == dtoType)
                .Select(x => x.tp)
                .ToArray();
        });

        foreach (var handlerType in handlerTypes)
        {
            var handler = _serviceProvider.GetService(handlerType);
            if (handler is null) continue;
            var method = handlerType.GetMethod("HandleAsync");
            if (method is null) continue;
            var task = method.Invoke(handler, new[] { dto, cancellationToken });
            if (task is Task awaited)
                await awaited.ConfigureAwait(false);
        }
    }
}
