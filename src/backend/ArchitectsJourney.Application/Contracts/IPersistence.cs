using ArchitectsJourney.Application.Events;

namespace ArchitectsJourney.Application.Contracts;

/// <summary>
/// Persistence contract for checking point session states.
/// Implements Save System responsibilities described in Document 09.
/// </summary>
public interface ISaveSystem
{
    Task SaveCheckpointAsync(
        Guid sessionId,
        ulong sequenceNumber,
        IReadOnlyList<SubsystemSnapshot> snapshots,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SubsystemSnapshot>?> LoadLastCheckpointAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Persistence contract for the Audit Event Store described in Document 10, Section 16.
/// </summary>
public interface IAuditEventStore
{
    Task AppendEventAsync(
        DomainEvent @event,
        ulong sequenceNumber,
        CancellationToken cancellationToken = default);

    Task UpdateLifecycleStatusAsync(
        Guid eventId,
        string status,
        CancellationToken cancellationToken = default);

    Task RecordAcknowledgementAsync(
        Guid eventId,
        string subscriberId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DomainEvent>> GetEventsForSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DomainEvent>> GetCausalChainAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default);
}
