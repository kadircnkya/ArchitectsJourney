namespace ArchitectsJourney.Application.Contracts;

/// <summary>
/// Infrastructure-level idempotency service for event processing.
/// Tracks processed events to guarantee exactly-once mutation across stateless engines.
/// </summary>
public interface IEventIdempotencyTracker
{
    /// <summary>
    /// Checks if the event has been processed for the specified playthrough,
    /// and if not, marks it as processed atomically.
    /// </summary>
    Task<bool> TryMarkProcessedAsync(Guid playthroughId, Guid eventId, CancellationToken cancellationToken = default);
}
