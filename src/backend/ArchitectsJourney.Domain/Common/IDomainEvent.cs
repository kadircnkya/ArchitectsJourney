namespace ArchitectsJourney.Domain.Common;

/// <summary>
/// Marker interface for aggregate-level domain events raised by aggregates.
/// These are distinct from the platform DomainEvent envelope (Application layer)
/// which flows through the Event Bus.
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTimeOffset OccurredAt { get; }
}
