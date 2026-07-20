namespace ArchitectsJourney.Domain.Common;

/// <summary>
/// Base class for all aggregate roots.
/// Aggregates collect domain events during state transitions.
/// The Application layer dispatches those events through the Event Bus after
/// the aggregate operation completes successfully.
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected AggregateRoot(TId id) : base(id) { }

    /// <summary>
    /// Domain events raised during this aggregate's state transitions.
    /// Read by the Application layer after a command completes.
    /// </summary>
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Raises a domain event and adds it to the pending collection.
    /// Call this inside aggregate state-change methods.
    /// </summary>
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Clears all pending domain events.
    /// Called by the Application layer after events have been dispatched.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}
