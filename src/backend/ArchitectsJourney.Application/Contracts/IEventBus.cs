using ArchitectsJourney.Application.Events;

namespace ArchitectsJourney.Application.Contracts;

/// <summary>
/// Result returned when an event is published to the Event Bus.
/// Defined in Document 10, Section 12.3.
/// </summary>
public sealed record PublicationResult
{
    public bool Accepted { get; init; }
    public ulong? SequenceNumber { get; init; }
    public string? RejectionReason { get; init; }
    public Guid EventId { get; init; }

    public static PublicationResult Success(Guid eventId, ulong sequenceNumber) =>
        new() { Accepted = true, EventId = eventId, SequenceNumber = sequenceNumber };

    /// <summary>
    /// Accepted but dispatch deferred (event buffered as derived event).
    /// SequenceNumber will be assigned when dispatched.
    /// </summary>
    public static PublicationResult Buffered(Guid eventId) =>
        new() { Accepted = true, EventId = eventId };

    public static PublicationResult Rejected(Guid eventId, string reason) =>
        new() { Accepted = false, EventId = eventId, RejectionReason = reason };
}

/// <summary>
/// The Event Bus contract — the communication backbone of the platform.
/// The Event System owns communication. It does not evaluate rules,
/// mutate state, or contain business logic.
/// Defined in Document 10, Section 12.
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Publishes an event to the Event Bus.
    /// If called from within a subscriber's OnEventAsync (derived event scenario),
    /// the event is buffered and dispatched after the current in-flight event completes.
    /// Returns immediately — publishers do not wait for subscriber acknowledgements.
    /// </summary>
    Task<PublicationResult> PublishAsync(
        DomainEvent @event,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a subscriber for one or more event types.
    /// Must be called during application startup — before any session begins.
    /// </summary>
    void RegisterSubscriber(SubscriptionRegistration registration);

    /// <summary>
    /// Registers an authorized publisher.
    /// Must be called during application startup.
    /// </summary>
    void RegisterPublisher(PublisherRegistration registration);
}
