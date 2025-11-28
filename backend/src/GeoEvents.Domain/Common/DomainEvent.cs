namespace GeoEvents.Domain.Common;

/// <summary>
/// Represents a domain event that has occurred in the system.
/// Domain events are used to trigger side effects across aggregates.
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTimeOffset OccurredAt { get; }
}

/// <summary>
/// Base implementation of domain event.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
