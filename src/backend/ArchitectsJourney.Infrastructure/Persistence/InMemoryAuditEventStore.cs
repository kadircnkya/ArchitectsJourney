using ArchitectsJourney.Application.Contracts;
using ArchitectsJourney.Application.Events;
using System.Collections.Concurrent;

namespace ArchitectsJourney.Infrastructure.Persistence;

/// <summary>
/// Thread-safe in-memory AuditEventStore implementation.
/// Logs state transitions and sequence tracking according to Document 10 requirements.
/// </summary>
public sealed class InMemoryAuditEventStore : IAuditEventStore
{
    private readonly ConcurrentDictionary<Guid, AuditEntry> _store = new();

    private sealed record AuditEntry(
        DomainEvent Event,
        ulong SequenceNumber,
        string Status,
        List<string> Confirmations);

    public Task AppendEventAsync(DomainEvent @event, ulong sequenceNumber, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);
        _store[@event.EventId] = new AuditEntry(@event, sequenceNumber, "ENQUEUED", []);
        return Task.CompletedTask;
    }

    public Task UpdateLifecycleStatusAsync(Guid eventId, string status, CancellationToken cancellationToken = default)
    {
        if (_store.TryGetValue(eventId, out var entry))
        {
            _store[eventId] = entry with { Status = status };
        }
        return Task.CompletedTask;
    }

    public Task RecordAcknowledgementAsync(Guid eventId, string subscriberId, CancellationToken cancellationToken = default)
    {
        if (_store.TryGetValue(eventId, out var entry))
        {
            lock (entry.Confirmations)
            {
                entry.Confirmations.Add(subscriberId);
            }
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<DomainEvent>> GetEventsForSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<DomainEvent> list = _store.Values
            .Where(e => e.Event.SessionId == sessionId)
            .OrderBy(e => e.SequenceNumber)
            .Select(e => e.Event)
            .ToList();
            
        return Task.FromResult(list);
    }

    public Task<IReadOnlyList<DomainEvent>> GetCausalChainAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<DomainEvent> list = _store.Values
            .Where(e => e.Event.CorrelationId == correlationId)
            .OrderBy(e => e.SequenceNumber)
            .Select(e => e.Event)
            .ToList();

        return Task.FromResult(list);
    }
}
